using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;
using CommunityElderCare.Api.Endpoints;
using CommunityElderCare.Core.Elders;
using CommunityElderCare.Infrastructure.Elders;
using CommunityElderCare.Infrastructure.Persistence;
using CommunityElderCare.Infrastructure.Identity;
using CommunityElderCare.Core.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IPasswordHasher<UserAccount>, PasswordHasher<UserAccount>>();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<IAccessPolicy, AccessPolicy>();
builder.Services.AddScoped<IElderProfileQuery, ElderProfileQuery>();
builder.Services.AddDbContext<CommunityCareDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("CommunityCare")
        ?? "Data Source=community-care.db"));
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var configuredSigningKey = builder.Configuration["COMMUNITYCARE_JWT_SIGNING_KEY"];
        if (string.IsNullOrWhiteSpace(configuredSigningKey) ||
            Encoding.UTF8.GetByteCount(configuredSigningKey) < 32)
        {
            throw new InvalidOperationException("COMMUNITYCARE_JWT_SIGNING_KEY must contain at least 32 bytes.");
        }

        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "community-elder-care-demo",
            ValidateAudience = true,
            ValidAudience = "community-elder-care-clients",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuredSigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            RoleClaimType = "role",
            NameClaimType = "sub",
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<CommunityCareDbContext>();
    var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
    await dbContext.Database.MigrateAsync();
    if (!await dbContext.ElderProfiles.AnyAsync())
    {
        var seed = DemoSeedBuilder.Build(20, 20260824, timeProvider.GetUtcNow());
        dbContext.ElderProfiles.AddRange(seed.Elders);
        await dbContext.SaveChangesAsync();
    }

    if (!await dbContext.UserAccounts.AnyAsync())
    {
        var demoPassword = builder.Configuration["COMMUNITYCARE_DEMO_PASSWORD"];
        if (string.IsNullOrEmpty(demoPassword))
        {
            throw new InvalidOperationException("COMMUNITYCARE_DEMO_PASSWORD is required for demo account seeding.");
        }

        var now = timeProvider.GetUtcNow();
        var mainElderId = DemoSeedBuilder
            .Build(20, 20260824, now)
            .MainElderId;
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<UserAccount>>();
        var accounts = DemoIdentitySeed.BuildAccounts(mainElderId);
        foreach (var account in accounts)
        {
            account.SetPasswordHash(passwordHasher.HashPassword(account, demoPassword));
        }

        dbContext.UserAccounts.AddRange(accounts);
        dbContext.ConsentGrants.Add(ConsentGrant.Create(
            Guid.Parse("33333333-3333-3333-3333-333333333301"),
            mainElderId,
            DemoIdentitySeed.FamilyUserId,
            [
                ConsentField.RecentStatus,
                ConsentField.CareEventSummary,
                ConsentField.VisitSummary,
                ConsentField.ReminderCompletion,
            ],
            now,
            now.AddYears(1),
            DemoIdentitySeed.ElderUserId));
        await dbContext.SaveChangesAsync();
    }
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));
app.MapGet("/health/ready", async (CommunityCareDbContext db, CancellationToken cancellationToken) =>
    await db.Database.CanConnectAsync(cancellationToken)
        ? Results.Ok(new { status = "ready" })
        : Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Database unavailable"));
app.MapElderEndpoints();
app.MapAuthEndpoints();
app.MapConsentEndpoints();
app.MapBreakGlassEndpoints();

app.Run();

public partial class Program;
