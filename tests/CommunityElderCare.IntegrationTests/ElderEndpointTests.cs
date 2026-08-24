using System.Net.Http.Json;
using System.Text.Json;
using CommunityElderCare.Api.Contracts.Elders;
using CommunityElderCare.Core.Identity;

namespace CommunityElderCare.IntegrationTests;

public sealed class ElderEndpointTests
{
    [Fact]
    public async Task High_attention_filter_returns_demo_profiles_only()
    {
        await using var factory = new CommunityCareWebFactory();
        using var client = factory.CreateAuthenticatedClient(
            DemoRole.CommunityStaff,
            areaCode: "A01");

        var elders = await client.GetFromJsonAsync<List<ElderSummaryResponse>>(
            "/api/v1/elders?attentionLevel=High");

        Assert.NotNull(elders);
        Assert.NotEmpty(elders);
        Assert.All(elders, elder => Assert.True(elder.IsDemoData));
        Assert.All(elders, elder => Assert.Equal("High", elder.AttentionLevel));
        Assert.All(elders, elder => Assert.Equal("A01", elder.AreaCode));
    }

    [Fact]
    public async Task Summary_omits_detail_fields_and_detail_returns_demo_collections()
    {
        await using var factory = new CommunityCareWebFactory();
        using var client = factory.CreateAuthenticatedClient(
            DemoRole.CommunityStaff,
            areaCode: "A01");

        using var listResponse = await client.GetAsync("/api/v1/elders");
        listResponse.EnsureSuccessStatusCode();
        using var listJson = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
        var firstSummary = listJson.RootElement[0];
        Assert.False(firstSummary.TryGetProperty("healthRisks", out _));
        Assert.False(firstSummary.TryGetProperty("serviceNeeds", out _));
        Assert.False(firstSummary.TryGetProperty("emergencyContacts", out _));

        var elderId = firstSummary.GetProperty("id").GetGuid();
        var detail = await client.GetFromJsonAsync<ElderDetailResponse>($"/api/v1/elders/{elderId}");

        Assert.NotNull(detail);
        Assert.True(detail.IsDemoData);
        Assert.NotEmpty(detail.HealthRisks);
        Assert.NotEmpty(detail.ServiceNeeds);
        Assert.All(detail.EmergencyContacts, contact =>
            Assert.Matches("^1990000[0-9]{4}$", contact.PhoneNumber));
    }
}
