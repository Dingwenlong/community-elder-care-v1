using System.Text.Json;
using CommunityElderCare.Api.Endpoints;
using CommunityElderCare.Core.Elders;
using CommunityElderCare.Infrastructure.Elders;
using CommunityElderCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IElderProfileQuery, ElderProfileQuery>();
builder.Services.AddDbContext<CommunityCareDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("CommunityCare")
        ?? "Data Source=community-care.db"));

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<CommunityCareDbContext>();
    await dbContext.Database.MigrateAsync();
    if (!await dbContext.ElderProfiles.AnyAsync())
    {
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        var seed = DemoSeedBuilder.Build(20, 20260824, timeProvider.GetUtcNow());
        dbContext.ElderProfiles.AddRange(seed.Elders);
        await dbContext.SaveChangesAsync();
    }
}

app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));
app.MapGet("/health/ready", async (CommunityCareDbContext db, CancellationToken cancellationToken) =>
    await db.Database.CanConnectAsync(cancellationToken)
        ? Results.Ok(new { status = "ready" })
        : Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Database unavailable"));
app.MapElderEndpoints();

app.Run();

public partial class Program;
