using CommunityElderCare.Api.Identity;
using CommunityElderCare.Core.Identity;
using CommunityElderCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommunityElderCare.Api.Endpoints;

public static class AuditEndpoints
{
    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/audit", ListAsync).RequireAuthorization();
        return endpoints;
    }

    private static async Task<IResult> ListAsync(
        string? entityType,
        Guid? entityId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        HttpContext httpContext,
        CommunityCareDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var actor = httpContext.User.GetActorContext();
        if (actor.Role != DemoRole.Administrator)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Administrator scope is required",
                extensions: new Dictionary<string, object?> { ["code"] = "FORBIDDEN_SCOPE" });
        }

        var query = dbContext.AuditEntries.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(entityType))
        {
            query = query.Where(entry => entry.EntityType == entityType);
        }
        if (entityId is not null)
        {
            query = query.Where(entry => entry.EntityId == entityId);
        }
        var loaded = await query.ToListAsync(cancellationToken);
        var entries = loaded
            .Where(entry => from is null || entry.OccurredAt >= from)
            .Where(entry => to is null || entry.OccurredAt <= to)
            .OrderByDescending(entry => entry.OccurredAt)
            .Take(500)
            .ToList();
        return Results.Ok(entries.Select(entry => new
        {
            entry.Id,
            entry.ActorUserId,
            entry.ActorKind,
            entry.Action,
            entry.EntityType,
            entry.EntityId,
            entry.OccurredAt,
            entry.Reason,
            entry.BeforeStatus,
            entry.AfterStatus,
            entry.IsDemoData,
        }));
    }
}
