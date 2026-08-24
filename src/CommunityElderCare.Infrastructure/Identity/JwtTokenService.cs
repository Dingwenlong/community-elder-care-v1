using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CommunityElderCare.Core.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace CommunityElderCare.Infrastructure.Identity;

public sealed class JwtTokenService(IConfiguration configuration, TimeProvider timeProvider)
{
    public JwtTokenResult Create(UserAccount account)
    {
        var key = configuration["COMMUNITYCARE_JWT_SIGNING_KEY"];
        if (string.IsNullOrWhiteSpace(key) || Encoding.UTF8.GetByteCount(key) < 32)
        {
            throw new InvalidOperationException("COMMUNITYCARE_JWT_SIGNING_KEY must contain at least 32 bytes.");
        }

        var now = timeProvider.GetUtcNow();
        var expiresAt = now.AddHours(8);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, account.Id.ToString()),
            new("role", account.Role.ToString()),
            new("demo_mode", "true"),
        };
        AddOptionalClaim(claims, "elder_id", account.ElderId);
        AddOptionalClaim(claims, "area_code", account.AreaCode);
        AddOptionalClaim(claims, "assigned_task_id", account.AssignedTaskId);

        var token = new JwtSecurityToken(
            issuer: "community-elder-care-demo",
            audience: "community-elder-care-clients",
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256));

        return new JwtTokenResult(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt);
    }

    private static void AddOptionalClaim(ICollection<Claim> claims, string name, object? value)
    {
        if (value is not null)
        {
            claims.Add(new Claim(name, value.ToString()!));
        }
    }
}

public sealed record JwtTokenResult(string AccessToken, DateTimeOffset ExpiresAt);
