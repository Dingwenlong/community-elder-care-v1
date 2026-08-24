using CommunityElderCare.Core.Identity;

namespace CommunityElderCare.Api.Contracts.Auth;

public sealed record LoginResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    DemoRole Role,
    string Shell,
    bool IsDemoMode);
