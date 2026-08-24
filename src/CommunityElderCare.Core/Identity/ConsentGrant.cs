namespace CommunityElderCare.Core.Identity;

public sealed class ConsentGrant
{
    private readonly List<ConsentGrantField> _fields = [];

    private ConsentGrant()
    {
    }

    private ConsentGrant(
        Guid id,
        Guid elderId,
        Guid granteeUserId,
        DateTimeOffset grantedAt,
        DateTimeOffset expiresAt,
        Guid grantedByUserId)
    {
        Id = id;
        ElderId = elderId;
        GranteeUserId = granteeUserId;
        GrantedAt = grantedAt;
        ExpiresAt = expiresAt;
        GrantedByUserId = grantedByUserId;
        IsDemoData = true;
    }

    public Guid Id { get; private set; }

    public Guid ElderId { get; private set; }

    public Guid GranteeUserId { get; private set; }

    public DateTimeOffset GrantedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public Guid GrantedByUserId { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public Guid? RevokedByUserId { get; private set; }

    public bool IsDemoData { get; private set; } = true;

    public IReadOnlyCollection<ConsentGrantField> Fields => _fields;

    public static ConsentGrant Create(
        Guid id,
        Guid elderId,
        Guid granteeUserId,
        IReadOnlyCollection<ConsentField> fields,
        DateTimeOffset grantedAt,
        DateTimeOffset expiresAt,
        Guid grantedByUserId)
    {
        ArgumentNullException.ThrowIfNull(fields);
        if (fields.Count == 0)
        {
            throw new ArgumentException("At least one consent field is required.", nameof(fields));
        }
        if (expiresAt <= grantedAt)
        {
            throw new ArgumentOutOfRangeException(nameof(expiresAt));
        }

        var grant = new ConsentGrant(
            id,
            elderId,
            granteeUserId,
            grantedAt,
            expiresAt,
            grantedByUserId);
        foreach (var field in fields.Distinct())
        {
            grant._fields.Add(new ConsentGrantField(Guid.NewGuid(), id, field));
        }

        return grant;
    }

    public bool IsActiveAt(DateTimeOffset now) =>
        RevokedAt is null && GrantedAt <= now && ExpiresAt > now;

    public void Revoke(DateTimeOffset revokedAt, Guid revokedByUserId)
    {
        if (RevokedAt is not null)
        {
            return;
        }
        if (revokedAt < GrantedAt)
        {
            throw new ArgumentOutOfRangeException(nameof(revokedAt));
        }

        RevokedAt = revokedAt;
        RevokedByUserId = revokedByUserId;
    }
}

public sealed class ConsentGrantField
{
    private ConsentGrantField()
    {
    }

    internal ConsentGrantField(Guid id, Guid consentGrantId, ConsentField field)
    {
        Id = id;
        ConsentGrantId = consentGrantId;
        Field = field;
    }

    public Guid Id { get; private set; }

    public Guid ConsentGrantId { get; private set; }

    public ConsentField Field { get; private set; }
}
