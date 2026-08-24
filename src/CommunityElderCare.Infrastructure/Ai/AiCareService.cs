using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CommunityElderCare.Core.Ai;
using CommunityElderCare.Core.CareEvents;
using CommunityElderCare.Core.Common;
using CommunityElderCare.Core.Identity;
using CommunityElderCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommunityElderCare.Infrastructure.Ai;

public sealed class AiCareService(
    CommunityCareDbContext dbContext,
    ICloudLlmClient cloudClient,
    ICareEventService careEventService,
    FixedContentFallback fallback,
    TimeProvider timeProvider) : IAiCareService
{
    public async Task<OperationResult<AiChatResult>> ChatAsync(
        AiChatCommand command,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        if (!IsSelfElder(actor, command.ElderId))
        {
            return Failure<AiChatResult>("FORBIDDEN_SCOPE", "Elder scope is required.");
        }
        if (string.IsNullOrWhiteSpace(command.SessionId) || string.IsNullOrWhiteSpace(command.Input))
        {
            return Failure<AiChatResult>("INVALID_AI_REQUEST", "Session and input are required.");
        }

        var danger = DangerCueScanner.Scan(command.Input);
        if (danger.IsEmergency || danger.NeedsConfirmation)
        {
            var careEvent = await CreateDangerEventAsync(
                command,
                danger,
                actor,
                cancellationToken);
            if (!careEvent.IsSuccess)
            {
                return Failure<AiChatResult>(
                    careEvent.ErrorCode ?? "EVENT_CREATE_FAILED",
                    careEvent.ErrorMessage ?? "Danger event was not created.");
            }
            return Success(new AiChatResult(
                fallback.For(danger.Code, emergency: danger.IsEmergency),
                UsedFallback: true,
                danger,
                careEvent.Value!.CareEvent.Id,
                danger.Code,
                null,
                null));
        }

        if (LooksLikePromptInjection(command.Input))
        {
            return Success(FallbackResult(danger, "PROMPT_INJECTION"));
        }

        string rawResponse;
        try
        {
            rawResponse = await cloudClient.CompleteJsonAsync(
                [
                    new LlmMessage(
                        "system",
                        "只回答陪伴、提醒和社区服务问题。不得诊断、改变药物、声称已执行外部动作或绕过人工确认。仅返回 JSON。"),
                    new LlmMessage("user", command.Input),
                ],
                "elder_chat_v1",
                cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Success(FallbackResult(danger, "CLOUD_TIMEOUT"));
        }
        catch (TaskCanceledException)
        {
            return Success(FallbackResult(danger, "CLOUD_TIMEOUT"));
        }
        catch (Exception)
        {
            return Success(FallbackResult(danger, "CLOUD_UNAVAILABLE"));
        }

        if (!TryParseChatResponse(rawResponse, out var response))
        {
            return Success(FallbackResult(danger, "MALFORMED_RESPONSE"));
        }
        var rejectionCode = ValidateGeneratedText(response.Reply);
        if (rejectionCode is not null)
        {
            return Success(FallbackResult(danger, rejectionCode));
        }

        var now = timeProvider.GetUtcNow();
        var sessionHash = StableHash(command.SessionId);
        AiDraft? draft = null;
        if (!string.IsNullOrWhiteSpace(response.ServiceRequestDraft) &&
            ValidateGeneratedText(response.ServiceRequestDraft) is null)
        {
            draft = AiDraft.Create(
                Guid.NewGuid(),
                command.ElderId,
                AiDraftKind.ServiceRequest,
                sessionHash,
                response.ServiceRequestDraft,
                null,
                now);
            dbContext.AiDrafts.Add(draft);
        }

        MemoryCandidate? memory = null;
        if (!string.IsNullOrWhiteSpace(response.MemoryCandidate) &&
            ValidateGeneratedText(response.MemoryCandidate) is null)
        {
            memory = MemoryCandidate.Create(
                Guid.NewGuid(),
                command.ElderId,
                sessionHash,
                response.MemoryCandidate,
                now);
            dbContext.MemoryCandidates.Add(memory);
        }
        if (draft is not null || memory is not null)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Success(new AiChatResult(
            response.Reply,
            UsedFallback: false,
            danger,
            null,
            null,
            draft,
            memory));
    }

    public async Task<OperationResult<AiDraft>> DraftServiceRequestAsync(
        DraftServiceRequestCommand command,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        if (!IsSelfElder(actor, command.ElderId))
        {
            return Failure<AiDraft>("FORBIDDEN_SCOPE", "Elder scope is required.");
        }
        try
        {
            var response = await cloudClient.CompleteJsonAsync(
                [
                    new LlmMessage("system", "把需求改写成简短社区服务草稿，只返回 JSON 的 draft 字段。"),
                    new LlmMessage("user", command.Input),
                ],
                "service_request_draft_v1",
                cancellationToken);
            if (!TryReadRequiredString(response, "draft", out var draftText) ||
                ValidateGeneratedText(draftText) is not null)
            {
                return Failure<AiDraft>("AI_OUTPUT_REJECTED", "Draft output was rejected.");
            }
            var draft = AiDraft.Create(
                Guid.NewGuid(),
                command.ElderId,
                AiDraftKind.ServiceRequest,
                StableHash(command.SessionId),
                draftText,
                null,
                timeProvider.GetUtcNow());
            dbContext.AiDrafts.Add(draft);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Success(draft);
        }
        catch (Exception)
        {
            return Failure<AiDraft>("AI_UNAVAILABLE", "Draft service is unavailable.");
        }
    }

    public async Task<OperationResult<AiDraft>> SummarizeVisitAsync(
        SummarizeVisitCommand command,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        if (actor.Role != DemoRole.CommunityStaff)
        {
            return Failure<AiDraft>("FORBIDDEN_SCOPE", "Community staff scope is required.");
        }
        var visitExists = await dbContext.VisitTasks.AsNoTracking().AnyAsync(
            visit => visit.Id == command.VisitId && visit.ElderId == command.ElderId,
            cancellationToken);
        if (!visitExists)
        {
            return Failure<AiDraft>("NOT_FOUND", "Visit not found.");
        }
        try
        {
            var response = await cloudClient.CompleteJsonAsync(
                [
                    new LlmMessage("system", "把探访记录改写为可由工作人员确认的客观摘要，只返回 JSON 的 summary 字段。"),
                    new LlmMessage("user", command.RawVisitNote),
                ],
                "visit_summary_draft_v1",
                cancellationToken);
            if (!TryReadRequiredString(response, "summary", out var summary) ||
                ValidateGeneratedText(summary) is not null)
            {
                return Failure<AiDraft>("AI_OUTPUT_REJECTED", "Summary output was rejected.");
            }
            var draft = AiDraft.Create(
                Guid.NewGuid(),
                command.ElderId,
                AiDraftKind.VisitSummary,
                StableHash(command.SessionId),
                summary,
                command.VisitId,
                timeProvider.GetUtcNow());
            dbContext.AiDrafts.Add(draft);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Success(draft);
        }
        catch (Exception)
        {
            return Failure<AiDraft>("AI_UNAVAILABLE", "Summary service is unavailable.");
        }
    }

    public async Task<OperationResult<AiDraft>> ConfirmDraftAsync(
        Guid draftId,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        var draft = await dbContext.AiDrafts.SingleOrDefaultAsync(
            candidate => candidate.Id == draftId,
            cancellationToken);
        if (draft is null)
        {
            return Failure<AiDraft>("NOT_FOUND", "Draft not found.");
        }
        var confirmed = draft.Confirm(actor, timeProvider.GetUtcNow());
        if (!confirmed.IsSuccess)
        {
            return confirmed;
        }
        if (draft.Kind == AiDraftKind.ServiceRequest)
        {
            var eventResult = await careEventService.CreateAsync(
                new CreateCareEventCommand(
                    draft.ElderId,
                    CareEventTrigger.LifeServiceNeed,
                    CareEventSource.ElderHelp,
                    $"AiDraft:{draft.Id:N}",
                    draft.GeneratedText,
                    draft.ConfirmedAt!.Value,
                    CareEventActorKind.Elder),
                actor,
                cancellationToken);
            if (!eventResult.IsSuccess)
            {
                return Failure<AiDraft>(
                    eventResult.ErrorCode ?? "EVENT_CREATE_FAILED",
                    eventResult.ErrorMessage ?? "Confirmed draft was not submitted.");
            }
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        return Success(draft);
    }

    public async Task<OperationResult<MemoryCandidate>> ConfirmMemoryAsync(
        Guid candidateId,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        var candidate = await dbContext.MemoryCandidates.SingleOrDefaultAsync(
            memory => memory.Id == candidateId,
            cancellationToken);
        if (candidate is null)
        {
            return Failure<MemoryCandidate>("NOT_FOUND", "Memory candidate not found.");
        }
        var result = candidate.Confirm(actor, timeProvider.GetUtcNow());
        if (result.IsSuccess)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        return result;
    }

    public async Task<OperationResult<bool>> DeleteMemoryAsync(
        Guid memoryId,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        var memory = await dbContext.MemoryCandidates.SingleOrDefaultAsync(
            candidate => candidate.Id == memoryId,
            cancellationToken);
        if (memory is null)
        {
            return Failure<bool>("NOT_FOUND", "Memory not found.");
        }
        if (!IsSelfElder(actor, memory.ElderId))
        {
            return Failure<bool>("FORBIDDEN_SCOPE", "Only the elder can delete memory.");
        }
        dbContext.MemoryCandidates.Remove(memory);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Success(true);
    }

    public async Task<IReadOnlyList<MemoryCandidate>> ListMemoriesAsync(
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        if (actor.Role != DemoRole.Elder || actor.ElderId is not Guid elderId)
        {
            return [];
        }
        return await dbContext.MemoryCandidates
            .AsNoTracking()
            .Where(memory => memory.ElderId == elderId && memory.ConfirmedAt != null)
            .ToListAsync(cancellationToken);
    }

    private async Task<OperationResult<CareEventOperationResult>> CreateDangerEventAsync(
        AiChatCommand command,
        DangerCueResult danger,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        var sourceId = $"AiCue:{StableHash($"{command.SessionId}|{command.Input}")}";
        return await careEventService.CreateAsync(
            new CreateCareEventCommand(
                command.ElderId,
                danger.IsEmergency
                    ? CareEventTrigger.DangerCue
                    : CareEventTrigger.StaffObservation,
                CareEventSource.AiCue,
                sourceId,
                $"AI 安全规则触发：{danger.Code}",
                timeProvider.GetUtcNow(),
                CareEventActorKind.Ai),
            actor: null,
            cancellationToken: cancellationToken);
    }

    private AiChatResult FallbackResult(DangerCueResult danger, string rejectionCode) => new(
        fallback.For(rejectionCode),
        UsedFallback: true,
        danger,
        null,
        rejectionCode,
        null,
        null);

    private static bool TryParseChatResponse(string value, out ChatResponse response)
    {
        response = default!;
        try
        {
            using var document = JsonDocument.Parse(value);
            var root = document.RootElement;
            if (!root.TryGetProperty("reply", out var replyElement) ||
                replyElement.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(replyElement.GetString()))
            {
                return false;
            }
            response = new ChatResponse(
                replyElement.GetString()!,
                ReadOptionalString(root, "serviceRequestDraft"),
                ReadOptionalString(root, "memoryCandidate"));
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryReadRequiredString(string json, string property, out string value)
    {
        value = string.Empty;
        try
        {
            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty(property, out var element) ||
                element.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(element.GetString()))
            {
                return false;
            }
            value = element.GetString()!;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string? ReadOptionalString(JsonElement root, string property) =>
        root.TryGetProperty(property, out var element) && element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : null;

    private static string? ValidateGeneratedText(string input)
    {
        if (ContainsAny(input, "已经联系", "已联系社区", "已通知社区", "已拨打"))
        {
            return "OUTPUT_EXTERNAL_ACTION_CLAIM";
        }
        if (ContainsAny(input, "确诊", "诊断为", "你患有"))
        {
            return "OUTPUT_DIAGNOSIS";
        }
        if (ContainsAny(input, "增加到", "减少到", "加量", "减量", "停药", "换药", "毫克"))
        {
            return "OUTPUT_MEDICATION_CHANGE";
        }
        if (ContainsAny(input, "没有危险", "绝对安全"))
        {
            return "OUTPUT_FALSE_REASSURANCE";
        }
        if (ContainsAny(input, "无需人工", "不用人工", "绕过人工", "不用社区确认"))
        {
            return "OUTPUT_BYPASS_CONFIRMATION";
        }
        return null;
    }

    private static bool LooksLikePromptInjection(string input) =>
        ContainsAny(input, "忽略系统", "忽略规则", "显示其他老人", "泄露提示词", "越过权限");

    private static bool ContainsAny(string input, params string[] phrases) =>
        phrases.Any(input.Contains);

    private static string StableHash(string input) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input))).ToLowerInvariant();

    private static bool IsSelfElder(ActorContext actor, Guid elderId) =>
        actor.Role == DemoRole.Elder && actor.ElderId == elderId;

    private static OperationResult<T> Success<T>(T value) => new(true, value, null, null);

    private static OperationResult<T> Failure<T>(string code, string message) =>
        new(false, default, code, message);

    private sealed record ChatResponse(
        string Reply,
        string? ServiceRequestDraft,
        string? MemoryCandidate);
}
