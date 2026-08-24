namespace CommunityElderCare.Core.Identity;

public sealed class BreakGlassGrant
{
    private BreakGlassGrant()
    {
    }

    private BreakGlassGrant(
        Guid id,
        Guid elderId,
        Guid communityStaffUserId,
        Guid careEventId,
        string reason,
        DateTimeOffset grantedAt,
        DateTimeOffset expiresAt)
    {
        Id = id;
        ElderId = elderId;
        CommunityStaffUserId = communityStaffUserId;
        CareEventId = careEventId;
        Reason = reason;
        GrantedAt = grantedAt;
        ExpiresAt = expiresAt;
        IsDemoData = true;
    }

    public Guid Id { get; private set; }

    public Guid ElderId { get; private set; }

    public Guid CommunityStaffUserId { get; private set; }

    public Guid CareEventId { get; private set; }

    public string Reason { get; private set; } = string.Empty;

    public DateTimeOffset GrantedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public bool IsDemoData { get; private set; } = true;

    public static BreakGlassGrant Create(
        Guid id,
        Guid elderId,
        Guid communityStaffUserId,
        Guid careEventId,
        string reason,
        DateTimeOffset grantedAt,
        DateTimeOffset expiresAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (expiresAt <= grantedAt || expiresAt > grantedAt.AddMinutes(15))
        {
            throw new ArgumentOutOfRangeException(nameof(expiresAt));
        }

        return new BreakGlassGrant(
            id,
            elderId,
            communityStaffUserId,
            careEventId,
            reason.Trim(),
            grantedAt,
            expiresAt);
    }

    public bool IsActiveAt(DateTimeOffset now) => GrantedAt <= now && ExpiresAt > now;
}
