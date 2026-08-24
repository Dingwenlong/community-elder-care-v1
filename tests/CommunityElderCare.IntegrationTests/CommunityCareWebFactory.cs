using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using CommunityElderCare.Core.Identity;
using CommunityElderCare.Infrastructure.Persistence;
using CommunityElderCare.Infrastructure.Identity;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace CommunityElderCare.IntegrationTests;

public class CommunityCareWebFactory : WebApplicationFactory<Program>
{
    private const string TestPassword = "DemoPassword!2026";
    private const string TestSigningKey = "test-only-signing-key-2026-08-24-community-care";
    private readonly string _databasePath = Path.Combine(
        Path.GetTempPath(),
        $"community-elder-care-{Guid.NewGuid():N}.db");

    public Guid MainElderId => DemoSeedBuilder
        .Build(20, 20260824, DateTimeOffset.UnixEpoch)
        .MainElderId;

    public CommunityCareWebFactory()
    {
        using var databaseFile = File.Create(_databasePath);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:CommunityCare"] = $"Data Source={_databasePath};Pooling=False",
                ["COMMUNITYCARE_DEMO_PASSWORD"] = TestPassword,
                ["COMMUNITYCARE_JWT_SIGNING_KEY"] = TestSigningKey,
            }));
    }

    public HttpClient CreateAuthenticatedClient(
        DemoRole role,
        string? areaCode = null,
        Guid? familyFor = null,
        Guid? elderId = null,
        Guid? assignedTaskId = null)
    {
        var resolvedElderId = elderId ?? familyFor ??
            (role is DemoRole.Elder or DemoRole.ServiceWorker ? MainElderId : null);
        var resolvedAreaCode = role == DemoRole.CommunityStaff ? areaCode ?? "A01" : areaCode;
        var resolvedTaskId = assignedTaskId ??
            (role is DemoRole.CommunityStaff or DemoRole.ServiceWorker
                ? DemoIdentitySeed.MainCareTaskId
                : null);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, DemoIdentitySeed.GetUserId(role).ToString()),
            new("role", role.ToString()),
            new("demo_mode", "true"),
        };
        AddOptionalClaim(claims, "elder_id", resolvedElderId);
        AddOptionalClaim(claims, "area_code", resolvedAreaCode);
        AddOptionalClaim(claims, "assigned_task_id", resolvedTaskId);

        var token = new JwtSecurityToken(
            issuer: "community-elder-care-demo",
            audience: "community-elder-care-clients",
            claims: claims,
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSigningKey)),
                SecurityAlgorithms.HmacSha256));

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new(
            "Bearer",
            new JwtSecurityTokenHandler().WriteToken(token));

        return client;
    }

    private static void AddOptionalClaim(ICollection<Claim> claims, string name, object? value)
    {
        if (value is not null)
        {
            claims.Add(new Claim(name, value.ToString()!));
        }
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        DeleteDatabaseFiles();
        GC.SuppressFinalize(this);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            DeleteDatabaseFiles();
        }
    }

    private void DeleteDatabaseFiles()
    {
        foreach (var path in new[] { _databasePath, $"{_databasePath}-shm", $"{_databasePath}-wal" })
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
