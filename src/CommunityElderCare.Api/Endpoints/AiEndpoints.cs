using CommunityElderCare.Api.Contracts.Ai;
using CommunityElderCare.Api.Identity;
using CommunityElderCare.Core.Ai;
using CommunityElderCare.Core.Common;

namespace CommunityElderCare.Api.Endpoints;

public static class AiEndpoints
{
    public static IEndpointRouteBuilder MapAiEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/ai").RequireAuthorization();
        group.MapPost("/elder-chat", ElderChatAsync);
        group.MapPost("/service-request-drafts", DraftServiceRequestAsync);
        group.MapPost("/visit-summary-drafts", DraftVisitSummaryAsync);
        group.MapPost("/drafts/{draftId:guid}/confirm", ConfirmDraftAsync);
        group.MapPost("/memory-candidates/{candidateId:guid}/confirm", ConfirmMemoryAsync);
        group.MapGet("/memories", ListMemoriesAsync);
        group.MapDelete("/memories/{memoryId:guid}", DeleteMemoryAsync);
        return endpoints;
    }

    private static async Task<IResult> ElderChatAsync(
        ElderChatRequest request,
        HttpContext httpContext,
        IAiCareService service,
        CancellationToken cancellationToken)
    {
        var result = await service.ChatAsync(
            new AiChatCommand(request.ElderId, request.SessionId, request.Input),
            httpContext.User.GetActorContext(),
            cancellationToken);
        if (!result.IsSuccess)
        {
            return ToProblem(result);
        }
        var chat = result.Value!;
        return Results.Ok(new AiChatResponse(
            chat.Reply,
            chat.UsedFallback,
            new DangerCueResponse(
                chat.DangerCue.IsEmergency,
                chat.DangerCue.NeedsConfirmation,
                chat.DangerCue.Code),
            chat.CareEventId,
            chat.RejectionCode,
            chat.ServiceRequestDraft is null ? null : ToResponse(chat.ServiceRequestDraft),
            chat.MemoryCandidate is null ? null : ToResponse(chat.MemoryCandidate)));
    }

    private static async Task<IResult> DraftServiceRequestAsync(
        ServiceRequestDraftRequest request,
        HttpContext httpContext,
        IAiCareService service,
        CancellationToken cancellationToken)
    {
        var result = await service.DraftServiceRequestAsync(
            new DraftServiceRequestCommand(request.ElderId, request.SessionId, request.Input),
            httpContext.User.GetActorContext(),
            cancellationToken);
        return result.IsSuccess ? Results.Ok(ToResponse(result.Value!)) : ToProblem(result);
    }

    private static async Task<IResult> DraftVisitSummaryAsync(
        VisitSummaryDraftRequest request,
        HttpContext httpContext,
        IAiCareService service,
        CancellationToken cancellationToken)
    {
        var result = await service.SummarizeVisitAsync(
            new SummarizeVisitCommand(
                request.ElderId,
                request.VisitId,
                request.SessionId,
                request.RawVisitNote),
            httpContext.User.GetActorContext(),
            cancellationToken);
        return result.IsSuccess ? Results.Ok(ToResponse(result.Value!)) : ToProblem(result);
    }

    private static async Task<IResult> ConfirmDraftAsync(
        Guid draftId,
        HttpContext httpContext,
        IAiCareService service,
        CancellationToken cancellationToken)
    {
        var result = await service.ConfirmDraftAsync(
            draftId,
            httpContext.User.GetActorContext(),
            cancellationToken);
        return result.IsSuccess ? Results.Ok(ToResponse(result.Value!)) : ToProblem(result);
    }

    private static async Task<IResult> ConfirmMemoryAsync(
        Guid candidateId,
        HttpContext httpContext,
        IAiCareService service,
        CancellationToken cancellationToken)
    {
        var result = await service.ConfirmMemoryAsync(
            candidateId,
            httpContext.User.GetActorContext(),
            cancellationToken);
        return result.IsSuccess ? Results.Ok(ToResponse(result.Value!)) : ToProblem(result);
    }

    private static async Task<IResult> ListMemoriesAsync(
        HttpContext httpContext,
        IAiCareService service,
        CancellationToken cancellationToken)
    {
        var actor = httpContext.User.GetActorContext();
        if (actor.Role != Core.Identity.DemoRole.Elder)
        {
            return Problem(StatusCodes.Status403Forbidden, "FORBIDDEN_SCOPE", "Elder scope is required");
        }
        var memories = await service.ListMemoriesAsync(actor, cancellationToken);
        return Results.Ok(memories.Select(ToResponse).ToList());
    }

    private static async Task<IResult> DeleteMemoryAsync(
        Guid memoryId,
        HttpContext httpContext,
        IAiCareService service,
        CancellationToken cancellationToken)
    {
        var result = await service.DeleteMemoryAsync(
            memoryId,
            httpContext.User.GetActorContext(),
            cancellationToken);
        return result.IsSuccess ? Results.NoContent() : ToProblem(result);
    }

    private static AiDraftResponse ToResponse(AiDraft draft) => new(
        draft.Id,
        draft.Kind.ToString(),
        draft.GeneratedText,
        draft.Status.ToString(),
        draft.CreatedAt,
        draft.ConfirmedAt);

    private static AiMemoryResponse ToResponse(MemoryCandidate memory) => new(
        memory.Id,
        memory.GeneratedText,
        memory.IsConfirmed,
        memory.CreatedAt,
        memory.ConfirmedAt);

    private static IResult ToProblem<T>(OperationResult<T> result)
    {
        var status = result.ErrorCode switch
        {
            "FORBIDDEN_SCOPE" => StatusCodes.Status403Forbidden,
            "NOT_FOUND" => StatusCodes.Status404NotFound,
            "AI_UNAVAILABLE" => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status400BadRequest,
        };
        return Problem(
            status,
            result.ErrorCode ?? "AI_REQUEST_FAILED",
            result.ErrorMessage ?? "AI request failed");
    }

    private static IResult Problem(int status, string code, string title) => Results.Problem(
        statusCode: status,
        title: title,
        extensions: new Dictionary<string, object?> { ["code"] = code });
}
