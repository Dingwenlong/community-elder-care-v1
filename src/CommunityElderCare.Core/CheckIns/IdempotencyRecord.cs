namespace CommunityElderCare.Core.CheckIns;

public sealed class IdempotencyRecord
{
    private IdempotencyRecord()
    {
    }

    public IdempotencyRecord(
        Guid id,
        Guid elderId,
        Guid requestId,
        string kind,
        Guid resourceId,
        DateTimeOffset recordedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);
        Id = id;
        ElderId = elderId;
        RequestId = requestId;
        Kind = kind;
        ResourceId = resourceId;
        RecordedAt = recordedAt;
        IsDemoData = true;
    }

    public Guid Id { get; private set; }
    public Guid ElderId { get; private set; }
    public Guid RequestId { get; private set; }
    public string Kind { get; private set; } = string.Empty;
    public Guid ResourceId { get; private set; }
    public DateTimeOffset RecordedAt { get; private set; }
    public bool IsDemoData { get; private set; } = true;
}
