using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CommunityElderCare.Core.Identity;
using CommunityElderCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CommunityElderCare.IntegrationTests;

public sealed class CheckInEndpointTests
{
    [Fact]
    public async Task Elder_retry_returns_original_check_in_once()
    {
        await using var factory = new CommunityCareWebFactory();
        using var client = factory.CreateAuthenticatedClient(DemoRole.Elder);
        var requestId = Guid.NewGuid();
        var payload = new { requestId, clientTime = DateTimeOffset.UtcNow.AddSeconds(-5) };

        var firstResponse = await client.PostAsJsonAsync(
            $"/api/v1/elders/{factory.MainElderId}/check-ins",
            payload);
        var secondResponse = await client.PostAsJsonAsync(
            $"/api/v1/elders/{factory.MainElderId}/check-ins",
            payload);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        var first = await firstResponse.Content.ReadFromJsonAsync<JsonElement>();
        var second = await secondResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(first.GetProperty("id").GetGuid(), second.GetProperty("id").GetGuid());
        Assert.False(first.GetProperty("isDuplicate").GetBoolean());
        Assert.True(second.GetProperty("isDuplicate").GetBoolean());
        Assert.True(first.TryGetProperty("receivedAt", out _));

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CommunityCareDbContext>();
        Assert.Equal(1, await dbContext.CheckIns.CountAsync(checkIn => checkIn.RequestId == requestId));
    }

    [Fact]
    public async Task Family_cannot_submit_elder_check_in()
    {
        await using var factory = new CommunityCareWebFactory();
        using var client = factory.CreateAuthenticatedClient(
            DemoRole.Family,
            familyFor: factory.MainElderId);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/elders/{factory.MainElderId}/check-ins",
            new { requestId = Guid.NewGuid(), clientTime = DateTimeOffset.UtcNow });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("FORBIDDEN_SCOPE", await ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Community_manual_confirmation_requires_reason()
    {
        await using var factory = new CommunityCareWebFactory();
        using var client = factory.CreateAuthenticatedClient(DemoRole.CommunityStaff, areaCode: "A01");

        var response = await client.PostAsJsonAsync(
            $"/api/v1/elders/{factory.MainElderId}/check-ins",
            new { requestId = Guid.NewGuid(), clientTime = DateTimeOffset.UtcNow });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("REASON_REQUIRED", await ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Community_manual_confirmation_persists_reason_and_server_time()
    {
        await using var factory = new CommunityCareWebFactory();
        using var client = factory.CreateAuthenticatedClient(DemoRole.CommunityStaff, areaCode: "A01");

        var response = await client.PostAsJsonAsync(
            $"/api/v1/elders/{factory.MainElderId}/check-ins?reason=电话确认老人平安",
            new { requestId = Guid.NewGuid(), clientTime = DateTimeOffset.UtcNow.AddMinutes(-2) });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("CommunityManual", json.GetProperty("kind").GetString());
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CommunityCareDbContext>();
        var stored = await dbContext.CheckIns.SingleAsync(checkIn =>
            checkIn.Id == json.GetProperty("id").GetGuid());
        Assert.Equal("电话确认老人平安", stored.ManualReason);
        Assert.NotEqual(stored.ClientTime, stored.ReceivedAt);
    }

    [Fact]
    public async Task Today_contains_only_persisted_receipts_and_seeded_reminder_states()
    {
        await using var factory = new CommunityCareWebFactory();
        using var client = factory.CreateAuthenticatedClient(DemoRole.Elder);
        var persistedRequestId = Guid.NewGuid();
        var neverReceivedRequestId = Guid.NewGuid();
        var response = await client.PostAsJsonAsync(
            $"/api/v1/elders/{factory.MainElderId}/check-ins",
            new { requestId = persistedRequestId, clientTime = DateTimeOffset.UtcNow });
        response.EnsureSuccessStatusCode();

        var todayResponse = await client.GetAsync($"/api/v1/elders/{factory.MainElderId}/today");

        Assert.Equal(HttpStatusCode.OK, todayResponse.StatusCode);
        var today = await todayResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(today.TryGetProperty("serverTime", out _));
        var checkIns = today.GetProperty("checkIns");
        Assert.Contains(checkIns.EnumerateArray(), item =>
            item.GetProperty("requestId").GetGuid() == persistedRequestId &&
            item.TryGetProperty("receivedAt", out _));
        Assert.DoesNotContain(checkIns.EnumerateArray(), item =>
            item.GetProperty("requestId").GetGuid() == neverReceivedRequestId);
        Assert.Equal(4, today.GetProperty("reminders").GetArrayLength());
    }

    [Fact]
    public async Task Reminder_completion_retry_returns_original_state()
    {
        await using var factory = new CommunityCareWebFactory();
        using var client = factory.CreateAuthenticatedClient(DemoRole.Elder);
        var today = await client.GetFromJsonAsync<JsonElement>(
            $"/api/v1/elders/{factory.MainElderId}/today");
        var reminderId = today.GetProperty("reminders")[0].GetProperty("id").GetGuid();
        var requestId = Guid.NewGuid();

        var firstResponse = await client.PostAsJsonAsync(
            $"/api/v1/reminders/{reminderId}/complete",
            new { requestId });
        var secondResponse = await client.PostAsJsonAsync(
            $"/api/v1/reminders/{reminderId}/complete",
            new { requestId });

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        var first = await firstResponse.Content.ReadFromJsonAsync<JsonElement>();
        var second = await secondResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(first.GetProperty("isDuplicate").GetBoolean());
        Assert.True(second.GetProperty("isDuplicate").GetBoolean());
        Assert.Equal(
            first.GetProperty("completedAt").GetDateTimeOffset(),
            second.GetProperty("completedAt").GetDateTimeOffset());
    }

    [Fact]
    public async Task Snooze_is_bounded_and_does_not_complete_reminder()
    {
        await using var factory = new CommunityCareWebFactory();
        using var client = factory.CreateAuthenticatedClient(DemoRole.Elder);
        var today = await client.GetFromJsonAsync<JsonElement>(
            $"/api/v1/elders/{factory.MainElderId}/today");
        var reminderId = today.GetProperty("reminders")[0].GetProperty("id").GetGuid();

        var invalidResponse = await client.PostAsJsonAsync(
            $"/api/v1/reminders/{reminderId}/snooze",
            new { requestId = Guid.NewGuid(), nextReminderAt = DateTimeOffset.UtcNow.AddMinutes(1) });
        Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);
        Assert.Equal("INVALID_SNOOZE_TIME", await ReadProblemCodeAsync(invalidResponse));

        var validResponse = await client.PostAsJsonAsync(
            $"/api/v1/reminders/{reminderId}/snooze",
            new { requestId = Guid.NewGuid(), nextReminderAt = DateTimeOffset.UtcNow.AddMinutes(10) });
        Assert.Equal(HttpStatusCode.OK, validResponse.StatusCode);
        var valid = await validResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Null, valid.GetProperty("completedAt").ValueKind);
    }

    private static async Task<string?> ReadProblemCodeAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.TryGetProperty("code", out var code) ? code.GetString() : null;
    }
}
