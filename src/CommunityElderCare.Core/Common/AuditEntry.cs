namespace CommunityElderCare.Core.Common;

public sealed class AuditEntry
{
    private AuditEntry()
    {
    }

    public AuditEntry(
        Guid id,
        Guid? actorUserId,
        string actorKind,
        string action,
        string entityType,
        Guid entityId,
        DateTimeOffset occurredAt,
        string reason,
        string? beforeStatus,
        string? afterStatus)
    {
        Id = id;
        ActorUserId = actorUserId;
        ActorKind = Required(actorKind, nameof(actorKind));
        Action = Required(action, nameof(action));
        EntityType = Required(entityType, nameof(entityType));
        EntityId = entityId;
        OccurredAt = occurredAt;
        Reason = Required(reason, nameof(reason));
        BeforeStatus = beforeStatus;
        AfterStatus = afterStatus;
        IsDemoData = true;
    }

    public Guid Id { get; private set; }
    public Guid? ActorUserId { get; private set; }
    public string ActorKind { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public string? BeforeStatus { get; private set; }
    public string? AfterStatus { get; private set; }
    public bool IsDemoData { get; private set; } = true;

    private static string Required(string value, string parameter)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameter);
        return value.Trim();
    }
}
