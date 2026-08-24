using CommunityElderCare.Api.Contracts.CareEvents;
using CommunityElderCare.Api.Identity;
using CommunityElderCare.Core.CareEvents;
using CommunityElderCare.Core.Common;
using CommunityElderCare.Core.Identity;

namespace CommunityElderCare.Api.Endpoints;

public static class CareEventEndpoints
{
    public static IEndpointRouteBuilder MapCareEventEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/care-events").RequireAuthorization();
        group.MapPost("/", CreateAsync);
        group.MapGet("/", ListAsync);
        group.MapGet("/{eventId:guid}", GetAsync);
        group.MapPost("/{eventId:guid}/accept", AcceptAsync);
        group.MapPost("/{eventId:guid}/transitions", TransitionAsync);
        return endpoints;
    }

    private static async Task<IResult> CreateAsync(
        CreateCareEventRequest request,
        HttpContext httpContext,
        ICareEventService service,
        CancellationToken cancellationToken)
    {
        if (request.ClientRequestId == Guid.Empty ||
            request.ElderId == Guid.Empty ||
            string.IsNullOrWhiteSpace(request.Summary))
        {
            return Problem(StatusCodes.Status400BadRequest, "INVALID_EVENT", "Event fields are required");
        }

        var actor = httpContext.User.GetActorContext();
        var source = actor.Role switch
        {
            DemoRole.Elder => CareEventSource.ElderHelp,
            DemoRole.Family => CareEventSource.FamilyReport,
            DemoRole.CommunityStaff => CareEventSource.StaffVisit,
            _ => (CareEventSource?)null,
        };
        if (source is null)
        {
            return Problem(StatusCodes.Status403Forbidden, "FORBIDDEN_SCOPE", "Role cannot create events");
        }

        var trigger = actor.Role == DemoRole.Family
            ? CareEventTrigger.FamilyConcern
            : request.Trigger;
        if (trigger is null)
        {
            return Problem(StatusCodes.Status400BadRequest, "TRIGGER_REQUIRED", "A structured trigger is required");
        }

        var actorKind = actor.Role switch
        {
            DemoRole.Elder => CareEventActorKind.Elder,
            DemoRole.Family => CareEventActorKind.Family,
            _ => CareEventActorKind.Staff,
        };
        var command = new CreateCareEventCommand(
            request.ElderId,
            trigger.Value,
            source.Value,
            $"{source.Value}:{request.ClientRequestId:N}",
            request.Summary,
            request.OccurredAt,
            actorKind);
        var result = await service.CreateAsync(command, actor, cancellationToken);
        return result.IsSuccess
            ? Results.Ok(ToResponse(result.Value!.CareEvent, result.Value.IsDuplicate))
            : ToProblem(result);
    }

    private static async Task<IResult> ListAsync(
        HttpContext httpContext,
        ICareEventService service,
        IAccessPolicy accessPolicy,
        CancellationToken cancellationToken)
    {
        var actor = httpContext.User.GetActorContext();
        var events = await service.ListAsync(actor, cancellationToken);
        if (actor.Role == DemoRole.Family && actor.ElderId is Guid elderId &&
            !await accessPolicy.CanReadAsync(
                actor,
                elderId,
                ConsentField.CareEventSummary,
                cancellationToken))
        {
            return Problem(StatusCodes.Status403Forbidden, "CONSENT_REQUIRED", "Family consent is required");
        }

        return Results.Ok(events.Select(careEvent => ToResponse(careEvent, false)).ToList());
    }

    private static async Task<IResult> GetAsync(
        Guid eventId,
        HttpContext httpContext,
        ICareEventService service,
        IAccessPolicy accessPolicy,
        CancellationToken cancellationToken)
    {
        var actor = httpContext.User.GetActorContext();
        var careEvent = await service.GetAsync(eventId, actor, cancellationToken);
        if (careEvent is null)
        {
            return Problem(StatusCodes.Status404NotFound, "NOT_FOUND", "Care event not found");
        }
        if (actor.Role == DemoRole.Family &&
            !await accessPolicy.CanReadAsync(
                actor,
                careEvent.ElderId,
                ConsentField.CareEventSummary,
                cancellationToken))
        {
            return Problem(StatusCodes.Status403Forbidden, "CONSENT_REQUIRED", "Family consent is required");
        }

        return Results.Ok(ToResponse(careEvent, false));
    }

    private static async Task<IResult> AcceptAsync(
        Guid eventId,
        HttpContext httpContext,
        ICareEventService service,
        CancellationToken cancellationToken)
    {
        var result = await service.AcceptAsync(
            eventId,
            httpContext.User.GetActorContext(),
            cancellationToken);
        return result.IsSuccess
            ? Results.Ok(ToResponse(result.Value!.CareEvent, result.Value.IsDuplicate))
            : ToProblem(result);
    }

    private static async Task<IResult> TransitionAsync(
        Guid eventId,
        CareEventTransitionRequest request,
        HttpContext httpContext,
        ICareEventService service,
        CancellationToken cancellationToken)
    {
        var result = await service.TransitionAsync(
            eventId,
            request.ToStatus,
            request.Reason,
            request.Resolution,
            httpContext.User.GetActorContext(),
            cancellationToken);
        return result.IsSuccess
            ? Results.Ok(ToResponse(result.Value!.CareEvent, result.Value.IsDuplicate))
            : ToProblem(result);
    }

    private static CareEventResponse ToResponse(CareEvent careEvent, bool isDuplicate) => new(
        careEvent.Id,
        careEvent.ElderId,
        careEvent.Category,
        careEvent.Level,
        careEvent.Status,
        careEvent.Source,
        careEvent.Summary,
        careEvent.OccurredAt,
        careEvent.CreatedAt,
        careEvent.LastActivityAt,
        careEvent.ResponsibilityQueue,
        careEvent.CurrentOwnerUserId,
        careEvent.Resolution,
        careEvent.IsDemoData,
        isDuplicate,
        careEvent.Evidence
            .OrderBy(item => item.RecordedAt)
            .Select(item => new CareEventEvidenceResponse(
                item.Id,
                item.Kind,
                item.Summary,
                item.OccurredAt,
                item.RecordedAt,
                item.IsSimulation))
            .ToList(),
        careEvent.Transitions
            .OrderBy(item => item.OccurredAt)
            .Select(item => new CareEventTransitionResponse(
                item.Id,
                item.FromStatus,
                item.ToStatus,
                item.ActorKind,
                item.ActorUserId,
                item.Reason,
                item.OccurredAt,
                item.IsSimulation))
            .ToList(),
        careEvent.ContactAttempts
            .OrderBy(item => item.AttemptedAt)
            .Select(item => new ContactAttemptResponse(
                item.Id,
                item.Kind,
                item.TargetLabel,
                item.AttemptedAt,
                item.Outcome,
                item.IsSimulation))
            .ToList(),
        CareEventStateMachine.AllowedTransitions(careEvent.Status));

    private static IResult ToProblem(OperationResult<CareEventOperationResult> result)
    {
        var responseCode = result.ErrorCode is
            "RESOLUTION_REQUIRED" or
            "MANDATORY_TASK_INCOMPLETE" or
            "FOLLOW_UP_INCOMPLETE"
                ? "CLOSE_GUARD_FAILED"
                : result.ErrorCode ?? "UNKNOWN";
        var statusCode = result.ErrorCode switch
        {
            "NOT_FOUND" => StatusCodes.Status404NotFound,
            "FORBIDDEN_SCOPE" => StatusCodes.Status403Forbidden,
            "INVALID_TRANSITION" or "OWNER_REQUIRED" or "STAFF_CLOSE_REQUIRED" or
                "RESOLUTION_REQUIRED" or "MANDATORY_TASK_INCOMPLETE" or
                "FOLLOW_UP_INCOMPLETE" => StatusCodes.Status409Conflict,
            "INVALID_EVENT" or "REASON_REQUIRED" => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError,
        };
        return Problem(
            statusCode,
            responseCode,
            result.ErrorMessage ?? "Request failed");
    }

    private static IResult Problem(int statusCode, string code, string title) => Results.Problem(
        statusCode: statusCode,
        title: title,
        extensions: new Dictionary<string, object?> { ["code"] = code });
}
