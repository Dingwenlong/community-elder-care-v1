using CommunityElderCare.Api.Contracts.Devices;
using CommunityElderCare.Api.Identity;
using CommunityElderCare.Core.Common;
using CommunityElderCare.Core.Devices;
using CommunityElderCare.Core.Identity;
using CommunityElderCare.Infrastructure.Devices;

namespace CommunityElderCare.Api.Endpoints;

public static class DeviceEndpoints
{
    public static IEndpointRouteBuilder MapDeviceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/device-signals", ReceiveHardwareAsync).AllowAnonymous();
        endpoints.MapPost("/api/v1/demo/device-signals", ReceiveSimulationAsync)
            .RequireAuthorization();
        return endpoints;
    }

    private static async Task<IResult> ReceiveHardwareAsync(
        DeviceSignalRequest request,
        HttpContext httpContext,
        DeviceTokenValidator tokenValidator,
        IDeviceSignalService service,
        CancellationToken cancellationToken)
    {
        var rawToken = httpContext.Request.Headers["X-Device-Token"].ToString();
        if (!await tokenValidator.ValidateAsync(request.DeviceId, rawToken, cancellationToken))
        {
            return Problem(
                StatusCodes.Status401Unauthorized,
                "INVALID_DEVICE_TOKEN",
                "Device authentication failed");
        }

        return await ReceiveAsync(
            request,
            new DeviceSignalIdentity(request.DeviceId, DeviceSignalOrigin.Hardware, null),
            service,
            cancellationToken);
    }

    private static async Task<IResult> ReceiveSimulationAsync(
        DeviceSignalRequest request,
        HttpContext httpContext,
        IDeviceSignalService service,
        CancellationToken cancellationToken)
    {
        var actor = httpContext.User.GetActorContext();
        if (actor.Role != DemoRole.Administrator)
        {
            return Problem(
                StatusCodes.Status403Forbidden,
                "FORBIDDEN_SCOPE",
                "Administrator scope is required");
        }

        return await ReceiveAsync(
            request,
            new DeviceSignalIdentity(
                request.DeviceId,
                DeviceSignalOrigin.AdministratorSimulator,
                actor.UserId),
            service,
            cancellationToken);
    }

    private static async Task<IResult> ReceiveAsync(
        DeviceSignalRequest request,
        DeviceSignalIdentity identity,
        IDeviceSignalService service,
        CancellationToken cancellationToken)
    {
        var result = await service.ReceiveAsync(
            new DeviceSignalCommand(
                request.DeviceId,
                request.EventId,
                request.DeviceTime,
                request.SignalType,
                request.ButtonState),
            identity,
            cancellationToken);
        return result.IsSuccess
            ? Results.Ok(ToResponse(result.Value!))
            : ToProblem(result);
    }

    private static DeviceSignalResponse ToResponse(DeviceSignalReceipt receipt) => new(
        receipt.SignalId,
        receipt.CareEventId,
        receipt.ReceivedAt,
        receipt.IsDuplicate,
        receipt.IsSimulation);

    private static IResult ToProblem(OperationResult<DeviceSignalReceipt> result)
    {
        var status = result.ErrorCode switch
        {
            "UNKNOWN_DEVICE" => StatusCodes.Status404NotFound,
            "DEVICE_ID_MISMATCH" => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status400BadRequest,
        };
        return Problem(
            status,
            result.ErrorCode ?? "DEVICE_SIGNAL_FAILED",
            result.ErrorMessage ?? "Device signal failed");
    }

    private static IResult Problem(int status, string code, string title) => Results.Problem(
        statusCode: status,
        title: title,
        extensions: new Dictionary<string, object?> { ["code"] = code });
}
