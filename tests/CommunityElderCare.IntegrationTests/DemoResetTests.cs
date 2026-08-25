using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CommunityElderCare.Core.CareEvents;
using CommunityElderCare.Core.Identity;
using CommunityElderCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CommunityElderCare.IntegrationTests;

public sealed class DemoResetTests
{
    [Fact]
    public async Task Reset_restores_the_same_twenty_profile_story_under_sixty_seconds()
    {
        await using var factory = new CommunityCareWebFactory();
        using var elder = factory.CreateAuthenticatedClient(DemoRole.Elder);
        using var admin = factory.CreateAuthenticatedClient(DemoRole.Administrator);
        var before = await SnapshotAsync(factory);
        var mutation = await elder.PostAsJsonAsync("/api/v1/care-events", new
        {
            clientRequestId = Guid.NewGuid(),
            elderId = factory.MainElderId,
            trigger = "LifeServiceNeed",
            summary = "重置前演示变更",
            occurredAt = DateTimeOffset.UtcNow,
        });
        mutation.EnsureSuccessStatusCode();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/demo/reset");
        request.Headers.Add("X-Confirm-Demo-Reset", "RESET-20");

        var response = await admin.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(20, result.GetProperty("elderCount").GetInt32());
        Assert.True(result.GetProperty("elapsedMilliseconds").GetInt64() < 60_000);
        var after = await SnapshotAsync(factory);
        Assert.Equal(before.ElderIds, after.ElderIds);
        Assert.Equal(before.DisplayNames, after.DisplayNames);
        Assert.Equal(20, after.ElderIds.Length);
        Assert.Equal(0, after.OpenEventCount);

        using var scope = factory.Services.CreateScope();
        var checkIns = scope.ServiceProvider.GetRequiredService<Core.CheckIns.ICheckInService>();
        var events = scope.ServiceProvider.GetRequiredService<ICareEventService>();
        var overdue = await checkIns.GetOverdueCheckInsAsync(DateTimeOffset.UtcNow, default);
        var main = Assert.Single(overdue, item => item.ElderId == factory.MainElderId);
        await events.CreateAsync(
            new CreateCareEventCommand(
                main.ElderId,
                CareEventTrigger.MissedCheckIn,
                CareEventSource.CheckIn,
                $"missed-check-in:{main.ElderId:N}:{main.DueAt.UtcTicks}",
                "老人未在计划时间内完成平安确认",
                main.DueAt,
                CareEventActorKind.Background),
            null,
            default);
        Assert.Equal(1, (await SnapshotAsync(factory)).OpenEventCount);
    }

    [Fact]
    public async Task Reset_requires_administrator_and_exact_confirmation()
    {
        await using var factory = new CommunityCareWebFactory();
        using var admin = factory.CreateAuthenticatedClient(DemoRole.Administrator);
        using var family = factory.CreateAuthenticatedClient(DemoRole.Family, familyFor: factory.MainElderId);

        var missing = await admin.PostAsync("/api/v1/demo/reset", null);
        using var familyRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/demo/reset");
        familyRequest.Headers.Add("X-Confirm-Demo-Reset", "RESET-20");
        var forbidden = await family.SendAsync(familyRequest);

        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        Assert.Equal("RESET_CONFIRMATION_REQUIRED", await ReadProblemCodeAsync(missing));
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }

    private static async Task<SeedSnapshot> SnapshotAsync(CommunityCareWebFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CommunityCareDbContext>();
        var elders = await db.ElderProfiles.AsNoTracking().OrderBy(item => item.Id).ToListAsync();
        var openCount = await db.CareEvents.CountAsync(item =>
            item.Status != CareEventStatus.Closed && item.Status != CareEventStatus.FalseAlarm);
        return new SeedSnapshot(
            elders.Select(item => item.Id).ToArray(),
            elders.Select(item => item.DemoDisplayName).ToArray(),
            openCount);
    }

    private static async Task<string?> ReadProblemCodeAsync(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        return problem.GetProperty("code").GetString();
    }

    private sealed record SeedSnapshot(Guid[] ElderIds, string[] DisplayNames, int OpenEventCount);
}
