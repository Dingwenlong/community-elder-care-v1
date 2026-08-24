using CommunityElderCare.Core.Common;
using CommunityElderCare.Core.Identity;

namespace CommunityElderCare.Core.CareEvents;

public sealed record CreateCareEventCommand(
    Guid ElderId,
    CareEventTrigger Trigger,
    CareEventSource Source,
    string SourceEventId,
    string Summary,
    DateTimeOffset OccurredAt,
    CareEventActorKind ActorKind);

public sealed record CareEventOperationResult(
    CareEvent CareEvent,
    bool IsDuplicate);

public sealed record AddCareEventEvidenceCommand(
    string Kind,
    string Summary,
    DateTimeOffset OccurredAt,
    string? SourceEventId);

public interface ICareEventService
{
    Task<OperationResult<CareEventOperationResult>> CreateAsync(
        CreateCareEventCommand command,
        ActorContext? actor,
        CancellationToken cancellationToken);

    Task<OperationResult<CareEventOperationResult>> AcceptAsync(
        Guid eventId,
        ActorContext actor,
        CancellationToken cancellationToken);

    Task<OperationResult<CareEventOperationResult>> TransitionAsync(
        Guid eventId,
        CareEventStatus target,
        string? reason,
        string? resolution,
        ActorContext actor,
        CancellationToken cancellationToken);

    Task<OperationResult<CareEventOperationResult>> EscalateAsync(
        Guid eventId,
        EscalationAction action,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<OperationResult<CareEventOperationResult>> AddEvidenceAsync(
        Guid eventId,
        AddCareEventEvidenceCommand command,
        ActorContext? actor,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<CareEvent>> ListAsync(
        ActorContext actor,
        CancellationToken cancellationToken);

    Task<CareEvent?> GetAsync(
        Guid eventId,
        ActorContext actor,
        CancellationToken cancellationToken);
}
