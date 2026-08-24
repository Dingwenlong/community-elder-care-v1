using CommunityElderCare.Core.Common;
using CommunityElderCare.Core.Identity;

namespace CommunityElderCare.Core.Ai;

public enum AiDraftKind
{
    ServiceRequest,
    VisitSummary,
}

public enum AiDraftStatus
{
    Pending,
    Confirmed,
}

public sealed class AiDraft
{
    private AiDraft()
    {
    }

    private AiDraft(
        Guid id,
        Guid elderId,
        AiDraftKind kind,
        string sessionIdHash,
        string generatedText,
        Guid? visitId,
        DateTimeOffset createdAt)
    {
        Id = id;
        ElderId = elderId;
        Kind = kind;
        SessionIdHash = sessionIdHash;
        GeneratedText = generatedText;
        VisitId = visitId;
        CreatedAt = createdAt;
        Status = AiDraftStatus.Pending;
        IsDemoData = true;
    }

    public Guid Id { get; private set; }
    public Guid ElderId { get; private set; }
    public AiDraftKind Kind { get; private set; }
    public string SessionIdHash { get; private set; } = string.Empty;
    public string GeneratedText { get; private set; } = string.Empty;
    public Guid? VisitId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public AiDraftStatus Status { get; private set; }
    public Guid? ConfirmedByUserId { get; private set; }
    public DateTimeOffset? ConfirmedAt { get; private set; }
    public bool IsDemoData { get; private set; } = true;

    public static AiDraft Create(
        Guid id,
        Guid elderId,
        AiDraftKind kind,
        string sessionIdHash,
        string generatedText,
        Guid? visitId,
        DateTimeOffset createdAt) => new(
        id,
        elderId,
        kind,
        sessionIdHash,
        generatedText.Trim(),
        visitId,
        createdAt);

    public OperationResult<AiDraft> Confirm(ActorContext actor, DateTimeOffset confirmedAt)
    {
        var allowed = Kind switch
        {
            AiDraftKind.ServiceRequest =>
                actor.Role == DemoRole.Elder && actor.ElderId == ElderId,
            AiDraftKind.VisitSummary => actor.Role == DemoRole.CommunityStaff,
            _ => false,
        };
        if (!allowed)
        {
            return new(false, null, "FORBIDDEN_SCOPE", "Actor cannot confirm this draft.");
        }
        if (Status == AiDraftStatus.Confirmed)
        {
            return new(true, this, null, null);
        }

        Status = AiDraftStatus.Confirmed;
        ConfirmedByUserId = actor.UserId;
        ConfirmedAt = confirmedAt;
        return new(true, this, null, null);
    }
}
