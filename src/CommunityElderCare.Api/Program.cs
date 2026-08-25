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
using CommunityElderCare.Core.Devices;
using CommunityElderCare.Infrastructure.Devices;
using CommunityElderCare.Infrastructure.Notifications;
using CommunityElderCare.Infrastructure.Demo;
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
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuditSaveChangesInterceptor>();
builder.Services.AddSingleton<DemoMutationGate>();
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
builder.Services.AddScoped<DeviceTokenValidator>();
builder.Services.AddScoped<IDeviceSignalService, DeviceSignalService>();
builder.Services.AddScoped<SimulationNotificationService>();
builder.Services.AddScoped<BackgroundJobRecorder>();
builder.Services.AddScoped<DemoResetService>();
builder.Services.AddScoped<IElderProfileQuery, ElderProfileQuery>();
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddHostedService<MissedCheckInWorker>();
    builder.Services.AddHostedService<ContactEscalationWorker>();
}
builder.Services.AddDbContext<CommunityCareDbContext>((services, options) =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("CommunityCare")
        ?? "Data Source=community-care.db")
    .AddInterceptors(services.GetRequiredService<AuditSaveChangesInterceptor>()));
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
    await RuntimeCopyUpgrade.ApplyAsync(dbContext);
    if (!await dbContext.ElderProfiles.AnyAsync())
    {
        var seed = DemoSeedBuilder.Build(20, 20260824, timeProvider.GetUtcNow());
        dbContext.ElderProfiles.AddRange(seed.Elders);
        await dbContext.SaveChangesAsync();
    }

    var demoPassword = builder.Configuration["COMMUNITYCARE_DEMO_PASSWORD"];
    if (string.IsNullOrEmpty(demoPassword))
    {
        throw new InvalidOperationException("COMMUNITYCARE_DEMO_PASSWORD is required for demo account seeding.");
    }

    var accountSeedTime = timeProvider.GetUtcNow();
    var accountMainElderId = DemoSeedBuilder
        .Build(20, 20260824, accountSeedTime)
        .MainElderId;
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<UserAccount>>();
    var accountTemplates = DemoIdentitySeed.BuildAccounts(accountMainElderId);
    var accountIds = accountTemplates.Select(account => account.Id).ToList();
    var existingAccounts = await dbContext.UserAccounts
        .Where(account => accountIds.Contains(account.Id))
        .ToDictionaryAsync(account => account.Id);
    foreach (var template in accountTemplates)
    {
        if (existingAccounts.TryGetValue(template.Id, out var existing))
        {
            existing.SetPasswordHash(passwordHasher.HashPassword(existing, demoPassword));
        }
        else
        {
            template.SetPasswordHash(passwordHasher.HashPassword(template, demoPassword));
            dbContext.UserAccounts.Add(template);
        }
    }

    var demoConsentId = Guid.Parse("33333333-3333-3333-3333-333333333301");
    if (!await dbContext.ConsentGrants.AnyAsync(grant => grant.Id == demoConsentId))
    {
        dbContext.ConsentGrants.Add(ConsentGrant.Create(
            demoConsentId,
            accountMainElderId,
            DemoIdentitySeed.FamilyUserId,
            [
                ConsentField.RecentStatus,
                ConsentField.CareEventSummary,
                ConsentField.VisitSummary,
                ConsentField.ReminderCompletion,
            ],
            accountSeedTime,
            accountSeedTime.AddYears(1),
            DemoIdentitySeed.ElderUserId));
    }
    await dbContext.SaveChangesAsync();

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
                "复诊预约提醒",
                dayStart.AddHours(10)),
            Reminder.Create(
                Guid.Parse("44444444-4444-4444-4444-444444444403"),
                mainElderId,
                ReminderType.CommunityActivity,
                "社区活动提醒",
                dayStart.AddHours(14)),
            Reminder.Create(
                Guid.Parse("44444444-4444-4444-4444-444444444404"),
                mainElderId,
                ReminderType.VisitSchedule,
                "上门探访提醒",
                dayStart.AddHours(16)));
        await dbContext.SaveChangesAsync();
    }

    var deviceSeedTime = timeProvider.GetUtcNow();
    var deviceToken = builder.Configuration["COMMUNITYCARE_DEVICE_TOKEN"];
    var deviceTokenHash = string.IsNullOrWhiteSpace(deviceToken)
        ? null
        : DeviceTokenValidator.HashToken(deviceToken);
    var demoDevice = await dbContext.Devices.SingleOrDefaultAsync(
        device => device.Id == DemoDeviceIds.MainSosDevice);
    if (demoDevice is null)
    {
        var mainElderId = DemoSeedBuilder.Build(20, 20260824, deviceSeedTime).MainElderId;
        dbContext.Devices.Add(Device.Register(
            DemoDeviceIds.MainSosDevice,
            mainElderId,
            "客厅 SOS 设备",
            deviceTokenHash,
            deviceSeedTime));
    }
    else
    {
        demoDevice.BindProcessTokenHash(deviceTokenHash);
    }
    await dbContext.SaveChangesAsync();
}

app.UseAuthentication();
app.UseAuthorization();
app.Use(async (context, next) =>
{
    var isMutation = !HttpMethods.IsGet(context.Request.Method) &&
        !HttpMethods.IsHead(context.Request.Method) &&
        !HttpMethods.IsOptions(context.Request.Method);
    if (!isMutation || context.Request.Path == "/api/v1/demo/reset")
    {
        await next(context);
        return;
    }

    var gate = context.RequestServices.GetRequiredService<DemoMutationGate>();
    using var lease = await gate.EnterAsync(context.RequestAborted);
    await next(context);
});

app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));
app.MapGet("/health/ready", async (
    CommunityCareDbContext db,
    CloudLlmOptions aiOptions,
    IWebHostEnvironment environment,
    IConfiguration configuration,
    CancellationToken cancellationToken) =>
{
    if (!await db.Database.CanConnectAsync(cancellationToken))
    {
        return Results.Problem(
            statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "Database unavailable");
    }
    var device = await db.Devices.AsNoTracking().SingleOrDefaultAsync(
        candidate => candidate.Id == DemoDeviceIds.MainSosDevice && candidate.IsEnabled,
        cancellationToken);
    var urls = configuration["ASPNETCORE_URLS"] ?? string.Empty;
    return Results.Ok(new
    {
        status = "ready",
        components = new object[]
        {
            new { name = "database", status = "ready", detail = "SQLite connected" },
            new
            {
                name = "backgroundJobs",
                status = environment.IsEnvironment("Testing") ? "degraded" : "ready",
                detail = environment.IsEnvironment("Testing") ? "disabled in tests" : "workers registered",
            },
            new
            {
                name = "ai",
                status = aiOptions.IsConfigured ? "ready" : "degraded",
                detail = aiOptions.IsConfigured ? "cloud adapter configured" : "fixed fallback active",
            },
            new
            {
                name = "deviceGateway",
                status = device is null ? "unavailable" : "ready",
                detail = device?.TokenHash is null ? "simulator only" : "hardware token bound",
            },
            new
            {
                name = "localNetwork",
                status = urls.Contains("0.0.0.0", StringComparison.Ordinal) ? "ready" : "degraded",
                detail = urls.Contains("0.0.0.0", StringComparison.Ordinal)
                    ? "LAN binding enabled"
                    : "loopback or test binding",
            },
        },
    });
});
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
app.MapDeviceEndpoints();
app.MapNotificationSimulationEndpoints();
app.MapAuditEndpoints();
app.MapReportEndpoints();
app.MapDemoEndpoints();

app.Run();

public partial class Program;
