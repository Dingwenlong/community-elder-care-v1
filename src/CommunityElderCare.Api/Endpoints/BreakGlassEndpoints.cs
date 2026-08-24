using CommunityElderCare.Api.Contracts.Auth;
using CommunityElderCare.Api.Identity;
using CommunityElderCare.Core.Identity;
using CommunityElderCare.Infrastructure.Persistence;

namespace CommunityElderCare.Api.Endpoints;

public static class BreakGlassEndpoints
{
    public static IEndpointRouteBuilder MapBreakGlassEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/elders/{elderId:guid}/break-glass", CreateAsync)
            .RequireAuthorization();
        return endpoints;
    }

    private static async Task<IResult> CreateAsync(
        Guid elderId,
        BreakGlassRequest request,
        HttpContext httpContext,
        CommunityCareDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var actor = httpContext.User.GetActorContext();
        if (actor.Role != DemoRole.CommunityStaff || !actor.AssignedTaskId.HasValue)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Forbidden scope",
                extensions: new Dictionary<string, object?> { ["code"] = "FORBIDDEN_SCOPE" });
        }
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Reason required",
                extensions: new Dictionary<string, object?> { ["code"] = "REASON_REQUIRED" });
        }
        if (request.DurationMinutes is < 1 or > 15)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid break-glass duration",
                extensions: new Dictionary<string, object?> { ["code"] = "INVALID_BREAK_GLASS_DURATION" });
        }

        var now = timeProvider.GetUtcNow();
        var grant = BreakGlassGrant.Create(
            Guid.NewGuid(),
            elderId,
            actor.UserId,
            actor.AssignedTaskId.Value,
            request.Reason,
            now,
            now.AddMinutes(request.DurationMinutes));
        dbContext.BreakGlassGrants.Add(grant);
        dbContext.AccessAuditRecords.Add(new AccessAuditRecord(
            Guid.NewGuid(),
            "BREAK_GLASS_GRANTED",
            actor.UserId,
            elderId,
            request.Reason,
            now,
            "SUMMARY_FIELDS"));
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(new BreakGlassResponse(
            grant.Id,
            grant.ElderId,
            grant.CareEventId,
            grant.ExpiresAt,
            grant.IsDemoData));
    }
}
