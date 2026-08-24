using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CommunityElderCare.Core.Identity;
using CommunityElderCare.Infrastructure.Identity;

namespace CommunityElderCare.IntegrationTests;

public sealed class ConsentEndpointTests
{
    [Fact]
    public async Task Elder_can_list_current_consent_records()
    {
        await using var factory = new CommunityCareWebFactory();
        using var elderClient = factory.CreateAuthenticatedClient(DemoRole.Elder);

        var response = await elderClient.GetAsync($"/api/v1/elders/{factory.MainElderId}/consents");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var records = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotEqual(0, records.GetArrayLength());
        Assert.True(records[0].GetProperty("isDemoData").GetBoolean());
    }

    [Fact]
    public async Task Elder_grant_then_revoke_changes_the_next_family_read()
    {
        await using var factory = new CommunityCareWebFactory();
        using var elderClient = factory.CreateAuthenticatedClient(DemoRole.Elder);
        using var familyClient = factory.CreateAuthenticatedClient(
            DemoRole.Family,
            familyFor: factory.MainElderId);
        var consentUrl = $"/api/v1/elders/{factory.MainElderId}/consents/{DemoIdentitySeed.FamilyUserId}";

        var grantResponse = await elderClient.PutAsJsonAsync(consentUrl, new
        {
            fields = new[] { "RecentStatus", "HealthRiskSummary" },
            expiresAt = DateTimeOffset.UtcNow.AddHours(1),
        });

        Assert.Equal(HttpStatusCode.OK, grantResponse.StatusCode);
        var allowedResponse = await familyClient.GetAsync($"/api/v1/elders/{factory.MainElderId}");
        Assert.Equal(HttpStatusCode.OK, allowedResponse.StatusCode);
        var allowedJson = await allowedResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(allowedJson.TryGetProperty("healthRisks", out _));

        var revokeResponse = await elderClient.DeleteAsync(consentUrl);

        Assert.Equal(HttpStatusCode.NoContent, revokeResponse.StatusCode);
        var deniedResponse = await familyClient.GetAsync($"/api/v1/elders/{factory.MainElderId}");
        Assert.Equal(HttpStatusCode.Forbidden, deniedResponse.StatusCode);
        Assert.Equal("CONSENT_REQUIRED", await ReadProblemCodeAsync(deniedResponse));
    }

    [Fact]
    public async Task Family_actor_cannot_grant_own_consent()
    {
        await using var factory = new CommunityCareWebFactory();
        using var familyClient = factory.CreateAuthenticatedClient(
            DemoRole.Family,
            familyFor: factory.MainElderId);

        var response = await familyClient.PutAsJsonAsync(
            $"/api/v1/elders/{factory.MainElderId}/consents/{DemoIdentitySeed.FamilyUserId}",
            new
            {
                fields = new[] { "HealthRiskSummary" },
                expiresAt = DateTimeOffset.UtcNow.AddHours(1),
            });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("FORBIDDEN_SCOPE", await ReadProblemCodeAsync(response));
    }

    private static async Task<string?> ReadProblemCodeAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.TryGetProperty("code", out var code) ? code.GetString() : null;
    }
}
