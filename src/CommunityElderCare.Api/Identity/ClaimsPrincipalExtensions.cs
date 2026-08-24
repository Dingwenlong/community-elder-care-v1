using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CommunityElderCare.Core.Identity;

namespace CommunityElderCare.Api.Identity;

public static class ClaimsPrincipalExtensions
{
    public static ActorContext GetActorContext(this ClaimsPrincipal principal)
    {
        var userId = ParseRequiredGuid(principal, JwtRegisteredClaimNames.Sub);
        var roleValue = principal.FindFirst("role")?.Value;
        if (!Enum.TryParse<DemoRole>(roleValue, ignoreCase: false, out var role))
        {
            throw new UnauthorizedAccessException("JWT role claim is missing or invalid.");
        }

        return new ActorContext(
            userId,
            role,
            ParseOptionalGuid(principal, "elder_id"),
            principal.FindFirst("area_code")?.Value,
            ParseOptionalGuid(principal, "assigned_task_id"));
    }

    private static Guid ParseRequiredGuid(ClaimsPrincipal principal, string claimType)
    {
        return Guid.TryParse(principal.FindFirst(claimType)?.Value, out var value)
            ? value
            : throw new UnauthorizedAccessException($"JWT {claimType} claim is missing or invalid.");
    }

    private static Guid? ParseOptionalGuid(ClaimsPrincipal principal, string claimType)
    {
        return Guid.TryParse(principal.FindFirst(claimType)?.Value, out var value) ? value : null;
    }
}
