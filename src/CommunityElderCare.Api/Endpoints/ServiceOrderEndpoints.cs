using CommunityElderCare.Api.Contracts.CareWork;
using CommunityElderCare.Api.Identity;
using CommunityElderCare.Core.CareWork;
using CommunityElderCare.Core.Common;
using CommunityElderCare.Core.Identity;
using CommunityElderCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommunityElderCare.Api.Endpoints;

public static class ServiceOrderEndpoints
{
    public static IEndpointRouteBuilder MapServiceOrderEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/service-orders", ListCommunityOrdersAsync)
            .RequireAuthorization();
        endpoints.MapGet("/api/v1/service-orders/my-tasks", ListWorkerTasksAsync)
            .RequireAuthorization();
        endpoints.MapPost("/api/v1/care-events/{eventId:guid}/service-orders", CreateAsync)
            .RequireAuthorization();
        endpoints.MapPost("/api/v1/service-orders/{orderId:guid}/accept", AcceptAsync)
            .RequireAuthorization();
        endpoints.MapPost("/api/v1/service-orders/{orderId:guid}/complete", CompleteAsync)
            .RequireAuthorization();
        return endpoints;
    }

    private static async Task<IResult> ListCommunityOrdersAsync(
        Guid? careEventId,
        HttpContext httpContext,
        CommunityCareDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var actor = httpContext.User.GetActorContext();
        if (actor.Role != DemoRole.Administrator &&
            (actor.Role != DemoRole.CommunityStaff || string.IsNullOrWhiteSpace(actor.AreaCode)))
        {
            return Problem(StatusCodes.Status403Forbidden, "FORBIDDEN_SCOPE", "Role cannot list service orders");
        }

        var query =
            from order in dbContext.ServiceOrders.AsNoTracking()
            join elder in dbContext.ElderProfiles.AsNoTracking()
                on order.ElderId equals elder.Id
            where (!careEventId.HasValue || order.CareEventId == careEventId.Value) &&
                (actor.Role == DemoRole.Administrator || elder.AreaCode == actor.AreaCode)
            select new CommunityServiceOrderResponse(
                order.Id,
                order.CareEventId,
                elder.DemoDisplayName,
                order.ServiceType,
                order.ScheduledWindow,
                order.ContactInstruction,
                order.Status,
                order.Result,
                order.IsMandatory,
                order.IsDemoData);

        var orders = await query.ToListAsync(cancellationToken);
        return Results.Ok(orders.OrderByDescending(item => item.OrderId).ToList());
    }

    private static async Task<IResult> ListWorkerTasksAsync(
        HttpContext httpContext,
        CommunityCareDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var actor = httpContext.User.GetActorContext();
        if (actor.Role != DemoRole.ServiceWorker ||
            actor.ElderId is not Guid elderId ||
            actor.AssignedTaskId is not Guid assignedTaskId)
        {
            return Problem(StatusCodes.Status403Forbidden, "FORBIDDEN_SCOPE", "Task scope is required");
        }

        var tasks = await (
            from order in dbContext.ServiceOrders.AsNoTracking()
            join elder in dbContext.ElderProfiles.AsNoTracking()
                on order.ElderId equals elder.Id
            where order.Id == assignedTaskId &&
                order.ElderId == elderId &&
                order.AssignedWorkerUserId == actor.UserId
            select new ServiceWorkerOrderResponse(
                order.Id,
                elder.DemoDisplayName,
                order.ServiceType,
                order.ScheduledWindow,
                order.ContactInstruction,
                order.Status))
            .ToListAsync(cancellationToken);

        return Results.Ok(tasks);
    }

    private static async Task<IResult> CreateAsync(
        Guid eventId,
        CreateServiceOrderRequest request,
        HttpContext httpContext,
        IServiceOrderService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(
            new CreateServiceOrderCommand(
                eventId,
                request.ServiceType,
                request.ScheduledWindow,
                request.ContactInstruction,
                request.AssignedWorkerUserId,
                request.IsMandatory),
            httpContext.User.GetActorContext(),
            cancellationToken);
        return result.IsSuccess ? Results.Ok(ToResponse(result.Value!)) : ToProblem(result);
    }

    private static async Task<IResult> AcceptAsync(
        Guid orderId,
        HttpContext httpContext,
        IServiceOrderService service,
        CancellationToken cancellationToken)
    {
        var result = await service.AcceptAsync(
            orderId,
            httpContext.User.GetActorContext(),
            cancellationToken);
        return result.IsSuccess ? Results.Ok(ToResponse(result.Value!)) : ToProblem(result);
    }

    private static async Task<IResult> CompleteAsync(
        Guid orderId,
        CompleteServiceOrderRequest request,
        HttpContext httpContext,
        IServiceOrderService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CompleteAsync(
            orderId,
            request.Result,
            httpContext.User.GetActorContext(),
            cancellationToken);
        return result.IsSuccess ? Results.Ok(ToResponse(result.Value!)) : ToProblem(result);
    }

    private static ServiceWorkerOrderResponse ToResponse(ServiceWorkerOrderView view) => new(
        view.Order.Id,
        view.ElderDisplayName,
        view.Order.ServiceType,
        view.Order.ScheduledWindow,
        view.Order.ContactInstruction,
        view.Order.Status);

    private static IResult ToProblem(OperationResult<ServiceWorkerOrderView> result)
    {
        var statusCode = result.ErrorCode switch
        {
            "NOT_FOUND" => StatusCodes.Status404NotFound,
            "FORBIDDEN_SCOPE" => StatusCodes.Status403Forbidden,
            "INVALID_WORK_STATUS" or "INVALID_EVENT_STATUS" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest,
        };
        return Results.Problem(
            statusCode: statusCode,
            title: result.ErrorMessage ?? "Request failed",
            extensions: new Dictionary<string, object?>
            {
                ["code"] = result.ErrorCode ?? "UNKNOWN",
            });
    }

    private static IResult Problem(int statusCode, string code, string title) => Results.Problem(
        statusCode: statusCode,
        title: title,
        extensions: new Dictionary<string, object?> { ["code"] = code });
}
