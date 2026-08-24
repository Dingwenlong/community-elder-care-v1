using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CommunityElderCare.Core.CareEvents;
using CommunityElderCare.Core.Identity;
using CommunityElderCare.Infrastructure.Persistence;
using CommunityElderCare.Infrastructure.Background;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace CommunityElderCare.IntegrationTests;

public sealed class CareEventEndpointTests
{
    [Fact]
    public async Task Staff_acceptance_assigns_exactly_one_current_owner()
    {
        await using var factory = new CommunityCareWebFactory();
        using var client = factory.CreateAuthenticatedClient(
            DemoRole.CommunityStaff,
            areaCode: "A01");
        var created = await CreateEventAsync(client, factory.MainElderId, "DeviceAnomaly");

        var response = await client.PostAsync(
            $"/api/v1/care-events/{created.GetProperty("id").GetGuid()}/accept",
            content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var accepted = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Accepted", accepted.GetProperty("status").GetString());
        Assert.NotEqual(Guid.Empty, accepted.GetProperty("currentOwnerUserId").GetGuid());
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CommunityCareDbContext>();
        var stored = await dbContext.CareEvents.SingleAsync(careEvent =>
            careEvent.Id == created.GetProperty("id").GetGuid());
        Assert.NotNull(stored.CurrentOwnerUserId);
    }

    [Fact]
    public async Task Illegal_transition_returns_conflict_with_stable_code()
    {
        await using var factory = new CommunityCareWebFactory();
        using var client = factory.CreateAuthenticatedClient(
            DemoRole.CommunityStaff,
            areaCode: "A01");
        var created = await CreateEventAsync(client, factory.MainElderId, "DeviceAnomaly");

        var response = await client.PostAsJsonAsync(
            $"/api/v1/care-events/{created.GetProperty("id").GetGuid()}/transitions",
            new { toStatus = "Closed", reason = "不允许直接结案", resolution = "无" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("INVALID_TRANSITION", await ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Duplicate_create_returns_the_original_event_without_duplicate_rows()
    {
        await using var factory = new CommunityCareWebFactory();
        using var client = factory.CreateAuthenticatedClient(DemoRole.Elder);
        var clientRequestId = Guid.NewGuid();
        var payload = new
        {
            clientRequestId,
            elderId = factory.MainElderId,
            trigger = "LifeServiceNeed",
            summary = "需要演示生活协助",
            occurredAt = DateTimeOffset.UtcNow,
        };

        var firstResponse = await client.PostAsJsonAsync("/api/v1/care-events", payload);
        var secondResponse = await client.PostAsJsonAsync("/api/v1/care-events", payload);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        var first = await firstResponse.Content.ReadFromJsonAsync<JsonElement>();
        var second = await secondResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(first.GetProperty("id").GetGuid(), second.GetProperty("id").GetGuid());
        Assert.False(first.GetProperty("isDuplicate").GetBoolean());
        Assert.True(second.GetProperty("isDuplicate").GetBoolean());
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CommunityCareDbContext>();
        Assert.Equal(1, await dbContext.CareEvents.CountAsync(careEvent =>
            careEvent.SourceEventId == $"ElderHelp:{clientRequestId:N}"));
    }

    [Fact]
    public async Task Emergency_level_is_independent_from_process_status_and_actions_are_simulated()
    {
        await using var factory = new CommunityCareWebFactory();
        using var elderClient = factory.CreateAuthenticatedClient(DemoRole.Elder);
        var created = await CreateEventAsync(
            elderClient,
            factory.MainElderId,
            "ExplicitSos");

        Assert.Equal("Emergency", created.GetProperty("level").GetString());
        Assert.Equal("PendingConfirmation", created.GetProperty("status").GetString());
        var attempts = created.GetProperty("contactAttempts").EnumerateArray().ToList();
        Assert.Contains(attempts, item =>
            item.GetProperty("kind").GetString() == "CommunityNotification" &&
            item.GetProperty("isSimulation").GetBoolean());
        Assert.Contains(attempts, item =>
            item.GetProperty("kind").GetString() == "EmergencyContact" &&
            item.GetProperty("isSimulation").GetBoolean());

        using var staffClient = factory.CreateAuthenticatedClient(
            DemoRole.CommunityStaff,
            areaCode: "A01");
        var acceptedResponse = await staffClient.PostAsync(
            $"/api/v1/care-events/{created.GetProperty("id").GetGuid()}/accept",
            content: null);
        acceptedResponse.EnsureSuccessStatusCode();
        var accepted = await acceptedResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Emergency", accepted.GetProperty("level").GetString());
        Assert.Equal("Accepted", accepted.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Family_report_contract_cannot_choose_level_status_or_owner()
    {
        await using var factory = new CommunityCareWebFactory();
        using var client = factory.CreateAuthenticatedClient(
            DemoRole.Family,
            familyFor: factory.MainElderId);
        var clientRequestId = Guid.NewGuid();
        var fakeOwner = Guid.NewGuid();

        var response = await client.PostAsJsonAsync(
            "/api/v1/care-events",
            new
            {
                clientRequestId,
                elderId = factory.MainElderId,
                trigger = "ExplicitSos",
                summary = "家属报告暂时联系不上老人",
                occurredAt = DateTimeOffset.UtcNow,
                level = "Emergency",
                status = "Closed",
                ownerUserId = fakeOwner,
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("FamilyReport", created.GetProperty("source").GetString());
        Assert.Equal("NeedsConfirmation", created.GetProperty("level").GetString());
        Assert.Equal("PendingConfirmation", created.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, created.GetProperty("currentOwnerUserId").ValueKind);

        var retry = await client.PostAsJsonAsync(
            "/api/v1/care-events",
            new
            {
                clientRequestId,
                elderId = factory.MainElderId,
                summary = "家属报告暂时联系不上老人",
                occurredAt = DateTimeOffset.UtcNow,
            });
        retry.EnsureSuccessStatusCode();
        var retried = await retry.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(created.GetProperty("id").GetGuid(), retried.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Detail_contains_persisted_evidence_transitions_and_attempts()
    {
        await using var factory = new CommunityCareWebFactory();
        using var client = factory.CreateAuthenticatedClient(
            DemoRole.CommunityStaff,
            areaCode: "A01");
        var created = await CreateEventAsync(client, factory.MainElderId, "ExplicitSos");
        var eventId = created.GetProperty("id").GetGuid();
        var acceptResponse = await client.PostAsync(
            $"/api/v1/care-events/{eventId}/accept",
            content: null);
        acceptResponse.EnsureSuccessStatusCode();

        var detailResponse = await client.GetAsync($"/api/v1/care-events/{eventId}");

        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        var detail = await detailResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotEmpty(detail.GetProperty("evidence").EnumerateArray());
        Assert.NotEmpty(detail.GetProperty("transitions").EnumerateArray());
        Assert.NotEmpty(detail.GetProperty("contactAttempts").EnumerateArray());
        Assert.Contains(
            "InProgress",
            detail.GetProperty("allowedTransitions").EnumerateArray()
                .Select(item => item.GetString()));
    }

    [Fact]
    public async Task Missed_check_in_worker_rerun_returns_the_original_event()
    {
        await using var factory = new CommunityCareWebFactory();
        var worker = new MissedCheckInWorker(
            factory.Services.GetRequiredService<IServiceScopeFactory>(),
            TimeProvider.System,
            NullLogger<MissedCheckInWorker>.Instance);

        await worker.RunOnceAsync(CancellationToken.None);
        await worker.RunOnceAsync(CancellationToken.None);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CommunityCareDbContext>();
        var missed = await dbContext.CareEvents.AsNoTracking()
            .Where(careEvent =>
                careEvent.ElderId == factory.MainElderId &&
                careEvent.SourceEventId.StartsWith("missed-check-in:"))
            .ToListAsync();
        Assert.Single(missed);
    }

    [Fact]
    public async Task Contact_escalation_worker_rerun_does_not_duplicate_attempts()
    {
        await using var factory = new CommunityCareWebFactory();
        using var client = factory.CreateAuthenticatedClient(
            DemoRole.CommunityStaff,
            areaCode: "A01");
        var created = await CreateEventAsync(client, factory.MainElderId, "DeviceAnomaly");
        var eventId = created.GetProperty("id").GetGuid();
        var createdAt = created.GetProperty("createdAt").GetDateTimeOffset();
        var timeProvider = new FixedTimeProvider(createdAt.AddMinutes(11));
        var worker = new ContactEscalationWorker(
            factory.Services.GetRequiredService<IServiceScopeFactory>(),
            timeProvider,
            EscalationPolicy.Demo,
            NullLogger<ContactEscalationWorker>.Instance);

        await worker.RunOnceAsync(CancellationToken.None);
        await worker.RunOnceAsync(CancellationToken.None);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CommunityCareDbContext>();
        var stored = await dbContext.CareEvents.AsNoTracking()
            .Include(careEvent => careEvent.ContactAttempts)
            .SingleAsync(careEvent => careEvent.Id == eventId);
        Assert.Equal(CareEventStatus.UnableToConfirm, stored.Status);
        Assert.Equal(
            stored.ContactAttempts.Count,
            stored.ContactAttempts.Select(attempt => attempt.DeterministicAttemptId).Distinct().Count());
        Assert.Equal(4, stored.ContactAttempts.Count);
    }

    private static async Task<JsonElement> CreateEventAsync(
        HttpClient client,
        Guid elderId,
        string trigger)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/care-events",
            new
            {
                clientRequestId = Guid.NewGuid(),
                elderId,
                trigger,
                summary = "演示照料事件",
                occurredAt = DateTimeOffset.UtcNow,
            });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<string?> ReadProblemCodeAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
