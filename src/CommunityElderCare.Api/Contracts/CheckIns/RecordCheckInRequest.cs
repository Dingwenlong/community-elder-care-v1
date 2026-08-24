namespace CommunityElderCare.Api.Contracts.CheckIns;

public sealed record RecordCheckInRequest(Guid RequestId, DateTimeOffset ClientTime);

public sealed record CheckInResponse(
    Guid Id,
    Guid RequestId,
    DateTimeOffset ClientTime,
    DateTimeOffset ReceivedAt,
    string Kind,
    bool IsDuplicate);

public sealed record ReminderActionRequest(Guid RequestId);

public sealed record SnoozeReminderRequest(Guid RequestId, DateTimeOffset NextReminderAt);

public sealed record ReminderActionResponse(
    Guid ReminderId,
    Guid RequestId,
    DateTimeOffset? CompletedAt,
    DateTimeOffset NextDueAt,
    bool IsDuplicate);
