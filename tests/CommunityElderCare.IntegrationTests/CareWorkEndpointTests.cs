using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CommunityElderCare.Core.Identity;
using CommunityElderCare.Infrastructure.Identity;

namespace CommunityElderCare.IntegrationTests;

public sealed class CareWorkEndpointTests
{
    [Fact]
    public async Task Visit_follow_up_and_event_form_a_guarded_closure_flow()
    {
        await using var factory = new CommunityCareWebFactory();
        using var staffClient = factory.CreateAuthenticatedClient(
            DemoRole.CommunityStaff,
            areaCode: "A01");
        var careEvent = await CreateAndAcceptEventAsync(staffClient, factory.MainElderId);
        var eventId = careEvent.GetProperty("id").GetGuid();

        var visitResponse = await staffClient.PostAsJsonAsync(
            $"/api/v1/care-events/{eventId}/visits",
            new
            {
                assignedStaffUserId = DemoIdentitySeed.CommunityUserId,
                scheduledStartAt = DateTimeOffset.UtcNow.AddHours(1),
                scheduledEndAt = DateTimeOffset.UtcNow.AddHours(2),
                isMandatory = true,
            });
        Assert.Equal(HttpStatusCode.OK, visitResponse.StatusCode);
        var visit = await visitResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Assigned", visit.GetProperty("status").GetString());
        var visitId = visit.GetProperty("visitId").GetGuid();

        var startResponse = await staffClient.PostAsync(
            $"/api/v1/visits/{visitId}/start",
            content: null);
        Assert.Equal(HttpStatusCode.OK, startResponse.StatusCode);
        var started = await startResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("InProgress", started.GetProperty("status").GetString());
        var afterStart = await staffClient.GetFromJsonAsync<JsonElement>(
            $"/api/v1/care-events/{eventId}");
        Assert.Equal("InProgress", afterStart.GetProperty("status").GetString());

        var completeVisitResponse = await staffClient.PostAsJsonAsync(
            $"/api/v1/visits/{visitId}/complete",
            new
            {
                rawStaffNote = "演示原始探访记录",
                confirmedSummary = "已当面确认老人状态",
                result = "探访完成",
            });
        Assert.Equal(HttpStatusCode.OK, completeVisitResponse.StatusCode);
        var completedVisit = await completeVisitResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Completed", completedVisit.GetProperty("status").GetString());

        var visitList = await staffClient.GetFromJsonAsync<JsonElement>(
            $"/api/v1/visits?careEventId={eventId}");
        var listedVisit = Assert.Single(visitList.EnumerateArray());
        Assert.Equal(visitId, listedVisit.GetProperty("visitId").GetGuid());
        Assert.Equal("演示·李安康", listedVisit.GetProperty("elderDisplayName").GetString());
        Assert.Equal("已当面确认老人状态", listedVisit.GetProperty("confirmedSummary").GetString());
        Assert.False(listedVisit.TryGetProperty("rawStaffNote", out _));
        using var otherAreaClient = factory.CreateAuthenticatedClient(
            DemoRole.CommunityStaff,
            areaCode: "A02");
        var otherAreaVisits = await otherAreaClient.GetFromJsonAsync<JsonElement>(
            $"/api/v1/visits?careEventId={eventId}");
        Assert.Empty(otherAreaVisits.EnumerateArray());

        var afterVisit = await staffClient.GetFromJsonAsync<JsonElement>(
            $"/api/v1/care-events/{eventId}");
        Assert.Equal("InProgress", afterVisit.GetProperty("status").GetString());
        Assert.Contains(
            afterVisit.GetProperty("evidence").EnumerateArray(),
            evidence => evidence.GetProperty("kind").GetString() == "VisitCompleted");

        var resolveResponse = await staffClient.PostAsJsonAsync(
            $"/api/v1/care-events/{eventId}/transitions",
            new
            {
                toStatus = "Resolved",
                reason = "现场探访已完成",
                resolution = "已完成状态确认",
            });
        Assert.Equal(HttpStatusCode.OK, resolveResponse.StatusCode);
        var resolved = await resolveResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Resolved", resolved.GetProperty("status").GetString());

        var followUpResponse = await staffClient.PostAsJsonAsync(
            $"/api/v1/care-events/{eventId}/follow-ups",
            new
            {
                assignedStaffUserId = DemoIdentitySeed.CommunityUserId,
                dueAt = DateTimeOffset.UtcNow.AddDays(1),
            });
        Assert.Equal(HttpStatusCode.OK, followUpResponse.StatusCode);
        var followUp = await followUpResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Assigned", followUp.GetProperty("status").GetString());
        var followUpId = followUp.GetProperty("followUpId").GetGuid();
        var awaitingFollowUp = await staffClient.GetFromJsonAsync<JsonElement>(
            $"/api/v1/care-events/{eventId}");
        Assert.Equal("FollowUpPending", awaitingFollowUp.GetProperty("status").GetString());

        var blockedClose = await staffClient.PostAsJsonAsync(
            $"/api/v1/care-events/{eventId}/transitions",
            new { toStatus = "Closed", reason = "申请结案" });
        Assert.Equal(HttpStatusCode.Conflict, blockedClose.StatusCode);
        Assert.Equal("CLOSE_GUARD_FAILED", await ReadProblemCodeAsync(blockedClose));

        var completeFollowUpResponse = await staffClient.PostAsJsonAsync(
            $"/api/v1/follow-ups/{followUpId}/complete",
            new { result = "随访已完成，状态稳定" });
        Assert.Equal(HttpStatusCode.OK, completeFollowUpResponse.StatusCode);
        var completedFollowUp = await completeFollowUpResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Completed", completedFollowUp.GetProperty("status").GetString());
        var followUpList = await staffClient.GetFromJsonAsync<JsonElement>(
            $"/api/v1/follow-ups?careEventId={eventId}");
        var listedFollowUp = Assert.Single(followUpList.EnumerateArray());
        Assert.Equal(followUpId, listedFollowUp.GetProperty("followUpId").GetGuid());
        Assert.Equal("演示·李安康", listedFollowUp.GetProperty("elderDisplayName").GetString());
        Assert.Equal("随访已完成，状态稳定", listedFollowUp.GetProperty("result").GetString());
        var readyToClose = await staffClient.GetFromJsonAsync<JsonElement>(
            $"/api/v1/care-events/{eventId}");
        Assert.Equal("FollowUpPending", readyToClose.GetProperty("status").GetString());

        var closeResponse = await staffClient.PostAsJsonAsync(
            $"/api/v1/care-events/{eventId}/transitions",
            new { toStatus = "Closed", reason = "随访完成后结案" });
        Assert.Equal(HttpStatusCode.OK, closeResponse.StatusCode);
        var closed = await closeResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Closed", closed.GetProperty("status").GetString());
        Assert.All(
            closed.GetProperty("transitions").EnumerateArray(),
            transition => Assert.Equal(
                DemoIdentitySeed.CommunityUserId,
                transition.GetProperty("actorUserId").GetGuid()));
    }

