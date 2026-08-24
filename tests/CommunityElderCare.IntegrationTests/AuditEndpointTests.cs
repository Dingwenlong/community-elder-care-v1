using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CommunityElderCare.Core.Devices;
using CommunityElderCare.Core.Identity;
using CommunityElderCare.Infrastructure.Identity;

namespace CommunityElderCare.IntegrationTests;

public sealed class AuditEndpointTests
{
    [Fact]
    public async Task Event_and_visit_mutations_are_audited_without_raw_visit_note()
    {
        await using var factory = new CommunityCareWebFactory();
        using var elder = factory.CreateAuthenticatedClient(DemoRole.Elder);
        using var staff = factory.CreateAuthenticatedClient(DemoRole.CommunityStaff, areaCode: "A01");
        using var admin = factory.CreateAuthenticatedClient(DemoRole.Administrator);
        var createResponse = await elder.PostAsJsonAsync("/api/v1/care-events", new
        {
            clientRequestId = Guid.NewGuid(),
            elderId = factory.MainElderId,
            trigger = "ExplicitSos",
            summary = "演示 SOS",
            occurredAt = DateTimeOffset.UtcNow,
        });
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var eventId = created.GetProperty("id").GetGuid();
        (await admin.PostAsJsonAsync("/api/v1/demo/device-signals", new
        {
            deviceId = DemoDeviceIds.MainSosDevice,
            eventId = Guid.NewGuid(),
            deviceTime = DateTimeOffset.UtcNow.AddYears(10),
            signalType = "NoWaterActivity",
            buttonState = (string?)null,
        })).EnsureSuccessStatusCode();
        (await staff.PostAsync($"/api/v1/care-events/{eventId}/accept", null)).EnsureSuccessStatusCode();
        (await staff.PostAsJsonAsync($"/api/v1/care-events/{eventId}/simulation-attempts", new
        {
            requestId = Guid.NewGuid(),
            channel = "Phone",
            recipientRole = "Family",
            simulateFailure = false,
        })).EnsureSuccessStatusCode();

        var visitResponse = await staff.PostAsJsonAsync($"/api/v1/care-events/{eventId}/visits", new
        {
            assignedStaffUserId = DemoIdentitySeed.CommunityUserId,
            scheduledStartAt = DateTimeOffset.UtcNow.AddHours(1),
            scheduledEndAt = DateTimeOffset.UtcNow.AddHours(2),
            isMandatory = true,
        });
        visitResponse.EnsureSuccessStatusCode();
        var visit = await visitResponse.Content.ReadFromJsonAsync<JsonElement>();
        var visitId = visit.GetProperty("visitId").GetGuid();
        (await staff.PostAsync($"/api/v1/visits/{visitId}/start", null)).EnsureSuccessStatusCode();
        const string rawNote = "不得进入审计的原始探访记录 SECRET_RAW_VISIT";
        (await staff.PostAsJsonAsync($"/api/v1/visits/{visitId}/complete", new
        {
            rawStaffNote = rawNote,
            confirmedSummary = "工作人员确认的安全摘要",
            result = "探访完成",
        })).EnsureSuccessStatusCode();
        (await staff.PostAsJsonAsync($"/api/v1/care-events/{eventId}/transitions", new
        {
            toStatus = "Resolved",
            reason = "现场探访完成",
            resolution = "已确认老人安全",
        })).EnsureSuccessStatusCode();
        var followUpResponse = await staff.PostAsJsonAsync($"/api/v1/care-events/{eventId}/follow-ups", new
        {
            assignedStaffUserId = DemoIdentitySeed.CommunityUserId,
            dueAt = DateTimeOffset.UtcNow.AddDays(1),
        });
        followUpResponse.EnsureSuccessStatusCode();
        var followUp = await followUpResponse.Content.ReadFromJsonAsync<JsonElement>();
        var followUpId = followUp.GetProperty("followUpId").GetGuid();
        (await staff.PostAsJsonAsync($"/api/v1/follow-ups/{followUpId}/complete", new
        {
            result = "随访完成，状态稳定",
        })).EnsureSuccessStatusCode();
        (await staff.PostAsJsonAsync($"/api/v1/care-events/{eventId}/transitions", new
        {
            toStatus = "Closed",
            reason = "随访完成后结案",
        })).EnsureSuccessStatusCode();

        var auditResponse = await admin.GetAsync("/api/v1/audit");

        Assert.Equal(HttpStatusCode.OK, auditResponse.StatusCode);
        var serialized = await auditResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain(rawNote, serialized, StringComparison.Ordinal);
        var entries = JsonDocument.Parse(serialized).RootElement.EnumerateArray().ToList();
        Assert.Contains(entries, item => item.GetProperty("action").GetString() == "CareEventCreated");
        Assert.Contains(entries, item => item.GetProperty("action").GetString() == "EvidenceMerged");
        Assert.Contains(entries, item => item.GetProperty("action").GetString() == "EventAccepted");
        Assert.Contains(entries, item => item.GetProperty("action").GetString() == "VisitCompleted");
        Assert.Contains(entries, item => item.GetProperty("action").GetString() == "SimulationContactRecorded");
        Assert.Contains(entries, item => item.GetProperty("action").GetString() == "NotificationAttemptRecorded");
        Assert.Contains(entries, item => item.GetProperty("action").GetString() == "EventResolved");
        Assert.Contains(entries, item => item.GetProperty("action").GetString() == "FollowUpScheduled");
        Assert.Contains(entries, item => item.GetProperty("action").GetString() == "FollowUpCompleted");
        Assert.Contains(entries, item => item.GetProperty("action").GetString() == "EventClosed");
        Assert.All(entries, item =>
        {
            Assert.False(string.IsNullOrWhiteSpace(item.GetProperty("actorKind").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(item.GetProperty("action").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(item.GetProperty("entityType").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(item.GetProperty("reason").GetString()));
            Assert.NotEqual(default, item.GetProperty("occurredAt").GetDateTimeOffset());
        });
    }

    [Fact]
    public async Task Non_administrator_cannot_read_audit_entries()
    {
        await using var factory = new CommunityCareWebFactory();
        using var staff = factory.CreateAuthenticatedClient(DemoRole.CommunityStaff, areaCode: "A01");

        var response = await staff.GetAsync("/api/v1/audit");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
