using CommunityElderCare.Api.Contracts.CareWork;
using CommunityElderCare.Api.Identity;
using CommunityElderCare.Core.CareWork;
using CommunityElderCare.Core.Common;

namespace CommunityElderCare.Api.Endpoints;

public static class ServiceOrderEndpoints
{
    public static IEndpointRouteBuilder MapServiceOrderEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/care-events/{eventId:guid}/service-orders", CreateAsync)
            .RequireAuthorization();
        endpoints.MapPost("/api/v1/service-orders/{orderId:guid}/accept", AcceptAsync)
            .RequireAuthorization();
        endpoints.MapPost("/api/v1/service-orders/{orderId:guid}/complete", CompleteAsync)
            .RequireAuthorization();
        return endpoints;
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
}
