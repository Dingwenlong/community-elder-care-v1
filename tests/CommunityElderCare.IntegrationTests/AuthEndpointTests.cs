using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CommunityElderCare.IntegrationTests;

public sealed class AuthEndpointTests
{
    [Fact]
    public async Task Demo_login_returns_role_shell_and_nonempty_jwt()
    {
        await using var factory = new CommunityCareWebFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            username = "family.demo",
            password = "DemoPassword!2026",
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrWhiteSpace(json.GetProperty("accessToken").GetString()));
        Assert.Equal("Family", json.GetProperty("role").GetString());
        Assert.Equal("mobile-family", json.GetProperty("shell").GetString());
        Assert.True(json.GetProperty("isDemoMode").GetBoolean());
    }

    [Fact]
    public async Task Wrong_demo_password_returns_stable_unauthorized_code()
    {
        await using var factory = new CommunityCareWebFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            username = "family.demo",
            password = "wrong-password",
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("INVALID_CREDENTIALS", json.GetProperty("code").GetString());
    }
}