    [Fact]
    public async Task Service_worker_response_is_minimal_and_task_scoped()
    {
        await using var factory = new CommunityCareWebFactory();
        using var staffClient = factory.CreateAuthenticatedClient(
            DemoRole.CommunityStaff,
            areaCode: "A01");
        var careEvent = await CreateAndAcceptEventAsync(staffClient, factory.MainElderId);
        var eventId = careEvent.GetProperty("id").GetGuid();
        var createResponse = await staffClient.PostAsJsonAsync(
            $"/api/v1/care-events/{eventId}/service-orders",
            new
            {
                serviceType = "助餐配送",
                scheduledWindow = "10:00-11:00",
                contactInstruction = "到达门口后按演示流程联系",
                assignedWorkerUserId = DemoIdentitySeed.ServiceWorkerUserId,
                isMandatory = true,
            });
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
        var order = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var orderId = order.GetProperty("orderId").GetGuid();
        AssertMinimalWorkerResponse(order);

        var communityOrders = await staffClient.GetFromJsonAsync<JsonElement>(
            $"/api/v1/service-orders?careEventId={eventId}");
        var communityOrder = Assert.Single(communityOrders.EnumerateArray());
        Assert.Equal(orderId, communityOrder.GetProperty("orderId").GetGuid());
        Assert.Equal(eventId, communityOrder.GetProperty("careEventId").GetGuid());
        Assert.Equal("演示·李安康", communityOrder.GetProperty("elderDisplayName").GetString());

        using var wrongTaskClient = factory.CreateAuthenticatedClient(
            DemoRole.ServiceWorker,
            elderId: factory.MainElderId,
            assignedTaskId: Guid.NewGuid());
        var denied = await wrongTaskClient.PostAsync(
            $"/api/v1/service-orders/{orderId}/accept",
            content: null);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);

