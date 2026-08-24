using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CommunityElderCare.Core.Identity;
using CommunityElderCare.Infrastructure.Identity;

namespace CommunityElderCare.IntegrationTests;

public sealed class FamilyEndpointTests
{
    [Fact]
    public async Task Summary_returns_only_the_family_grant_and_safe_text()
    {
        await using var factory = new CommunityCareWebFactory();
        using var client = factory.CreateAuthenticatedClient(
            DemoRole.Family,
            familyFor: factory.MainElderId);

        var response = await client.GetAsync(
            $"/api/v1/family/elders/{factory.MainElderId}/summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var fields = json.GetProperty("grantedFields")
            .EnumerateArray()
            .Select(value => value.GetString())
            .ToHashSet();
        Assert.Contains("RecentStatus", fields);
        Assert.Contains("CareEventSummary", fields);
        Assert.Contains("VisitSummary", fields);
        Assert.True(json.TryGetProperty("consentExpiresAt", out _));
        Assert.False(json.TryGetProperty("rawAiText", out _));
        Assert.False(json.TryGetProperty("internalNote", out _));
        Assert.False(json.TryGetProperty("responsibilityQueue", out _));
    }

    [Fact]
    public async Task Revocation_denies_the_next_family_summary_read()
    {
        await using var factory = new CommunityCareWebFactory();
        using var elderClient = factory.CreateAuthenticatedClient(DemoRole.Elder);
        using var familyClient = factory.CreateAuthenticatedClient(
            DemoRole.Family,
            familyFor: factory.MainElderId);

        var revoke = await elderClient.DeleteAsync(
            $"/api/v1/elders/{factory.MainElderId}/consents/{DemoIdentitySeed.FamilyUserId}");
        var response = await familyClient.GetAsync(
            $"/api/v1/family/elders/{factory.MainElderId}/summary");

        Assert.Equal(HttpStatusCode.NoContent, revoke.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("CONSENT_REQUIRED", await ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Care_records_return_confirmed_summary_and_never_raw_staff_note()
    {
        await using var factory = new CommunityCareWebFactory();
        using var familyClient = factory.CreateAuthenticatedClient(
            DemoRole.Family,
            familyFor: factory.MainElderId);
        using var staffClient = factory.CreateAuthenticatedClient(DemoRole.CommunityStaff);
        var now = DateTimeOffset.UtcNow;

        var createEvent = await familyClient.PostAsJsonAsync(
            "/api/v1/care-events/",
            new
            {
                clientRequestId = Guid.NewGuid(),
                elderId = factory.MainElderId,
                summary = "家属报告联系不上老人",
                occurredAt = now,
            });
        createEvent.EnsureSuccessStatusCode();
        var eventJson = await createEvent.Content.ReadFromJsonAsync<JsonElement>();
        var eventId = eventJson.GetProperty("id").GetGuid();

        (await staffClient.PostAsync($"/api/v1/care-events/{eventId}/accept", null))
            .EnsureSuccessStatusCode();
        var createVisit = await staffClient.PostAsJsonAsync(
            $"/api/v1/care-events/{eventId}/visits",
            new
            {
                assignedStaffUserId = DemoIdentitySeed.CommunityUserId,
                scheduledStartAt = now.AddMinutes(5),
                scheduledEndAt = now.AddMinutes(35),
                isMandatory = true,
            });
        createVisit.EnsureSuccessStatusCode();
        var visitJson = await createVisit.Content.ReadFromJsonAsync<JsonElement>();
        var visitId = visitJson.GetProperty("visitId").GetGuid();
        (await staffClient.PostAsync($"/api/v1/visits/{visitId}/start", null))
            .EnsureSuccessStatusCode();
        (await staffClient.PostAsJsonAsync(
            $"/api/v1/visits/{visitId}/complete",
            new
            {
                rawStaffNote = "社区内部备注不得返回",
                confirmedSummary = "老人状态平稳",
                result = "完成探访",
            })).EnsureSuccessStatusCode();

        var response = await familyClient.GetAsync(
            $"/api/v1/family/elders/{factory.MainElderId}/care-records");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var records = await response.Content.ReadFromJsonAsync<JsonElement>();
        var visit = Assert.Single(records.EnumerateArray());
        Assert.Equal("Visit", visit.GetProperty("kind").GetString());
        Assert.Equal("老人状态平稳", visit.GetProperty("summary").GetString());
        Assert.DoesNotContain("社区内部备注", records.ToString(), StringComparison.Ordinal);
    }

    private static async Task<string?> ReadProblemCodeAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.TryGetProperty("code", out var code) ? code.GetString() : null;
    }
}
