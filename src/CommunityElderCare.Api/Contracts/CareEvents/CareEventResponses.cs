using CommunityElderCare.Core.CareEvents;

namespace CommunityElderCare.Api.Contracts.CareEvents;

public sealed record CreateCareEventRequest(
    Guid ClientRequestId,
    Guid ElderId,
    CareEventTrigger? Trigger,
    string Summary,
    DateTimeOffset OccurredAt);

public sealed record CareEventTransitionRequest(
    CareEventStatus ToStatus,
    string? Reason,
    string? Resolution);

public sealed record CareEventResponse(
    Guid Id,
    Guid ElderId,
    CareEventCategory Category,
    CareEventLevel Level,
    CareEventStatus Status,
    CareEventSource Source,
    string Summary,
    DateTimeOffset OccurredAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastActivityAt,
    string ResponsibilityQueue,
    Guid? CurrentOwnerUserId,
    string? Resolution,
    bool IsDemoData,
    bool IsDuplicate,
    IReadOnlyList<CareEventEvidenceResponse> Evidence,
    IReadOnlyList<CareEventTransitionResponse> Transitions,
    IReadOnlyList<ContactAttemptResponse> ContactAttempts,
    IReadOnlyCollection<CareEventStatus> AllowedTransitions);

public sealed record CareEventEvidenceResponse(
    Guid Id,
    string Kind,
    string Summary,
    DateTimeOffset OccurredAt,
    DateTimeOffset RecordedAt,
    bool IsSimulation);

public sealed record CareEventTransitionResponse(
    Guid Id,
    CareEventStatus FromStatus,
    CareEventStatus ToStatus,
    CareEventActorKind ActorKind,
    Guid? ActorUserId,
    string? Reason,
    DateTimeOffset OccurredAt,
    bool IsSimulation);

public sealed record ContactAttemptResponse(
    Guid Id,
    ContactAttemptKind Kind,
    string TargetLabel,
    DateTimeOffset AttemptedAt,
    string Outcome,
    bool IsSimulation);