        using var workerClient = factory.CreateAuthenticatedClient(
            DemoRole.ServiceWorker,
            elderId: factory.MainElderId,
            assignedTaskId: orderId);
        var workerTasks = await workerClient.GetFromJsonAsync<JsonElement>(
            "/api/v1/service-orders/my-tasks");
        var workerTask = Assert.Single(workerTasks.EnumerateArray());
        Assert.Equal(orderId, workerTask.GetProperty("orderId").GetGuid());
        AssertMinimalWorkerResponse(workerTask);
        var communityListDenied = await workerClient.GetAsync("/api/v1/service-orders");
        Assert.Equal(HttpStatusCode.Forbidden, communityListDenied.StatusCode);

        var acceptedResponse = await workerClient.PostAsync(
            $"/api/v1/service-orders/{orderId}/accept",
            content: null);
        Assert.Equal(HttpStatusCode.OK, acceptedResponse.StatusCode);
        var accepted = await acceptedResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("InProgress", accepted.GetProperty("status").GetString());
        AssertMinimalWorkerResponse(accepted);

        var completedResponse = await workerClient.PostAsJsonAsync(
            $"/api/v1/service-orders/{orderId}/complete",
            new { result = "已完成演示助餐配送" });
        Assert.Equal(HttpStatusCode.OK, completedResponse.StatusCode);
        var completed = await completedResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Completed", completed.GetProperty("status").GetString());
        AssertMinimalWorkerResponse(completed);
    }

    private static async Task<JsonElement> CreateAndAcceptEventAsync(
        HttpClient client,
        Guid elderId)
    {
        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/care-events",
            new
            {
                clientRequestId = Guid.NewGuid(),
                elderId,
                trigger = "DeviceAnomaly",
                summary = "演示照料工作事件",
                occurredAt = DateTimeOffset.UtcNow,
            });
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var acceptResponse = await client.PostAsync(
            $"/api/v1/care-events/{created.GetProperty("id").GetGuid()}/accept",
            content: null);
        acceptResponse.EnsureSuccessStatusCode();
        return await acceptResponse.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static void AssertMinimalWorkerResponse(JsonElement response)
    {
        Assert.True(response.TryGetProperty("orderId", out _));
        Assert.True(response.TryGetProperty("elderDisplayName", out _));
        Assert.True(response.TryGetProperty("serviceType", out _));
        Assert.True(response.TryGetProperty("scheduledWindow", out _));
        Assert.True(response.TryGetProperty("contactInstruction", out _));
        Assert.True(response.TryGetProperty("status", out _));
        Assert.False(response.TryGetProperty("elderId", out _));
        Assert.False(response.TryGetProperty("careEventId", out _));
        Assert.False(response.TryGetProperty("healthRisks", out _));
        Assert.False(response.TryGetProperty("family", out _));
        Assert.False(response.TryGetProperty("communityNotes", out _));
        Assert.False(response.TryGetProperty("otherOrders", out _));
        Assert.False(response.TryGetProperty("rawStaffNote", out _));
    }

    private static async Task<string?> ReadProblemCodeAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.TryGetProperty("code", out var code) ? code.GetString() : null;
    }
}
