using CommunityElderCare.Core.Common;
using CommunityElderCare.Core.Identity;

namespace CommunityElderCare.Core.Ai;

public sealed class MemoryCandidate
{
    private MemoryCandidate()
    {
    }

    private MemoryCandidate(
        Guid id,
        Guid elderId,
        string sessionIdHash,
        string generatedText,
        DateTimeOffset createdAt)
    {
        Id = id;
        ElderId = elderId;
        SessionIdHash = sessionIdHash;
        GeneratedText = generatedText;
        CreatedAt = createdAt;
        IsDemoData = true;
    }

    public Guid Id { get; private set; }
    public Guid ElderId { get; private set; }
    public string SessionIdHash { get; private set; } = string.Empty;
    public string GeneratedText { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? ConfirmedByUserId { get; private set; }
    public DateTimeOffset? ConfirmedAt { get; private set; }
    public bool IsDemoData { get; private set; } = true;

    public bool IsConfirmed => ConfirmedAt is not null;

    public static MemoryCandidate Create(
        Guid id,
        Guid elderId,
        string sessionIdHash,
        string generatedText,
        DateTimeOffset createdAt) => new(
        id,
        elderId,
        sessionIdHash,
        generatedText.Trim(),
        createdAt);

    public OperationResult<MemoryCandidate> Confirm(
        ActorContext actor,
        DateTimeOffset confirmedAt)
    {
        if (actor.Role != DemoRole.Elder || actor.ElderId != ElderId)
        {
            return new(false, null, "FORBIDDEN_SCOPE", "Only the elder can confirm memory.");
        }
        if (ConfirmedAt is null)
        {
            ConfirmedByUserId = actor.UserId;
            ConfirmedAt = confirmedAt;
        }
        return new(true, this, null, null);
    }
}
