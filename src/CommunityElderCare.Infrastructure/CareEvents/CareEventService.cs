using CommunityElderCare.Core.CareEvents;
using CommunityElderCare.Core.Common;
using CommunityElderCare.Core.Identity;
using CommunityElderCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommunityElderCare.Infrastructure.CareEvents;

public sealed class CareEventService(
    CommunityCareDbContext dbContext,
    TimeProvider timeProvider,
    EscalationPolicy escalationPolicy) : ICareEventService
{
    private readonly CareEventCorrelator _correlator = new();

    public async Task<OperationResult<CareEventOperationResult>> CreateAsync(
        CreateCareEventCommand command,
        ActorContext? actor,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.SourceEventId) ||
            string.IsNullOrWhiteSpace(command.Summary))
        {
            return Failure("INVALID_EVENT", "Source event ID and summary are required.");
        }

        var areaResult = await ResolveAreaAsync(command.ElderId, cancellationToken);
        if (!areaResult.IsSuccess)
        {
            return Failure(areaResult.ErrorCode!, areaResult.ErrorMessage!);
        }
        if (!CanCreate(command, actor, areaResult.Value!))
        {
            return Failure("FORBIDDEN_SCOPE", "Care-event creation scope denied.");
        }

        var existing = await FindBySourceEventIdAsync(
            command.ElderId,
            command.SourceEventId,
            tracking: false,
            cancellationToken);
        if (existing is not null)
        {
            return Success(existing, isDuplicate: true);
        }

        var now = timeProvider.GetUtcNow();
        var classification = CareEventClassifier.Classify(command.Trigger);
        if (command.Trigger == CareEventTrigger.DeviceAnomaly)
        {
            var openEvents = await LoadForElderAsync(command.ElderId, tracking: true, cancellationToken);
            var matchId = _correlator.FindMatch(openEvents, command.ElderId, command.OccurredAt);
            if (matchId is not null)
            {
                var match = openEvents.Single(careEvent => careEvent.Id == matchId.Value);
                var evidenceId = Guid.NewGuid();
                match.AddEvidence(
                    evidenceId,
                    command.Trigger.ToString(),
                    command.Summary,
                    command.OccurredAt,
                    now,
                    command.SourceEventId);
                dbContext.CareEventEvidence.Add(match.Evidence.Single(item => item.Id == evidenceId));
                try
                {
                    await dbContext.SaveChangesAsync(cancellationToken);
                    return Success(match, isDuplicate: false);
                }
                catch (DbUpdateException)
                {
                    dbContext.ChangeTracker.Clear();
                    var duplicate = await FindBySourceEventIdAsync(
                        command.ElderId,
                        command.SourceEventId,
                        tracking: false,
                        cancellationToken);
                    return duplicate is null
                        ? Failure("PERSISTENCE_ERROR", "Correlated evidence could not be stored.")
                        : Success(duplicate, isDuplicate: true);
                }
            }
        }

        var careEvent = CareEvent.Create(
            Guid.NewGuid(),
            command.ElderId,
            classification.Category,
            classification.Level,
            command.Source,
            command.SourceEventId,
            command.Summary,
            command.OccurredAt,
            $"{areaResult.Value}:care",
            createdAt: now);
        careEvent.AddEvidence(
            Guid.NewGuid(),
            command.Trigger.ToString(),
            command.Summary,
            command.OccurredAt,
            now,
            command.SourceEventId);
        foreach (var action in escalationPolicy.GetDueActions(
                     careEvent.Level,
                     careEvent.CreatedAt,
                     now))
        {
            AddSimulationAttempt(careEvent, action, now);
        }
        dbContext.CareEvents.Add(careEvent);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return Success(careEvent, isDuplicate: false);
        }
        catch (DbUpdateException)
        {
            dbContext.ChangeTracker.Clear();
            var duplicate = await FindBySourceEventIdAsync(
                command.ElderId,
                command.SourceEventId,
                tracking: false,
                cancellationToken);
            return duplicate is null
                ? Failure("PERSISTENCE_ERROR", "Care event could not be stored.")
                : Success(duplicate, isDuplicate: true);
        }
    }

    public async Task<OperationResult<CareEventOperationResult>> AcceptAsync(
        Guid eventId,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        var careEvent = await LoadByIdAsync(eventId, tracking: true, cancellationToken);
        if (careEvent is null)
        {
            return Failure("NOT_FOUND", "Care event not found.");
        }
        if (actor.Role != DemoRole.CommunityStaff ||
            !await IsStaffInElderAreaAsync(actor, careEvent.ElderId, cancellationToken))
        {
            return Failure("FORBIDDEN_SCOPE", "Only community staff in the elder area can accept.");
        }

        var beforeIds = careEvent.Transitions.Select(item => item.Id).ToHashSet();
        var decision = careEvent.Accept(actor.UserId, timeProvider.GetUtcNow());
        if (!decision.IsAllowed)
        {
            return Failure(decision.ErrorCode!, decision.ErrorMessage!);
        }
        AddNewTransitions(careEvent, beforeIds);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Success(careEvent, isDuplicate: false);
    }

    public async Task<OperationResult<CareEventOperationResult>> TransitionAsync(
        Guid eventId,
        CareEventStatus target,
        string? reason,
        string? resolution,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        var careEvent = await LoadByIdAsync(eventId, tracking: true, cancellationToken);
        if (careEvent is null)
        {
            return Failure("NOT_FOUND", "Care event not found.");
        }
        if (actor.Role != DemoRole.CommunityStaff ||
            !await IsStaffInElderAreaAsync(actor, careEvent.ElderId, cancellationToken))
        {
            return Failure("FORBIDDEN_SCOPE", "Only community staff in the elder area can transition.");
        }
        if (careEvent.Status is not CareEventStatus.PendingConfirmation and
            not CareEventStatus.UnableToConfirm &&
            careEvent.CurrentOwnerUserId != actor.UserId)
        {
            return Failure("OWNER_REQUIRED", "Only the current owner can transition this event.");
        }

        var beforeIds = careEvent.Transitions.Select(item => item.Id).ToHashSet();
        var decision = target == CareEventStatus.Accepted
            ? careEvent.Accept(actor.UserId, timeProvider.GetUtcNow())
            : careEvent.TryTransition(
                target,
                CareEventActorKind.Staff,
                actor.UserId,
                reason,
                resolution,
                timeProvider.GetUtcNow());
        if (!decision.IsAllowed)
        {
            return Failure(decision.ErrorCode!, decision.ErrorMessage!);
        }
        AddNewTransitions(careEvent, beforeIds);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Success(careEvent, isDuplicate: false);
    }

    public async Task<OperationResult<CareEventOperationResult>> EscalateAsync(
        Guid eventId,
        EscalationAction action,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var careEvent = await LoadByIdAsync(eventId, tracking: true, cancellationToken);
        if (careEvent is null)
        {
            return Failure("NOT_FOUND", "Care event not found.");
        }
        if (CareEventStateMachine.IsTerminal(careEvent.Status))
        {
            return Success(careEvent, isDuplicate: true);
        }

        var transitionIds = careEvent.Transitions.Select(item => item.Id).ToHashSet();
        var attemptIds = careEvent.ContactAttempts.Select(item => item.Id).ToHashSet();
        if (action == EscalationAction.MarkUnableToConfirm &&
            careEvent.Status != CareEventStatus.UnableToConfirm)
        {
            var decision = careEvent.TryTransition(
                CareEventStatus.UnableToConfirm,
                CareEventActorKind.Background,
                actorUserId: null,
                "升级时限内仍未确认",
                resolution: null,
                now);
            if (!decision.IsAllowed)
            {
                return Failure(decision.ErrorCode!, decision.ErrorMessage!);
            }
        }
        else if (action != EscalationAction.MarkUnableToConfirm)
        {
            AddSimulationAttempt(careEvent, action, now);
            if (action == EscalationAction.Reassign)
            {
                careEvent.ClearOwnerForReassignment();
            }
        }

        AddNewTransitions(careEvent, transitionIds);
        foreach (var attempt in careEvent.ContactAttempts.Where(item => !attemptIds.Contains(item.Id)))
        {
            dbContext.ContactAttempts.Add(attempt);
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        return Success(careEvent, isDuplicate: false);
    }

    public async Task<OperationResult<CareEventOperationResult>> AddEvidenceAsync(
        Guid eventId,
        AddCareEventEvidenceCommand command,
        ActorContext? actor,
        CancellationToken cancellationToken)
    {
        var careEvent = await LoadByIdAsync(eventId, tracking: true, cancellationToken);
        if (careEvent is null)
        {
            return Failure("NOT_FOUND", "Care event not found.");
        }
        if (actor is not null && !await CanReadAsync(actor, careEvent.ElderId, cancellationToken))
        {
            return Failure("FORBIDDEN_SCOPE", "Care-event evidence scope denied.");
        }
        if (!string.IsNullOrWhiteSpace(command.SourceEventId))
        {
            var duplicate = careEvent.Evidence.Any(item => item.SourceEventId == command.SourceEventId);
            if (duplicate)
            {
                return Success(careEvent, isDuplicate: true);
            }
        }

        var evidenceId = Guid.NewGuid();
        careEvent.AddEvidence(
            evidenceId,
            command.Kind,
            command.Summary,
            command.OccurredAt,
            timeProvider.GetUtcNow(),
            command.SourceEventId);
        dbContext.CareEventEvidence.Add(careEvent.Evidence.Single(item => item.Id == evidenceId));
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return Success(careEvent, isDuplicate: false);
        }
        catch (DbUpdateException) when (!string.IsNullOrWhiteSpace(command.SourceEventId))
        {
            dbContext.ChangeTracker.Clear();
            var duplicate = await FindBySourceEventIdAsync(
                careEvent.ElderId,
                command.SourceEventId,
                tracking: false,
                cancellationToken);
            return duplicate is null
                ? Failure("PERSISTENCE_ERROR", "Care-event evidence could not be stored.")
                : Success(duplicate, isDuplicate: true);
        }
    }

    public async Task<IReadOnlyList<CareEvent>> ListAsync(
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        var events = await LoadAllAsync(tracking: false, cancellationToken);
        if (actor.Role == DemoRole.Administrator)
        {
            return events.OrderByDescending(item => item.LastActivityAt).ToList();
        }
        if (actor.Role is DemoRole.Elder or DemoRole.Family or DemoRole.ServiceWorker)
        {
            return events
                .Where(item => actor.ElderId == item.ElderId)
                .OrderByDescending(item => item.LastActivityAt)
                .ToList();
        }
        if (actor.Role == DemoRole.CommunityStaff)
        {
            var elderIds = (await dbContext.ElderProfiles.AsNoTracking()
                    .Where(profile => profile.AreaCode == actor.AreaCode)
                    .Select(profile => profile.Id)
                    .ToListAsync(cancellationToken))
                .ToHashSet();
            return events
                .Where(item => elderIds.Contains(item.ElderId))
                .OrderByDescending(item => item.LastActivityAt)
                .ToList();
        }
        return [];
    }

    public async Task<CareEvent?> GetAsync(
        Guid eventId,
        ActorContext actor,
        CancellationToken cancellationToken) =>
        (await ListAsync(actor, cancellationToken)).SingleOrDefault(item => item.Id == eventId);

    private async Task<OperationResult<string>> ResolveAreaAsync(
        Guid elderId,
        CancellationToken cancellationToken)
    {
        var area = await dbContext.ElderProfiles.AsNoTracking()
            .Where(profile => profile.Id == elderId)
            .Select(profile => profile.AreaCode)
            .SingleOrDefaultAsync(cancellationToken);
        return string.IsNullOrWhiteSpace(area)
            ? new(false, null, "NOT_FOUND", "Elder not found.")
            : new(true, area, null, null);
    }

    private static bool CanCreate(CreateCareEventCommand command, ActorContext? actor, string area)
    {
        if (actor is null)
        {
            return (command.ActorKind is CareEventActorKind.Background or
                    CareEventActorKind.Device or
                    CareEventActorKind.Ai) &&
                (command.Source is CareEventSource.CheckIn or
                    CareEventSource.Device or
                    CareEventSource.AiCue);
        }

        return actor.Role switch
        {
            DemoRole.Elder =>
                actor.ElderId == command.ElderId && command.Source == CareEventSource.ElderHelp,
            DemoRole.Family =>
                actor.ElderId == command.ElderId && command.Source == CareEventSource.FamilyReport,
            DemoRole.CommunityStaff =>
                actor.AreaCode == area && command.Source == CareEventSource.StaffVisit,
            _ => false,
        };
    }

    private async Task<bool> CanReadAsync(
        ActorContext actor,
        Guid elderId,
        CancellationToken cancellationToken) =>
        actor.Role == DemoRole.Administrator ||
        (actor.Role is DemoRole.Elder or DemoRole.Family or DemoRole.ServiceWorker &&
         actor.ElderId == elderId) ||
        (actor.Role == DemoRole.CommunityStaff &&
         await IsStaffInElderAreaAsync(actor, elderId, cancellationToken));

    private Task<bool> IsStaffInElderAreaAsync(
        ActorContext actor,
        Guid elderId,
        CancellationToken cancellationToken) =>
        dbContext.ElderProfiles.AsNoTracking().AnyAsync(
            profile => profile.Id == elderId && profile.AreaCode == actor.AreaCode,
            cancellationToken);

    private Task<CareEvent?> LoadByIdAsync(
        Guid eventId,
        bool tracking,
        CancellationToken cancellationToken) =>
        WithGraph(tracking).SingleOrDefaultAsync(item => item.Id == eventId, cancellationToken);

    private async Task<CareEvent?> FindBySourceEventIdAsync(
        Guid elderId,
        string sourceEventId,
        bool tracking,
        CancellationToken cancellationToken)
    {
        var root = await WithGraph(tracking).SingleOrDefaultAsync(
            item => item.ElderId == elderId && item.SourceEventId == sourceEventId,
            cancellationToken);
        if (root is not null)
        {
            return root;
        }

        var linkedId = await dbContext.CareEventEvidence.AsNoTracking()
            .Where(item => item.SourceEventId == sourceEventId)
            .Join(
                dbContext.CareEvents.AsNoTracking().Where(item => item.ElderId == elderId),
                evidence => evidence.CareEventId,
                careEvent => careEvent.Id,
                (evidence, _) => (Guid?)evidence.CareEventId)
            .FirstOrDefaultAsync(cancellationToken);
        return linkedId is null
            ? null
            : await LoadByIdAsync(linkedId.Value, tracking, cancellationToken);
    }

    private Task<List<CareEvent>> LoadForElderAsync(
        Guid elderId,
        bool tracking,
        CancellationToken cancellationToken) =>
        WithGraph(tracking)
            .Where(item => item.ElderId == elderId)
            .ToListAsync(cancellationToken);

    private Task<List<CareEvent>> LoadAllAsync(
        bool tracking,
        CancellationToken cancellationToken) =>
        WithGraph(tracking).ToListAsync(cancellationToken);

    private IQueryable<CareEvent> WithGraph(bool tracking)
    {
        var query = dbContext.CareEvents
            .Include(item => item.Evidence)
            .Include(item => item.Transitions)
            .Include(item => item.ContactAttempts)
            .AsSplitQuery();
        return tracking ? query : query.AsNoTracking();
    }

    private void AddNewTransitions(CareEvent careEvent, IReadOnlySet<Guid> beforeIds)
    {
        foreach (var transition in careEvent.Transitions.Where(item => !beforeIds.Contains(item.Id)))
        {
            dbContext.CareEventTransitions.Add(transition);
        }
    }

    private static void AddSimulationAttempt(
        CareEvent careEvent,
        EscalationAction action,
        DateTimeOffset attemptedAt)
    {
        var mapping = action switch
        {
            EscalationAction.ElderReminder =>
                (ContactAttemptKind.ElderReminder, "老人端", "已生成模拟提醒记录"),
            EscalationAction.PhoneConfirmationAttempt =>
                (ContactAttemptKind.PhoneConfirmation, "老人电话", "已生成模拟电话确认记录"),
            EscalationAction.EmergencyContactAttempt =>
                (ContactAttemptKind.EmergencyContact, "紧急联系人", "已生成模拟联系人通知记录"),
            EscalationAction.CommunityNotification =>
                (ContactAttemptKind.CommunityNotification, "社区照料队列", "已生成模拟社区通知记录"),
            EscalationAction.Reassign =>
                (ContactAttemptKind.Reassignment, "社区照料队列", "已生成模拟重新分派记录"),
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, "Action has no contact attempt."),
        };
        careEvent.AddContactAttempt(
            Guid.NewGuid(),
            $"{careEvent.Id:N}:{action}",
            mapping.Item1,
            mapping.Item2,
            attemptedAt,
            mapping.Item3);
    }

    private static OperationResult<CareEventOperationResult> Success(
        CareEvent careEvent,
        bool isDuplicate) =>
        new(true, new(careEvent, isDuplicate), null, null);

    private static OperationResult<CareEventOperationResult> Failure(string code, string message) =>
        new(false, null, code, message);
}
