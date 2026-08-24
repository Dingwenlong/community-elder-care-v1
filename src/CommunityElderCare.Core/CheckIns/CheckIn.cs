namespace CommunityElderCare.Core.CheckIns;

public enum CheckInKind
{
    ElderSelf,
    CommunityManual,
}

public sealed class CheckIn
{
    private CheckIn()
    {
    }

    private CheckIn(
        Guid id,
        Guid elderId,
        Guid requestId,
        DateTimeOffset clientTime,
        DateTimeOffset receivedAt,
        CheckInKind kind,
        Guid actorUserId,
        string? manualReason)
    {
        Id = id;
        ElderId = elderId;
        RequestId = requestId;
        ClientTime = clientTime;
        ReceivedAt = receivedAt;
        Kind = kind;
        ActorUserId = actorUserId;
        ManualReason = manualReason;
        IsDemoData = true;
    }

    public Guid Id { get; private set; }
    public Guid ElderId { get; private set; }
    public Guid RequestId { get; private set; }
    public DateTimeOffset ClientTime { get; private set; }
    public DateTimeOffset ReceivedAt { get; private set; }
    public CheckInKind Kind { get; private set; }
    public Guid ActorUserId { get; private set; }
    public string? ManualReason { get; private set; }
    public bool IsDemoData { get; private set; } = true;

    public static CheckIn Create(
        Guid id,
        Guid elderId,
        Guid requestId,
        DateTimeOffset clientTime,
        DateTimeOffset receivedAt,
        CheckInKind kind,
        Guid actorUserId,
        string? manualReason = null) =>
        new(id, elderId, requestId, clientTime, receivedAt, kind, actorUserId, manualReason);
}
