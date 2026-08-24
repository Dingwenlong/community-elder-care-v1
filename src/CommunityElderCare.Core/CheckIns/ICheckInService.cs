using CommunityElderCare.Core.Common;
using CommunityElderCare.Core.Identity;

namespace CommunityElderCare.Core.CheckIns;

public interface ICheckInService
{
    Task<OperationResult<CheckInResult>> RecordAsync(
        Guid elderId,
        Guid requestId,
        DateTimeOffset clientTime,
        ActorContext actor,
        CancellationToken cancellationToken);

    Task<OperationResult<CheckInResult>> RecordManualAsync(
        Guid elderId,
        Guid requestId,
        DateTimeOffset clientTime,
        string reason,
        ActorContext actor,
        CancellationToken cancellationToken);

    Task<OperationResult<ReminderActionResult>> CompleteReminderAsync(
        Guid reminderId,
        Guid requestId,
        ActorContext actor,
        CancellationToken cancellationToken);

    Task<OperationResult<ReminderActionResult>> SnoozeReminderAsync(
        Guid reminderId,
        Guid requestId,
        DateTimeOffset nextReminderAt,
        ActorContext actor,
        CancellationToken cancellationToken);

    Task<TodaySnapshot> GetTodayAsync(
        Guid elderId,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<OverdueCheckIn>> GetOverdueCheckInsAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken);
}

public sealed record CheckInResult(
    Guid Id,
    Guid RequestId,
    DateTimeOffset ClientTime,
    DateTimeOffset ReceivedAt,
    CheckInKind Kind,
    bool IsDuplicate);

public sealed record ReminderActionResult(
    Guid ReminderId,
    Guid RequestId,
    DateTimeOffset? CompletedAt,
    DateTimeOffset NextDueAt,
    bool IsDuplicate);

public sealed record TodaySnapshot(
    DateTimeOffset ServerTime,
    IReadOnlyList<CheckIn> CheckIns,
    IReadOnlyList<Reminder> Reminders);

public sealed record OverdueCheckIn(Guid ElderId, DateTimeOffset DueAt);
