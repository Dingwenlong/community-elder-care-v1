using CommunityElderCare.Core.Identity;

namespace CommunityElderCare.Api.Contracts.Consents;

public sealed record UpdateConsentRequest(
    IReadOnlyList<ConsentField> Fields,
    DateTimeOffset ExpiresAt);

public sealed record ConsentResponse(
    Guid Id,
    Guid ElderId,
    Guid GranteeUserId,
    IReadOnlyList<ConsentField> Fields,
    DateTimeOffset GrantedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? RevokedAt,
    bool IsDemoData);
