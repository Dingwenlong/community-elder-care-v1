using CommunityElderCare.Core.Identity;

namespace CommunityElderCare.Api.Contracts.Family;

public sealed record FamilySummaryResponse(
    string ElderDisplayName,
    IReadOnlyList<ConsentField> GrantedFields,
    DateTimeOffset ConsentExpiresAt,
    string? RecentStatus,
    string? ReminderSummary,
    string? CareProgress,
    string? VisitSummary,
    string? LastCommunityConfirmation);

public sealed record FamilyCareRecordResponse(
    DateTimeOffset OccurredAt,
    string Kind,
    string Summary,
    bool IsDemoData = true);

public sealed record FamilyCareEventResponse(
    Guid Id,
    string Source,
    string Level,
    string Status,
    string Summary,
    bool IsDuplicate);
