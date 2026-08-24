using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CommunityElderCare.Core.Identity;
using CommunityElderCare.Infrastructure.Identity;

namespace CommunityElderCare.IntegrationTests;

public sealed class BreakGlassEndpointTests
{
    [Fact]
    public async Task Assigned_community_staff_can_open_bounded_cross_area_access()
    {
        await using var factory = new CommunityCareWebFactory();
        using var client = factory.CreateAuthenticatedClient(
            DemoRole.CommunityStaff,
            areaCode: "A02",
            assignedTaskId: DemoIdentitySeed.MainCareTaskId);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/elders/{factory.MainElderId}/break-glass",
            new { reason = "跨片区紧急协助演示", durationMinutes = 15 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detailResponse = await client.GetAsync($"/api/v1/elders/{factory.MainElderId}");
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        var detail = await detailResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(detail.TryGetProperty("healthRisks", out _));
    }

    [Theory]
    [InlineData("", 15, "REASON_REQUIRED")]
    [InlineData("时间过长", 16, "INVALID_BREAK_GLASS_DURATION")]
    public async Task Break_glass_rejects_invalid_request(
        string reason,
        int durationMinutes,
        string expectedCode)
    {
        await using var factory = new CommunityCareWebFactory();
        using var client = factory.CreateAuthenticatedClient(
            DemoRole.CommunityStaff,
            areaCode: "A02",
            assignedTaskId: DemoIdentitySeed.MainCareTaskId);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/elders/{factory.MainElderId}/break-glass",
            new { reason, durationMinutes });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(expectedCode, json.GetProperty("code").GetString());
    }
}
