using CommunityElderCare.Core.CheckIns;
using CommunityElderCare.Core.Common;
using CommunityElderCare.Core.Identity;
using CommunityElderCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommunityElderCare.Infrastructure.CheckIns;

public sealed class CheckInService(
    CommunityCareDbContext dbContext,
    TimeProvider timeProvider) : ICheckInService
{
    public Task<OperationResult<CheckInResult>> RecordAsync(
        Guid elderId,
        Guid requestId,
        DateTimeOffset clientTime,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        if (actor.Role != DemoRole.Elder || actor.ElderId != elderId)
        {
            return Task.FromResult(Failure<CheckInResult>("FORBIDDEN_SCOPE", "Only the elder can self check in."));
        }

        return RecordCoreAsync(
            elderId,
            requestId,
            clientTime,
            CheckInKind.ElderSelf,
            actor.UserId,
            manualReason: null,
            cancellationToken);
    }

    public async Task<OperationResult<CheckInResult>> RecordManualAsync(
        Guid elderId,
        Guid requestId,
        DateTimeOffset clientTime,
        string reason,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        if (actor.Role != DemoRole.CommunityStaff)
        {
            return Failure<CheckInResult>("FORBIDDEN_SCOPE", "Only community staff can confirm manually.");
        }
        if (string.IsNullOrWhiteSpace(reason))
        {
            return Failure<CheckInResult>("REASON_REQUIRED", "A manual-confirmation reason is required.");
        }
        var inArea = await dbContext.ElderProfiles.AsNoTracking().AnyAsync(
            profile => profile.Id == elderId && profile.AreaCode == actor.AreaCode,
            cancellationToken);
        if (!inArea)
        {
            return Failure<CheckInResult>("FORBIDDEN_SCOPE", "The elder is outside the staff area.");
        }

        return await RecordCoreAsync(
            elderId,
            requestId,
            clientTime,
            CheckInKind.CommunityManual,
            actor.UserId,
            reason.Trim(),
            cancellationToken);
    }

    public async Task<OperationResult<ReminderActionResult>> CompleteReminderAsync(
        Guid reminderId,
        Guid requestId,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        var reminder = await dbContext.Reminders.SingleOrDefaultAsync(
            candidate => candidate.Id == reminderId,
            cancellationToken);
        if (reminder is null)
        {
            return Failure<ReminderActionResult>("NOT_FOUND", "Reminder not found.");
        }
        if (!await CanActForElderAsync(actor, reminder.ElderId, cancellationToken))
        {
            return Failure<ReminderActionResult>("FORBIDDEN_SCOPE", "Reminder scope denied.");
        }

        const string kind = "REMINDER_COMPLETE";
        var existing = await FindIdempotencyAsync(reminder.ElderId, requestId, kind, cancellationToken);
        if (existing is not null)
        {
            return Success(ToReminderResult(reminder, requestId, isDuplicate: true));
        }

        var now = timeProvider.GetUtcNow();
        reminder.Complete(now, actor.UserId);
        dbContext.IdempotencyRecords.Add(new IdempotencyRecord(
            Guid.NewGuid(), reminder.ElderId, requestId, kind, reminder.Id, now));
        await dbContext.SaveChangesAsync(cancellationToken);
        return Success(ToReminderResult(reminder, requestId, isDuplicate: false));
    }

    public async Task<OperationResult<ReminderActionResult>> SnoozeReminderAsync(
        Guid reminderId,
        Guid requestId,
        DateTimeOffset nextReminderAt,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        var reminder = await dbContext.Reminders.SingleOrDefaultAsync(
            candidate => candidate.Id == reminderId,
            cancellationToken);
        if (reminder is null)
        {
            return Failure<ReminderActionResult>("NOT_FOUND", "Reminder not found.");
        }
        if (!await CanActForElderAsync(actor, reminder.ElderId, cancellationToken))
        {
            return Failure<ReminderActionResult>("FORBIDDEN_SCOPE", "Reminder scope denied.");
        }

        const string kind = "REMINDER_SNOOZE";
        var existing = await FindIdempotencyAsync(reminder.ElderId, requestId, kind, cancellationToken);
        if (existing is not null)
        {
            return Success(ToReminderResult(reminder, requestId, isDuplicate: true));
        }

        var now = timeProvider.GetUtcNow();
        try
        {
            reminder.Snooze(now, nextReminderAt, actor.UserId);
        }
        catch (ArgumentOutOfRangeException)
        {
            return Failure<ReminderActionResult>(
                "INVALID_SNOOZE_TIME",
                "Snooze time must be between five minutes and twenty-four hours.");
        }
        catch (InvalidOperationException)
        {
            return Failure<ReminderActionResult>("REMINDER_COMPLETED", "Completed reminder cannot be snoozed.");
        }

        dbContext.IdempotencyRecords.Add(new IdempotencyRecord(
            Guid.NewGuid(), reminder.ElderId, requestId, kind, reminder.Id, now));
        await dbContext.SaveChangesAsync(cancellationToken);
        return Success(ToReminderResult(reminder, requestId, isDuplicate: false));
    }

    public async Task<TodaySnapshot> GetTodayAsync(
        Guid elderId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var start = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, now.Offset);
        var end = start.AddDays(1);
        var checkIns = await dbContext.CheckIns.AsNoTracking()
            .Where(checkIn => checkIn.ElderId == elderId)
            .ToListAsync(cancellationToken);
        var reminders = await dbContext.Reminders.AsNoTracking()
            .Where(reminder => reminder.ElderId == elderId)
            .ToListAsync(cancellationToken);

        return new TodaySnapshot(
            now,
            checkIns
                .Where(checkIn => checkIn.ReceivedAt >= start && checkIn.ReceivedAt < end)
                .OrderByDescending(checkIn => checkIn.ReceivedAt)
                .ToList(),
            reminders
                .Where(reminder => reminder.NextDueAt >= start && reminder.NextDueAt < end)
                .OrderBy(reminder => reminder.NextDueAt)
                .ToList());
    }

    public async Task<IReadOnlyList<OverdueCheckIn>> GetOverdueCheckInsAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var profiles = await dbContext.ElderProfiles.AsNoTracking().ToListAsync(cancellationToken);
        var checkIns = await dbContext.CheckIns.AsNoTracking().ToListAsync(cancellationToken);
        return profiles
            .Where(profile =>
                profile.NextCheckInDueAt < now &&
                !checkIns.Any(checkIn =>
                    checkIn.ElderId == profile.Id &&
                    checkIn.ReceivedAt >= profile.NextCheckInDueAt))
            .Select(profile => new OverdueCheckIn(profile.Id, profile.NextCheckInDueAt))
            .OrderBy(item => item.DueAt)
            .ToList();
    }

    private async Task<OperationResult<CheckInResult>> RecordCoreAsync(
        Guid elderId,
        Guid requestId,
        DateTimeOffset clientTime,
        CheckInKind kind,
        Guid actorUserId,
        string? manualReason,
        CancellationToken cancellationToken)
    {
        var existing = await FindCheckInAsync(elderId, requestId, kind, cancellationToken);
        if (existing is not null)
        {
            return Success(ToCheckInResult(existing, isDuplicate: true));
        }

        var now = timeProvider.GetUtcNow();
        var checkIn = CheckIn.Create(
            Guid.NewGuid(), elderId, requestId, clientTime, now, kind, actorUserId, manualReason);
        dbContext.CheckIns.Add(checkIn);
        dbContext.IdempotencyRecords.Add(new IdempotencyRecord(
            Guid.NewGuid(), elderId, requestId, $"CHECK_IN_{kind}", checkIn.Id, now));

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return Success(ToCheckInResult(checkIn, isDuplicate: false));
        }
        catch (DbUpdateException)
        {
            dbContext.ChangeTracker.Clear();
            var duplicate = await FindCheckInAsync(elderId, requestId, kind, cancellationToken);
            return duplicate is null
                ? Failure<CheckInResult>("PERSISTENCE_ERROR", "Check-in could not be stored.")
                : Success(ToCheckInResult(duplicate, isDuplicate: true));
        }
    }

    private Task<CheckIn?> FindCheckInAsync(
        Guid elderId,
        Guid requestId,
        CheckInKind kind,
        CancellationToken cancellationToken) =>
        dbContext.CheckIns.AsNoTracking().SingleOrDefaultAsync(
            checkIn =>
                checkIn.ElderId == elderId &&
                checkIn.RequestId == requestId &&
                checkIn.Kind == kind,
            cancellationToken);

    private Task<IdempotencyRecord?> FindIdempotencyAsync(
        Guid elderId,
        Guid requestId,
        string kind,
        CancellationToken cancellationToken) =>
        dbContext.IdempotencyRecords.AsNoTracking().SingleOrDefaultAsync(
            record =>
                record.ElderId == elderId &&
                record.RequestId == requestId &&
                record.Kind == kind,
            cancellationToken);

    private async Task<bool> CanActForElderAsync(
        ActorContext actor,
        Guid elderId,
        CancellationToken cancellationToken)
    {
        if (actor.Role == DemoRole.Elder)
        {
            return actor.ElderId == elderId;
        }
        if (actor.Role != DemoRole.CommunityStaff)
        {
            return false;
        }

        return await dbContext.ElderProfiles.AsNoTracking().AnyAsync(
            profile => profile.Id == elderId && profile.AreaCode == actor.AreaCode,
            cancellationToken);
    }

    private static CheckInResult ToCheckInResult(CheckIn checkIn, bool isDuplicate) => new(
        checkIn.Id,
        checkIn.RequestId,
        checkIn.ClientTime,
        checkIn.ReceivedAt,
        checkIn.Kind,
        isDuplicate);

    private static ReminderActionResult ToReminderResult(
        Reminder reminder,
        Guid requestId,
        bool isDuplicate) => new(
        reminder.Id,
        requestId,
        reminder.CompletedAt,
        reminder.NextDueAt,
        isDuplicate);

    private static OperationResult<T> Success<T>(T value) => new(true, value, null, null);

    private static OperationResult<T> Failure<T>(string code, string message) =>
        new(false, default, code, message);
}
