using CommunityElderCare.Api.Contracts.Notifications;
using CommunityElderCare.Api.Identity;
using CommunityElderCare.Core.Common;
using CommunityElderCare.Infrastructure.Notifications;

namespace CommunityElderCare.Api.Endpoints;

public static class NotificationSimulationEndpoints
{
    public static IEndpointRouteBuilder MapNotificationSimulationEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(
                "/api/v1/care-events/{eventId:guid}/simulation-attempts",
                RecordAsync)
            .RequireAuthorization();
        return endpoints;
    }

    private static async Task<IResult> RecordAsync(
        Guid eventId,
        SimulationAttemptRequest request,
        HttpContext httpContext,
        SimulationNotificationService service,
        CancellationToken cancellationToken)
    {
        var result = await service.RecordAsync(
            new RecordSimulationAttemptCommand(
                eventId,
                request.RequestId,
                request.Channel,
                request.RecipientRole,
                request.SimulateFailure),
            httpContext.User.GetActorContext(),
            cancellationToken);
        if (!result.IsSuccess)
        {
            return Problem(
                result.ErrorCode == "NOT_FOUND"
                    ? StatusCodes.Status404NotFound
                    : result.ErrorCode == "FORBIDDEN_SCOPE"
                        ? StatusCodes.Status403Forbidden
                        : StatusCodes.Status400BadRequest,
                result.ErrorCode ?? "SIMULATION_ATTEMPT_FAILED",
                result.ErrorMessage ?? "Simulation attempt failed");
        }

        var receipt = result.Value!;
        var attempt = receipt.Attempt;
        return Results.Ok(new SimulationAttemptResponse(
            attempt.Id,
            attempt.CareEventId,
            attempt.RequestId,
            attempt.Channel,
            attempt.RecipientRole,
            attempt.AttemptedAt,
            attempt.Outcome,
            attempt.IsSimulation,
            receipt.IsDuplicate));
    }

    private static IResult Problem(int status, string code, string title) => Results.Problem(
        statusCode: status,
        title: title,
        extensions: new Dictionary<string, object?> { ["code"] = code });
}
