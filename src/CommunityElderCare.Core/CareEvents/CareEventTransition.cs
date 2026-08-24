namespace CommunityElderCare.Core.CareEvents;

public sealed class CareEventTransition
{
    private CareEventTransition()
    {
    }

    internal CareEventTransition(
        Guid id,
        Guid careEventId,
        CareEventStatus fromStatus,
        CareEventStatus toStatus,
        CareEventActorKind actorKind,
        Guid? actorUserId,
        string? reason,
        DateTimeOffset occurredAt)
    {
        Id = id;
        CareEventId = careEventId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        ActorKind = actorKind;
        ActorUserId = actorUserId;
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        OccurredAt = occurredAt;
        IsSimulation = true;
    }

    public Guid Id { get; private set; }
    public Guid CareEventId { get; private set; }
    public CareEventStatus FromStatus { get; private set; }
    public CareEventStatus ToStatus { get; private set; }
    public CareEventActorKind ActorKind { get; private set; }
    public Guid? ActorUserId { get; private set; }
    public string? Reason { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public bool IsSimulation { get; private set; } = true;
}
