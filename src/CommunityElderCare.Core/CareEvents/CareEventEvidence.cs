namespace CommunityElderCare.Core.CareEvents;

public sealed class CareEventEvidence
{
    private CareEventEvidence()
    {
    }

    internal CareEventEvidence(
        Guid id,
        Guid careEventId,
        string kind,
        string summary,
        DateTimeOffset occurredAt,
        DateTimeOffset recordedAt,
        string? sourceEventId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);
        ArgumentException.ThrowIfNullOrWhiteSpace(summary);
        Id = id;
        CareEventId = careEventId;
        Kind = kind.Trim();
        Summary = summary.Trim();
        OccurredAt = occurredAt;
        RecordedAt = recordedAt;
        SourceEventId = string.IsNullOrWhiteSpace(sourceEventId) ? null : sourceEventId.Trim();
        IsSimulation = true;
    }

    public Guid Id { get; private set; }
    public Guid CareEventId { get; private set; }
    public string Kind { get; private set; } = string.Empty;
    public string Summary { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; private set; }
    public DateTimeOffset RecordedAt { get; private set; }
    public string? SourceEventId { get; private set; }
    public bool IsSimulation { get; private set; } = true;
}
