using CommunityElderCare.Api.Contracts.CareWork;
using CommunityElderCare.Api.Identity;
using CommunityElderCare.Core.CareWork;
using CommunityElderCare.Core.Common;
using CommunityElderCare.Core.Identity;
using CommunityElderCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommunityElderCare.Api.Endpoints;

public static class VisitEndpoints
{
    public static IEndpointRouteBuilder MapVisitEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/visits", ListVisitsAsync)
            .RequireAuthorization();
        endpoints.MapPost("/api/v1/care-events/{eventId:guid}/visits", CreateVisitAsync)
            .RequireAuthorization();
        endpoints.MapPost("/api/v1/visits/{visitId:guid}/start", StartVisitAsync)
            .RequireAuthorization();
        endpoints.MapPost("/api/v1/visits/{visitId:guid}/complete", CompleteVisitAsync)
            .RequireAuthorization();
        endpoints.MapPost("/api/v1/care-events/{eventId:guid}/follow-ups", CreateFollowUpAsync)
            .RequireAuthorization();
        endpoints.MapGet("/api/v1/follow-ups", ListFollowUpsAsync)
            .RequireAuthorization();
        endpoints.MapPost("/api/v1/follow-ups/{followUpId:guid}/complete", CompleteFollowUpAsync)
            .RequireAuthorization();
        return endpoints;
    }

    private static async Task<IResult> ListVisitsAsync(
        Guid? careEventId,
        HttpContext httpContext,
        CommunityCareDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var actor = httpContext.User.GetActorContext();
        if (!CanListCommunityWork(actor))
        {
            return Problem(StatusCodes.Status403Forbidden, "FORBIDDEN_SCOPE", "Role cannot list visits");
        }

        var query =
            from visit in dbContext.VisitTasks.AsNoTracking()
            join elder in dbContext.ElderProfiles.AsNoTracking()
                on visit.ElderId equals elder.Id
            where (!careEventId.HasValue || visit.CareEventId == careEventId.Value) &&
                (actor.Role == DemoRole.Administrator || elder.AreaCode == actor.AreaCode)
            select new CommunityVisitResponse(
                visit.Id,
                visit.CareEventId,
                elder.DemoDisplayName,
                visit.AssignedStaffUserId,
                visit.ScheduledStartAt,
                visit.ScheduledEndAt,
                visit.StartedAt,
                visit.CompletedAt,
                visit.ConfirmedSummary,
                visit.Result,
                visit.Status,
                visit.IsMandatory,
                visit.IsDemoData);

        var visits = await query.ToListAsync(cancellationToken);
        return Results.Ok(visits.OrderBy(item => item.ScheduledStartAt).ToList());
    }

    private static async Task<IResult> ListFollowUpsAsync(
        Guid? careEventId,
        HttpContext httpContext,
        CommunityCareDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var actor = httpContext.User.GetActorContext();
        if (!CanListCommunityWork(actor))
        {
            return Problem(StatusCodes.Status403Forbidden, "FORBIDDEN_SCOPE", "Role cannot list follow-ups");
        }

        var query =
            from followUp in dbContext.FollowUps.AsNoTracking()
            join elder in dbContext.ElderProfiles.AsNoTracking()
                on followUp.ElderId equals elder.Id
            where (!careEventId.HasValue || followUp.CareEventId == careEventId.Value) &&
                (actor.Role == DemoRole.Administrator || elder.AreaCode == actor.AreaCode)
            select new CommunityFollowUpResponse(
                followUp.Id,
                followUp.CareEventId,
                elder.DemoDisplayName,
                followUp.AssignedStaffUserId,
                followUp.DueAt,
                followUp.CompletedAt,
                followUp.Result,
                followUp.Status,
                followUp.IsDemoData);

        var followUps = await query.ToListAsync(cancellationToken);
        return Results.Ok(followUps.OrderBy(item => item.DueAt).ToList());
    }

    private static async Task<IResult> CreateVisitAsync(
        Guid eventId,
        CreateVisitRequest request,
        HttpContext httpContext,
        IVisitService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(
            new CreateVisitCommand(
                eventId,
                request.AssignedStaffUserId,
                request.ScheduledStartAt,
                request.ScheduledEndAt,
                request.IsMandatory),
            httpContext.User.GetActorContext(),
            cancellationToken);
        return result.IsSuccess ? Results.Ok(ToResponse(result.Value!)) : ToProblem(result);
    }

    private static async Task<IResult> StartVisitAsync(
        Guid visitId,
        HttpContext httpContext,
        IVisitService service,
        CancellationToken cancellationToken)
    {
        var result = await service.StartAsync(
            visitId,
            httpContext.User.GetActorContext(),
            cancellationToken);
        return result.IsSuccess ? Results.Ok(ToResponse(result.Value!)) : ToProblem(result);
    }

    private static async Task<IResult> CompleteVisitAsync(
        Guid visitId,
        CompleteVisitRequest request,
        HttpContext httpContext,
        IVisitService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CompleteAsync(
            visitId,
            new CompleteVisitCommand(
                request.RawStaffNote,
                request.ConfirmedSummary,
                request.Result),
            httpContext.User.GetActorContext(),
            cancellationToken);
        return result.IsSuccess ? Results.Ok(ToResponse(result.Value!)) : ToProblem(result);
    }

    private static async Task<IResult> CreateFollowUpAsync(
        Guid eventId,
        CreateFollowUpRequest request,
        HttpContext httpContext,
        IVisitService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateFollowUpAsync(
            new CreateFollowUpCommand(eventId, request.AssignedStaffUserId, request.DueAt),
            httpContext.User.GetActorContext(),
            cancellationToken);
        return result.IsSuccess ? Results.Ok(ToResponse(result.Value!)) : ToProblem(result);
    }

    private static async Task<IResult> CompleteFollowUpAsync(
        Guid followUpId,
        CompleteFollowUpRequest request,
        HttpContext httpContext,
        IVisitService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CompleteFollowUpAsync(
            followUpId,
            request.Result,
            httpContext.User.GetActorContext(),
            cancellationToken);
        return result.IsSuccess ? Results.Ok(ToResponse(result.Value!)) : ToProblem(result);
    }

    private static VisitResponse ToResponse(VisitTask visit) => new(
        visit.Id,
        visit.CareEventId,
        visit.AssignedStaffUserId,
        visit.ScheduledStartAt,
        visit.ScheduledEndAt,
        visit.StartedAt,
        visit.CompletedAt,
        visit.ConfirmedSummary,
        visit.Result,
        visit.Status,
        visit.IsMandatory,
        visit.IsDemoData);

    private static FollowUpResponse ToResponse(FollowUp followUp) => new(
        followUp.Id,
        followUp.CareEventId,
        followUp.AssignedStaffUserId,
        followUp.DueAt,
        followUp.CompletedAt,
        followUp.Result,
        followUp.Status,
        followUp.IsDemoData);

    private static IResult ToProblem<T>(OperationResult<T> result)
    {
        var statusCode = result.ErrorCode switch
        {
            "NOT_FOUND" => StatusCodes.Status404NotFound,
            "FORBIDDEN_SCOPE" => StatusCodes.Status403Forbidden,
            "INVALID_WORK_STATUS" or "INVALID_EVENT_STATUS" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest,
        };
        return Problem(statusCode, result.ErrorCode ?? "UNKNOWN", result.ErrorMessage ?? "Request failed");
    }

    private static IResult Problem(int statusCode, string code, string title) => Results.Problem(
        statusCode: statusCode,
        title: title,
        extensions: new Dictionary<string, object?> { ["code"] = code });

    private static bool CanListCommunityWork(ActorContext actor) =>
        actor.Role == DemoRole.Administrator ||
        (actor.Role == DemoRole.CommunityStaff && !string.IsNullOrWhiteSpace(actor.AreaCode));
}
