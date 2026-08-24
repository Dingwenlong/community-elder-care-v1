namespace CommunityElderCare.Core.Identity;

public sealed class AccessAuditRecord
{
    private AccessAuditRecord()
    {
    }

    public AccessAuditRecord(
        Guid id,
        string action,
        Guid actorUserId,
        Guid elderId,
        string reason,
        DateTimeOffset occurredAt,
        string fieldList)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        Id = id;
        Action = action;
        ActorUserId = actorUserId;
        ElderId = elderId;
        Reason = reason.Trim();
        OccurredAt = occurredAt;
        FieldList = fieldList;
        IsDemoData = true;
    }

    public Guid Id { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public Guid ActorUserId { get; private set; }
    public Guid ElderId { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; private set; }
    public string FieldList { get; private set; } = string.Empty;
    public bool IsDemoData { get; private set; } = true;
}
