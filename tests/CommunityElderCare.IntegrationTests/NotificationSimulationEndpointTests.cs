using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CommunityElderCare.Core.Identity;
using CommunityElderCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CommunityElderCare.IntegrationTests;

public sealed class NotificationSimulationEndpointTests
{
    [Fact]
    public async Task Http_retry_is_idempotent_and_operator_retry_preserves_both_attempts()
    {
        await using var factory = new CommunityCareWebFactory();
        using var staff = factory.CreateAuthenticatedClient(DemoRole.CommunityStaff, areaCode: "A01");
        var eventId = await CreateAndAcceptAsync(staff, factory.MainElderId);
        var failedRequestId = Guid.NewGuid();
        var failedPayload = new
        {
            requestId = failedRequestId,
            channel = "Phone",
            recipientRole = "Family",
            simulateFailure = true,
        };

        var first = await staff.PostAsJsonAsync(
            $"/api/v1/care-events/{eventId}/simulation-attempts",
            failedPayload);
        var duplicate = await staff.PostAsJsonAsync(
            $"/api/v1/care-events/{eventId}/simulation-attempts",
            failedPayload);
        var retry = await staff.PostAsJsonAsync(
            $"/api/v1/care-events/{eventId}/simulation-attempts",
            new
            {
                requestId = Guid.NewGuid(),
                channel = "Phone",
                recipientRole = "Family",
                simulateFailure = false,
            });

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, duplicate.StatusCode);
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        var firstBody = await first.Content.ReadFromJsonAsync<JsonElement>();
        var duplicateBody = await duplicate.Content.ReadFromJsonAsync<JsonElement>();
        var retryBody = await retry.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(firstBody.GetProperty("attemptId").GetGuid(), duplicateBody.GetProperty("attemptId").GetGuid());
        Assert.True(duplicateBody.GetProperty("isDuplicate").GetBoolean());
        Assert.NotEqual(firstBody.GetProperty("attemptId").GetGuid(), retryBody.GetProperty("attemptId").GetGuid());
        Assert.True(firstBody.GetProperty("isSimulation").GetBoolean());
        Assert.Equal("模拟失败", firstBody.GetProperty("outcome").GetString());
        Assert.Equal("模拟送达", retryBody.GetProperty("outcome").GetString());

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CommunityCareDbContext>();
        Assert.Equal(2, await db.NotificationAttempts.CountAsync());
    }

    [Fact]
    public async Task Wrong_area_staff_cannot_record_simulated_contact()
    {
        await using var factory = new CommunityCareWebFactory();
        using var owner = factory.CreateAuthenticatedClient(DemoRole.CommunityStaff, areaCode: "A01");
        using var wrongArea = factory.CreateAuthenticatedClient(DemoRole.CommunityStaff, areaCode: "A02");
        var eventId = await CreateAndAcceptAsync(owner, factory.MainElderId);

        var response = await wrongArea.PostAsJsonAsync(
            $"/api/v1/care-events/{eventId}/simulation-attempts",
            new
            {
                requestId = Guid.NewGuid(),
                channel = "Sms",
                recipientRole = "Family",
                simulateFailure = false,
            });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static async Task<Guid> CreateAndAcceptAsync(HttpClient staff, Guid elderId)
    {
        var createdResponse = await staff.PostAsJsonAsync("/api/v1/care-events", new
        {
            clientRequestId = Guid.NewGuid(),
            elderId,
            trigger = "StaffObservation",
            summary = "工作人员需要确认老人状态",
            occurredAt = DateTimeOffset.UtcNow,
        });
        createdResponse.EnsureSuccessStatusCode();
        var created = await createdResponse.Content.ReadFromJsonAsync<JsonElement>();
        var eventId = created.GetProperty("id").GetGuid();
        (await staff.PostAsync($"/api/v1/care-events/{eventId}/accept", null)).EnsureSuccessStatusCode();
        return eventId;
    }
}
