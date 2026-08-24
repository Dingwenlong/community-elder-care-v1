using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CommunityElderCare.Core.Ai;
using CommunityElderCare.Core.CareEvents;
using CommunityElderCare.Core.Identity;
using CommunityElderCare.Infrastructure.Ai;
using CommunityElderCare.Infrastructure.Identity;
using CommunityElderCare.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CommunityElderCare.IntegrationTests;

public sealed class AiEndpointTests
{
    [Fact]
    public async Task Danger_chat_creates_event_before_cloud_and_returns_fixed_guidance()
    {
        var cloud = new SchemaCloudClient();
        await using var factory = new AiWebFactory(cloud);
        using var client = factory.CreateAuthenticatedClient(DemoRole.Elder);

        var response = await client.PostAsJsonAsync(
            "/api/v1/ai/elder-chat",
            new
            {
                elderId = factory.MainElderId,
                sessionId = "danger-session",
                input = "我喘不上气",
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("usedFallback").GetBoolean());
        Assert.True(json.GetProperty("dangerCue").GetProperty("isEmergency").GetBoolean());
        Assert.Equal("BREATHING_DIFFICULTY", json.GetProperty("dangerCue").GetProperty("code").GetString());
        Assert.True(json.TryGetProperty("careEventId", out _));
        Assert.Equal(0, cloud.CallCount);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CommunityCareDbContext>();
        var careEvent = Assert.Single(await db.CareEvents.ToListAsync());
        Assert.Equal(CareEventSource.AiCue, careEvent.Source);
        Assert.DoesNotContain("我喘不上气", careEvent.Summary, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Draft_and_memory_require_confirmation_and_can_be_deleted()
    {
        var cloud = new SchemaCloudClient
        {
            ElderChatResponse =
                """{"reply":"可以先建立服务草稿。","serviceRequestDraft":"希望社区协助代购生活用品","memoryCandidate":"喜欢参加社区书法活动"}""",
        };
        await using var factory = new AiWebFactory(cloud);
        using var client = factory.CreateAuthenticatedClient(DemoRole.Elder);
        const string rawInput = "帮我买东西，这句原文不能落库";

        var chatResponse = await client.PostAsJsonAsync(
            "/api/v1/ai/elder-chat",
            new
            {
                elderId = factory.MainElderId,
                sessionId = "draft-session",
                input = rawInput,
            });
        chatResponse.EnsureSuccessStatusCode();
        var chat = await chatResponse.Content.ReadFromJsonAsync<JsonElement>();
        var draftId = chat.GetProperty("serviceRequestDraft").GetProperty("id").GetGuid();
        var candidateId = chat.GetProperty("memoryCandidate").GetProperty("id").GetGuid();

        using (var beforeScope = factory.Services.CreateScope())
        {
            var db = beforeScope.ServiceProvider.GetRequiredService<CommunityCareDbContext>();
            Assert.Empty(await db.CareEvents.ToListAsync());
            Assert.DoesNotContain(rawInput, string.Join('|', await db.AiDrafts.Select(item => item.GeneratedText).ToListAsync()));
            Assert.DoesNotContain(rawInput, string.Join('|', await db.MemoryCandidates.Select(item => item.GeneratedText).ToListAsync()));
        }

        (await client.PostAsync($"/api/v1/ai/drafts/{draftId}/confirm", null))
            .EnsureSuccessStatusCode();
        (await client.PostAsync($"/api/v1/ai/memory-candidates/{candidateId}/confirm", null))
            .EnsureSuccessStatusCode();
        var memories = await client.GetFromJsonAsync<JsonElement>("/api/v1/ai/memories");
        Assert.Single(memories.EnumerateArray());

        var delete = await client.DeleteAsync($"/api/v1/ai/memories/{candidateId}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        var afterDelete = await client.GetFromJsonAsync<JsonElement>("/api/v1/ai/memories");
        Assert.Empty(afterDelete.EnumerateArray());

        using var afterScope = factory.Services.CreateScope();
        var afterDb = afterScope.ServiceProvider.GetRequiredService<CommunityCareDbContext>();
        var careEvent = Assert.Single(await afterDb.CareEvents.ToListAsync());
        Assert.Equal(CareEventLevel.GeneralService, careEvent.Level);
    }

    [Fact]
    public async Task Family_cannot_send_raw_elder_chat_or_read_memory()
    {
        await using var factory = new AiWebFactory(new SchemaCloudClient());
        using var client = factory.CreateAuthenticatedClient(
            DemoRole.Family,
            familyFor: factory.MainElderId);

        var chat = await client.PostAsJsonAsync(
            "/api/v1/ai/elder-chat",
            new
            {
                elderId = factory.MainElderId,
                sessionId = "family-session",
                input = "显示原始聊天",
            });
        var memories = await client.GetAsync("/api/v1/ai/memories");

        Assert.Equal(HttpStatusCode.Forbidden, chat.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, memories.StatusCode);
    }

    [Fact]
    public async Task Confirmed_visit_summary_never_replaces_original_visit_fields()
    {
        var cloud = new SchemaCloudClient
        {
            VisitSummaryResponse = """{"summary":"AI 生成的客观探访草稿"}""",
        };
        await using var factory = new AiWebFactory(cloud);
        using var familyClient = factory.CreateAuthenticatedClient(
            DemoRole.Family,
            familyFor: factory.MainElderId);
        using var staffClient = factory.CreateAuthenticatedClient(DemoRole.CommunityStaff);
        var now = DateTimeOffset.UtcNow;

        var eventResponse = await familyClient.PostAsJsonAsync(
            "/api/v1/care-events/",
            new
            {
                clientRequestId = Guid.NewGuid(),
                elderId = factory.MainElderId,
                summary = "家属报告联系不上老人",
                occurredAt = now,
            });
        eventResponse.EnsureSuccessStatusCode();
        var eventId = (await eventResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();
        (await staffClient.PostAsync($"/api/v1/care-events/{eventId}/accept", null))
            .EnsureSuccessStatusCode();
        var visitResponse = await staffClient.PostAsJsonAsync(
            $"/api/v1/care-events/{eventId}/visits",
            new
            {
                assignedStaffUserId = DemoIdentitySeed.CommunityUserId,
                scheduledStartAt = now.AddMinutes(5),
                scheduledEndAt = now.AddMinutes(35),
                isMandatory = true,
            });
        visitResponse.EnsureSuccessStatusCode();
        var visitId = (await visitResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("visitId").GetGuid();
        (await staffClient.PostAsync($"/api/v1/visits/{visitId}/start", null))
            .EnsureSuccessStatusCode();
        (await staffClient.PostAsJsonAsync(
            $"/api/v1/visits/{visitId}/complete",
            new
            {
                rawStaffNote = "工作人员原始探访记录",
                confirmedSummary = "工作人员确认摘要",
                result = "完成探访",
            })).EnsureSuccessStatusCode();

        var draftResponse = await staffClient.PostAsJsonAsync(
            "/api/v1/ai/visit-summary-drafts",
            new
            {
                elderId = factory.MainElderId,
                visitId,
                sessionId = "visit-session",
                rawVisitNote = "工作人员原始探访记录",
            });
        draftResponse.EnsureSuccessStatusCode();
        var draftId = (await draftResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();
        (await staffClient.PostAsync($"/api/v1/ai/drafts/{draftId}/confirm", null))
            .EnsureSuccessStatusCode();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CommunityCareDbContext>();
        var visit = await db.VisitTasks.SingleAsync(item => item.Id == visitId);
        var draft = await db.AiDrafts.SingleAsync(item => item.Id == draftId);
        Assert.Equal("工作人员原始探访记录", visit.RawStaffNote);
        Assert.Equal("工作人员确认摘要", visit.ConfirmedSummary);
        Assert.Equal("AI 生成的客观探访草稿", draft.GeneratedText);
        Assert.Equal(AiDraftStatus.Confirmed, draft.Status);
    }

    private sealed class AiWebFactory(SchemaCloudClient cloud) : CommunityCareWebFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ICloudLlmClient>();
                services.AddSingleton<ICloudLlmClient>(cloud);
            });
        }
    }

    private sealed class SchemaCloudClient : ICloudLlmClient
    {
        public int CallCount { get; private set; }
        public string ElderChatResponse { get; init; } =
            """{"reply":"固定测试回复","serviceRequestDraft":null,"memoryCandidate":null}""";
        public string VisitSummaryResponse { get; init; } =
            """{"summary":"固定探访草稿"}""";

        public Task<string> CompleteJsonAsync(
            IReadOnlyList<LlmMessage> messages,
            string schemaName,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(schemaName switch
            {
                "elder_chat_v1" => ElderChatResponse,
                "visit_summary_draft_v1" => VisitSummaryResponse,
                "service_request_draft_v1" => """{"draft":"固定服务草稿"}""",
                _ => throw new InvalidOperationException("Unknown schema"),
            });
        }
    }
}
