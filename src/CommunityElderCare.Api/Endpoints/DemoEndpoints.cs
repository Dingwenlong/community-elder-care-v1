using CommunityElderCare.Api.Identity;
using CommunityElderCare.Core.Identity;
using CommunityElderCare.Infrastructure.Demo;

namespace CommunityElderCare.Api.Endpoints;

public static class DemoEndpoints
{
    public static IEndpointRouteBuilder MapDemoEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/demo/reset", ResetAsync).RequireAuthorization();
        endpoints.MapPost("/api/v1/demo/operations-scenario", async (
            HttpContext context, OperationsScenarioService service, CancellationToken ct) =>
        {
            var actor = context.User.GetActorContext();
            if (actor.Role != DemoRole.Administrator) return OperationsEndpoints.Forbidden();
            if (context.Request.Headers["X-Confirm-Operations-Scenario"] != "LOAD-OPERATIONS")
                return OperationsEndpoints.Problem(400, "SCENARIO_CONFIRMATION_REQUIRED", "请确认加载运营演示场景。");
            return Results.Ok(await service.LoadAsync(actor, ct));
        }).RequireAuthorization();
        return endpoints;
    }

    private static async Task<IResult> ResetAsync(
        HttpContext httpContext,
        DemoResetService service,
        CancellationToken cancellationToken)
    {
        var actor = httpContext.User.GetActorContext();
        if (actor.Role != DemoRole.Administrator)
        {
            return Problem(StatusCodes.Status403Forbidden, "FORBIDDEN_SCOPE", "Administrator scope is required");
        }
        if (httpContext.Request.Headers["X-Confirm-Demo-Reset"].ToString() != "RESET-20")
        {
            return Problem(
                StatusCodes.Status400BadRequest,
                "RESET_CONFIRMATION_REQUIRED",
                "Exact demo reset confirmation is required");
        }

        var result = await service.ResetAsync(actor, cancellationToken);
        return Results.Ok(result);
    }

    private static IResult Problem(int status, string code, string title) => Results.Problem(
        statusCode: status,
        title: title,
        extensions: new Dictionary<string, object?> { ["code"] = code });
}
