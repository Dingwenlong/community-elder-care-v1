using CommunityElderCare.Core.CareWork;

namespace CommunityElderCare.Api.Contracts.CareWork;

public sealed record CreateVisitRequest(
    Guid AssignedStaffUserId,
    DateTimeOffset ScheduledStartAt,
    DateTimeOffset ScheduledEndAt,
    bool IsMandatory);

public sealed record CompleteVisitRequest(
    string RawStaffNote,
    string ConfirmedSummary,
    string Result);

public sealed record VisitResponse(
    Guid VisitId,
    Guid CareEventId,
    Guid AssignedStaffUserId,
    DateTimeOffset ScheduledStartAt,
    DateTimeOffset ScheduledEndAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    string? ConfirmedSummary,
    string? Result,
    WorkStatus Status,
    bool IsMandatory,
    bool IsDemoData);

public sealed record CommunityVisitResponse(
    Guid VisitId,
    Guid CareEventId,
    string ElderDisplayName,
    Guid AssignedStaffUserId,
    DateTimeOffset ScheduledStartAt,
    DateTimeOffset ScheduledEndAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    string? ConfirmedSummary,
    string? Result,
    WorkStatus Status,
    bool IsMandatory,
    bool IsDemoData);

public sealed record CreateServiceOrderRequest(
    string ServiceType,
    string ScheduledWindow,
    string ContactInstruction,
    Guid AssignedWorkerUserId,
    bool IsMandatory,
    DateTimeOffset? DueAt = null);

public sealed record CompleteServiceOrderRequest(string Result);

public sealed record ServiceWorkerOrderResponse(
    Guid OrderId,
    string ElderDisplayName,
    string ServiceType,
    string ScheduledWindow,
    string ContactInstruction,
    WorkStatus Status,
    DateTimeOffset? DueAt = null);

public sealed record CommunityServiceOrderResponse(
    Guid OrderId,
    Guid CareEventId,
    string ElderDisplayName,
    string ServiceType,
    string ScheduledWindow,
    string ContactInstruction,
    WorkStatus Status,
    string? Result,
    bool IsMandatory,
    bool IsDemoData,
    DateTimeOffset? DueAt = null);

public sealed record CreateFollowUpRequest(
    Guid AssignedStaffUserId,
    DateTimeOffset DueAt);

public sealed record CompleteFollowUpRequest(string Result);

public sealed record FollowUpResponse(
    Guid FollowUpId,
    Guid CareEventId,
    Guid AssignedStaffUserId,
    DateTimeOffset DueAt,
    DateTimeOffset? CompletedAt,
    string? Result,
    WorkStatus Status,
    bool IsDemoData);

public sealed record CommunityFollowUpResponse(
    Guid FollowUpId,
    Guid CareEventId,
    string ElderDisplayName,
    Guid AssignedStaffUserId,
    DateTimeOffset DueAt,
    DateTimeOffset? CompletedAt,
    string? Result,
    WorkStatus Status,
    bool IsDemoData);
