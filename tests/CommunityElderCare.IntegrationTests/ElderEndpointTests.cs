using System.Net.Http.Json;
using System.Net;
using System.Text.Json;
using CommunityElderCare.Api.Contracts.Elders;
using CommunityElderCare.Core.Identity;
using CommunityElderCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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

    [Fact]
    public async Task Family_detail_omits_ungranted_health_fields()
    {
        await using var factory = new CommunityCareWebFactory();
        using var client = factory.CreateAuthenticatedClient(
            DemoRole.Family,
            familyFor: factory.MainElderId);

        var response = await client.GetAsync($"/api/v1/elders/{factory.MainElderId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(json.TryGetProperty("healthRisks", out _));
        Assert.True(json.TryGetProperty("recentStatus", out _));
    }

    [Theory]
    [InlineData(DemoRole.Family, "A01", HttpStatusCode.Forbidden, "FORBIDDEN_SCOPE")]
    [InlineData(DemoRole.CommunityStaff, "A02", HttpStatusCode.Forbidden, "FORBIDDEN_SCOPE")]
    public async Task Care_profile_update_rejects_forbidden_scope(
        DemoRole role,
        string areaCode,
        HttpStatusCode expectedStatus,
        string expectedCode)
    {
        await using var factory = new CommunityCareWebFactory();
        using var client = factory.CreateAuthenticatedClient(
            role,
            areaCode: areaCode,
            familyFor: role == DemoRole.Family ? factory.MainElderId : null);

        var response = await client.PutAsJsonAsync(
            $"/api/v1/elders/{factory.MainElderId}/care-profile",
            ValidCareProfileUpdate("演示资料调整"));

        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal(expectedCode, await ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Care_profile_update_requires_reason()
    {
        await using var factory = new CommunityCareWebFactory();
        using var client = factory.CreateAuthenticatedClient(
            DemoRole.CommunityStaff,
            areaCode: "A01");

        var response = await client.PutAsJsonAsync(
            $"/api/v1/elders/{factory.MainElderId}/care-profile",
            ValidCareProfileUpdate(string.Empty));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("REASON_REQUIRED", await ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Care_profile_update_replaces_all_collections_and_records_reason()
    {
        await using var factory = new CommunityCareWebFactory();
        using var client = factory.CreateAuthenticatedClient(
            DemoRole.CommunityStaff,
            areaCode: "A01");

        var response = await client.PutAsJsonAsync(
            $"/api/v1/elders/{factory.MainElderId}/care-profile",
            ValidCareProfileUpdate("社区复核演示档案"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CommunityCareDbContext>();
        var audit = await dbContext.AccessAuditRecords.SingleAsync(record =>
            record.ElderId == factory.MainElderId && record.Action == "CARE_PROFILE_UPDATED");
        Assert.Equal("社区复核演示档案", audit.Reason);
        Assert.NotEqual(Guid.Empty, audit.ActorUserId);
    }

    private static object ValidCareProfileUpdate(string reason) => new
    {
        attentionLevel = "Priority",
        healthRisks = new[] { new { code = "FALL_ATTENTION", demoLabel = "跌倒风险关注" } },
        serviceNeeds = new[] { new { code = "HOME_VISIT", demoLabel = "上门探访" } },
        emergencyContacts = new[]
        {
            new
            {
                demoName = "联系人99",
                relationship = "子女",
                phoneNumber = "19900009999",
                contactOrder = 1,
            },
        },
        reason,
    };

    private static async Task<string?> ReadProblemCodeAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.TryGetProperty("code", out var code) ? code.GetString() : null;
    }
}
