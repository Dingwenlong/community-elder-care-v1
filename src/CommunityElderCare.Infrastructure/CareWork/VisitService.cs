using CommunityElderCare.Core.CareEvents;
using CommunityElderCare.Core.CareWork;
using CommunityElderCare.Core.Common;
using CommunityElderCare.Core.Identity;
using CommunityElderCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommunityElderCare.Infrastructure.CareWork;

public sealed class VisitService(
    CommunityCareDbContext dbContext,
    TimeProvider timeProvider) : IVisitService
{
    public async Task<OperationResult<VisitTask>> CreateAsync(
        CreateVisitCommand command,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        var careEvent = await LoadEventAsync(command.CareEventId, cancellationToken);
        if (careEvent is null)
        {
            return Failure<VisitTask>("NOT_FOUND", "Care event not found.");
        }
        if (!IsCurrentCommunityOwner(actor, careEvent))
        {
            return Failure<VisitTask>("FORBIDDEN_SCOPE", "Only the current community owner can create a visit.");
        }
        if (careEvent.Status is not CareEventStatus.Accepted and not CareEventStatus.InProgress)
        {
            return Failure<VisitTask>("INVALID_EVENT_STATUS", "Visit requires an accepted or in-progress event.");
        }
        var validAssignee = await dbContext.UserAccounts.AsNoTracking().AnyAsync(
            account =>
                account.Id == command.AssignedStaffUserId &&
                account.Role == DemoRole.CommunityStaff,
            cancellationToken);
        if (!validAssignee)
        {
            return Failure<VisitTask>("INVALID_ASSIGNEE", "Visit assignee must be community staff.");
        }

        var now = timeProvider.GetUtcNow();
        var create = VisitTask.Create(
            Guid.NewGuid(),
            careEvent.Id,
            careEvent.ElderId,
            command.AssignedStaffUserId,
            command.ScheduledStartAt,
            command.ScheduledEndAt,
            command.IsMandatory,
            now);
        if (!create.IsSuccess)
        {
            return create;
        }

        var visit = create.Value!;
        dbContext.VisitTasks.Add(visit);
        if (visit.IsMandatory)
        {
            careEvent.SetWorkState(
                hasIncompleteMandatoryTask: true,
                careEvent.RequiresFollowUp,
                careEvent.IsFollowUpCompleted);
        }
        AddEvidence(
            careEvent,
            "VisitScheduled",
            $"已安排演示探访：{visit.ScheduledStartAt:yyyy-MM-dd HH:mm}",
            now,
            $"visit:{visit.Id:N}:scheduled");
        await dbContext.SaveChangesAsync(cancellationToken);
        return Success(visit);
    }

    public async Task<OperationResult<VisitTask>> StartAsync(
        Guid visitId,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        var visit = await dbContext.VisitTasks.SingleOrDefaultAsync(
            item => item.Id == visitId,
            cancellationToken);
        if (visit is null)
        {
            return Failure<VisitTask>("NOT_FOUND", "Visit not found.");
        }
        var careEvent = await LoadEventAsync(visit.CareEventId, cancellationToken);
        if (careEvent is null)
        {
            return Failure<VisitTask>("NOT_FOUND", "Care event not found.");
        }
        var now = timeProvider.GetUtcNow();
        var start = visit.Start(actor, now);
        if (!start.IsSuccess)
        {
            return start;
        }

        if (careEvent.Status == CareEventStatus.Accepted)
        {
            var transition = careEvent.TryTransition(
                CareEventStatus.InProgress,
                CareEventActorKind.Staff,
                actor.UserId,
                "开始上门探访",
                resolution: null,
                now);
            if (!transition.IsAllowed)
            {
                return Failure<VisitTask>(transition.ErrorCode!, transition.ErrorMessage!);
            }
            AddLatestTransition(careEvent);
        }
        else if (careEvent.Status != CareEventStatus.InProgress)
        {
            return Failure<VisitTask>("INVALID_EVENT_STATUS", "Visit cannot start for this event status.");
        }

        AddEvidence(
            careEvent,
            "VisitStarted",
            "工作人员已开始演示探访",
            now,
            $"visit:{visit.Id:N}:started");
        await dbContext.SaveChangesAsync(cancellationToken);
        return Success(visit);
    }

    public async Task<OperationResult<VisitTask>> CompleteAsync(
        Guid visitId,
        CompleteVisitCommand command,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        var visit = await dbContext.VisitTasks.SingleOrDefaultAsync(
            item => item.Id == visitId,
            cancellationToken);
        if (visit is null)
        {
            return Failure<VisitTask>("NOT_FOUND", "Visit not found.");
        }
        var careEvent = await LoadEventAsync(visit.CareEventId, cancellationToken);
        if (careEvent is null)
        {
            return Failure<VisitTask>("NOT_FOUND", "Care event not found.");
        }
        if (careEvent.Status != CareEventStatus.InProgress)
        {
            return Failure<VisitTask>("INVALID_EVENT_STATUS", "Visit completion requires an in-progress event.");
        }

        var now = timeProvider.GetUtcNow();
        var complete = visit.Complete(
            actor,
            command.RawStaffNote,
            command.ConfirmedSummary,
            command.Result,
            now);
        if (!complete.IsSuccess)
        {
            return complete;
        }

        await RefreshMandatoryWorkStateAsync(careEvent, cancellationToken);
        AddEvidence(
            careEvent,
            "VisitCompleted",
            visit.ConfirmedSummary!,
            now,
            $"visit:{visit.Id:N}:completed");
        await dbContext.SaveChangesAsync(cancellationToken);
        return Success(visit);
    }

    public async Task<OperationResult<FollowUp>> CreateFollowUpAsync(
        CreateFollowUpCommand command,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        var careEvent = await LoadEventAsync(command.CareEventId, cancellationToken);
        if (careEvent is null)
        {
            return Failure<FollowUp>("NOT_FOUND", "Care event not found.");
        }
        if (!IsCurrentCommunityOwner(actor, careEvent))
        {
            return Failure<FollowUp>("FORBIDDEN_SCOPE", "Only the current community owner can create follow-up.");
        }
        if (careEvent.Status != CareEventStatus.Resolved)
        {
            return Failure<FollowUp>("INVALID_EVENT_STATUS", "Follow-up can only be created for a resolved event.");
        }
        var validAssignee = await dbContext.UserAccounts.AsNoTracking().AnyAsync(
            account =>
                account.Id == command.AssignedStaffUserId &&
                account.Role == DemoRole.CommunityStaff,
            cancellationToken);
        if (!validAssignee)
        {
            return Failure<FollowUp>("INVALID_ASSIGNEE", "Follow-up assignee must be community staff.");
        }

        var now = timeProvider.GetUtcNow();
        var create = FollowUp.Create(
            Guid.NewGuid(),
            careEvent.Id,
            careEvent.ElderId,
            command.AssignedStaffUserId,
            command.DueAt,
            now);
        if (!create.IsSuccess)
        {
            return create;
        }

        var transition = careEvent.TryTransition(
            CareEventStatus.FollowUpPending,
            CareEventActorKind.Staff,
            actor.UserId,
            "已安排随访",
            resolution: null,
            now);
        if (!transition.IsAllowed)
        {
            return Failure<FollowUp>(transition.ErrorCode!, transition.ErrorMessage!);
        }

        var followUp = create.Value!;
        dbContext.FollowUps.Add(followUp);
        careEvent.SetWorkState(
            careEvent.HasIncompleteMandatoryTask,
            requiresFollowUp: true,
            isFollowUpCompleted: false);
        AddLatestTransition(careEvent);
        AddEvidence(
            careEvent,
            "FollowUpScheduled",
            $"已安排演示随访：{followUp.DueAt:yyyy-MM-dd HH:mm}",
            now,
            $"follow-up:{followUp.Id:N}:scheduled");
        await dbContext.SaveChangesAsync(cancellationToken);
        return Success(followUp);
    }

    public async Task<OperationResult<FollowUp>> CompleteFollowUpAsync(
        Guid followUpId,
        string result,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        var followUp = await dbContext.FollowUps.SingleOrDefaultAsync(
            item => item.Id == followUpId,
            cancellationToken);
        if (followUp is null)
        {
            return Failure<FollowUp>("NOT_FOUND", "Follow-up not found.");
        }
        var careEvent = await LoadEventAsync(followUp.CareEventId, cancellationToken);
        if (careEvent is null)
        {
            return Failure<FollowUp>("NOT_FOUND", "Care event not found.");
        }
        if (careEvent.Status != CareEventStatus.FollowUpPending)
        {
            return Failure<FollowUp>("INVALID_EVENT_STATUS", "Event is not waiting for follow-up.");
        }

        var now = timeProvider.GetUtcNow();
        var complete = followUp.Complete(actor, result, now);
        if (!complete.IsSuccess)
        {
            return complete;
        }

        var followUps = await dbContext.FollowUps
            .Where(item => item.CareEventId == careEvent.Id)
            .ToListAsync(cancellationToken);
        var allCompleted = followUps.All(item => item.Status == WorkStatus.Completed);
        careEvent.SetWorkState(
            careEvent.HasIncompleteMandatoryTask,
            requiresFollowUp: true,
            isFollowUpCompleted: allCompleted);
        AddEvidence(
            careEvent,
            "FollowUpCompleted",
            followUp.Result!,
            now,
            $"follow-up:{followUp.Id:N}:completed");
        await dbContext.SaveChangesAsync(cancellationToken);
        return Success(followUp);
    }

    private async Task RefreshMandatoryWorkStateAsync(
        CareEvent careEvent,
        CancellationToken cancellationToken)
    {
        var visits = await dbContext.VisitTasks
            .Where(item => item.CareEventId == careEvent.Id && item.IsMandatory)
            .ToListAsync(cancellationToken);
        var orders = await dbContext.ServiceOrders
            .Where(item => item.CareEventId == careEvent.Id && item.IsMandatory)
            .ToListAsync(cancellationToken);
        var incomplete = visits.Any(item => item.Status is not WorkStatus.Completed and not WorkStatus.Cancelled) ||
            orders.Any(item => item.Status is not WorkStatus.Completed and not WorkStatus.Cancelled);
        careEvent.SetWorkState(
            incomplete,
            careEvent.RequiresFollowUp,
            careEvent.IsFollowUpCompleted);
    }

    private Task<CareEvent?> LoadEventAsync(Guid eventId, CancellationToken cancellationToken) =>
        dbContext.CareEvents
            .Include(item => item.Evidence)
            .Include(item => item.Transitions)
            .AsSplitQuery()
            .SingleOrDefaultAsync(item => item.Id == eventId, cancellationToken);

    private static bool IsCurrentCommunityOwner(ActorContext actor, CareEvent careEvent) =>
        actor.Role == DemoRole.CommunityStaff && careEvent.CurrentOwnerUserId == actor.UserId;

    private void AddEvidence(
        CareEvent careEvent,
        string kind,
        string summary,
        DateTimeOffset now,
        string sourceEventId)
    {
        var id = Guid.NewGuid();
        careEvent.AddEvidence(id, kind, summary, now, now, sourceEventId);
        dbContext.CareEventEvidence.Add(careEvent.Evidence.Single(item => item.Id == id));
    }

    private void AddLatestTransition(CareEvent careEvent)
    {
        var transition = careEvent.Transitions.Last();
        dbContext.CareEventTransitions.Add(transition);
    }

    private static OperationResult<T> Success<T>(T value) => new(true, value, null, null);

    private static OperationResult<T> Failure<T>(string code, string message) =>
        new(false, default, code, message);
}
