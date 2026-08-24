using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CommunityElderCare.Core.Devices;
using CommunityElderCare.Core.Identity;
using CommunityElderCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CommunityElderCare.IntegrationTests;

public sealed class DeviceEndpointTests
{
    [Fact]
    public async Task Valid_device_token_accepts_one_idempotent_signal()
    {
        await using var factory = new CommunityCareWebFactory();
        using var client = factory.CreateClient();
        var eventId = Guid.NewGuid();
        var payload = SignalPayload(eventId, "SosButton", "Held2Seconds");

        using var firstRequest = DeviceRequest(payload, CommunityCareWebFactory.TestDeviceToken);
        using var secondRequest = DeviceRequest(payload, CommunityCareWebFactory.TestDeviceToken);
        var firstResponse = await client.SendAsync(firstRequest);
        var secondResponse = await client.SendAsync(secondRequest);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        var first = await firstResponse.Content.ReadFromJsonAsync<JsonElement>();
        var second = await secondResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(first.GetProperty("signalId").GetGuid(), second.GetProperty("signalId").GetGuid());
        Assert.False(first.GetProperty("isDuplicate").GetBoolean());
        Assert.True(second.GetProperty("isDuplicate").GetBoolean());

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CommunityCareDbContext>();
        Assert.Single(await db.DeviceSignals.ToListAsync());
        var device = await db.Devices.SingleAsync();
        Assert.NotEqual(CommunityCareWebFactory.TestDeviceToken, device.TokenHash);
        Assert.Equal(64, device.TokenHash!.Length);
        Assert.Single(await db.CareEventEvidence.Where(item =>
            item.SourceEventId == $"Device:{DemoDeviceIds.MainSosDevice:N}:{eventId:N}").ToListAsync());
    }

    [Theory]
    [InlineData("wrong-token")]
    [InlineData("")]
    public async Task Invalid_device_token_is_unauthorized_and_stores_nothing(string token)
    {
        await using var factory = new CommunityCareWebFactory();
        using var client = factory.CreateClient();
        using var request = DeviceRequest(SignalPayload(Guid.NewGuid(), "SosButton", "Held2Seconds"), token);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("INVALID_DEVICE_TOKEN", await ReadProblemCodeAsync(response));
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CommunityCareDbContext>();
        Assert.Empty(await db.DeviceSignals.ToListAsync());
    }

    [Fact]
    public async Task Unknown_device_does_not_reveal_registration_state()
    {
        await using var factory = new CommunityCareWebFactory();
        using var client = factory.CreateClient();
        var payload = new
        {
            deviceId = Guid.NewGuid(),
            eventId = Guid.NewGuid(),
            deviceTime = DateTimeOffset.UtcNow,
            signalType = "DeviceOffline",
            buttonState = (string?)null,
        };
        using var request = DeviceRequest(payload, CommunityCareWebFactory.TestDeviceToken);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("INVALID_DEVICE_TOKEN", await ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Administrator_simulator_uses_gateway_and_marks_signal_as_simulated()
    {
        await using var factory = new CommunityCareWebFactory();
        using var family = factory.CreateAuthenticatedClient(DemoRole.Family, familyFor: factory.MainElderId);
        using var administrator = factory.CreateAuthenticatedClient(DemoRole.Administrator);
        var payload = SignalPayload(Guid.NewGuid(), "NoWaterActivity", buttonState: null);

        var forbidden = await family.PostAsJsonAsync("/api/v1/demo/device-signals", payload);
        var accepted = await administrator.PostAsJsonAsync("/api/v1/demo/device-signals", payload);

        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        var response = await accepted.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(response.GetProperty("isSimulation").GetBoolean());
        Assert.NotEqual(Guid.Empty, response.GetProperty("careEventId").GetGuid());
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CommunityCareDbContext>();
        var stored = await db.DeviceSignals.SingleAsync();
        Assert.True(stored.IsSimulation);
    }

    private static object SignalPayload(Guid eventId, string signalType, string? buttonState) => new
    {
        deviceId = DemoDeviceIds.MainSosDevice,
        eventId,
        deviceTime = DateTimeOffset.UtcNow,
        signalType,
        buttonState,
    };

    private static HttpRequestMessage DeviceRequest(object payload, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/device-signals")
        {
            Content = JsonContent.Create(payload),
        };
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Add("X-Device-Token", token);
        }
        return request;
    }

    private static async Task<string?> ReadProblemCodeAsync(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        return problem.GetProperty("code").GetString();
    }
}
