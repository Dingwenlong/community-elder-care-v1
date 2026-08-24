using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;
using CommunityElderCare.Api.Endpoints;
using CommunityElderCare.Core.Elders;
using CommunityElderCare.Infrastructure.Elders;
using CommunityElderCare.Infrastructure.Persistence;
using CommunityElderCare.Infrastructure.Identity;
using CommunityElderCare.Core.Identity;
using CommunityElderCare.Core.CheckIns;
using CommunityElderCare.Infrastructure.CheckIns;
using CommunityElderCare.Core.CareEvents;
using CommunityElderCare.Infrastructure.CareEvents;
using CommunityElderCare.Infrastructure.Background;
using CommunityElderCare.Core.CareWork;
using CommunityElderCare.Infrastructure.CareWork;
using CommunityElderCare.Core.Ai;
using CommunityElderCare.Infrastructure.Ai;
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
builder.Services.AddSingleton(_ => new EscalationPolicy(
    TimeSpan.FromMinutes(builder.Configuration.GetValue("CareEvents:DemoEscalation:PhoneAttemptMinutes", 2)),
    TimeSpan.FromMinutes(builder.Configuration.GetValue("CareEvents:DemoEscalation:EmergencyContactMinutes", 5)),
    TimeSpan.FromMinutes(builder.Configuration.GetValue("CareEvents:DemoEscalation:UnableToConfirmMinutes", 10))));
builder.Services.AddSingleton<IPasswordHasher<UserAccount>, PasswordHasher<UserAccount>>();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<IAccessPolicy, AccessPolicy>();
builder.Services.AddScoped<ICheckInService, CheckInService>();
builder.Services.AddScoped<ICareEventService, CareEventService>();
builder.Services.AddScoped<IVisitService, VisitService>();
builder.Services.AddScoped<IServiceOrderService, ServiceOrderService>();
var cloudLlmOptions = new CloudLlmOptions
{
    BaseUrl = builder.Configuration[$"{CloudLlmOptions.SectionName}:BaseUrl"],
    Model = builder.Configuration[$"{CloudLlmOptions.SectionName}:Model"],
    ApiKey = builder.Configuration["COMMUNITYCARE_LLM_API_KEY"],
};
builder.Services.AddSingleton(cloudLlmOptions);
builder.Services.AddSingleton<FixedContentFallback>();
builder.Services.AddHttpClient<OpenAiCompatibleLlmClient>(client =>
    client.Timeout = Timeout.InfiniteTimeSpan);
builder.Services.AddScoped<ICloudLlmClient>(services =>
    services.GetRequiredService<OpenAiCompatibleLlmClient>());
builder.Services.AddScoped<IAiCareService, AiCareService>();
builder.Services.AddScoped<IElderProfileQuery, ElderProfileQuery>();
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddHostedService<MissedCheckInWorker>();
    builder.Services.AddHostedService<ContactEscalationWorker>();
}
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

    if (!await dbContext.Reminders.AnyAsync())
    {
        var now = timeProvider.GetUtcNow();
        var dayStart = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, now.Offset);
        var mainElderId = DemoSeedBuilder.Build(20, 20260824, now).MainElderId;
        dbContext.Reminders.AddRange(
            Reminder.Create(
                Guid.Parse("44444444-4444-4444-4444-444444444401"),
                mainElderId,
                ReminderType.Medication,
                "按既有医嘱查看今日服药提醒",
                dayStart.AddHours(8)),
            Reminder.Create(
                Guid.Parse("44444444-4444-4444-4444-444444444402"),
                mainElderId,
                ReminderType.FollowUpAppointment,
                "演示复诊预约提醒",
                dayStart.AddHours(10)),
            Reminder.Create(
                Guid.Parse("44444444-4444-4444-4444-444444444403"),
                mainElderId,
                ReminderType.CommunityActivity,
                "社区活动演示提醒",
                dayStart.AddHours(14)),
            Reminder.Create(
                Guid.Parse("44444444-4444-4444-4444-444444444404"),
                mainElderId,
                ReminderType.VisitSchedule,
                "上门探访演示提醒",
                dayStart.AddHours(16)));
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
app.MapCheckInEndpoints();
app.MapCareEventEndpoints();
app.MapVisitEndpoints();
app.MapServiceOrderEndpoints();
app.MapFamilyEndpoints();
app.MapAiEndpoints();

app.Run();

public partial class Program;
