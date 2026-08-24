namespace CommunityElderCare.Api.Contracts.CheckIns;

public sealed record TodayResponse(
    Guid ElderId,
    DateTimeOffset ServerTime,
    bool IsDemoData,
    IReadOnlyList<TodayCheckInResponse> CheckIns,
    IReadOnlyList<TodayReminderResponse> Reminders);

public sealed record TodayCheckInResponse(
    Guid Id,
    Guid RequestId,
    DateTimeOffset ClientTime,
    DateTimeOffset ReceivedAt,
    string Kind);

public sealed record TodayReminderResponse(
    Guid Id,
    string Type,
    string DemoLabel,
    DateTimeOffset DueAt,
    DateTimeOffset NextDueAt,
    string State,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? SnoozedAt);
