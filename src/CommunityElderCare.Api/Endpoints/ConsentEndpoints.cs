using CommunityElderCare.Api.Contracts.Consents;
using CommunityElderCare.Api.Identity;
using CommunityElderCare.Core.Identity;
using CommunityElderCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommunityElderCare.Api.Endpoints;

public static class ConsentEndpoints
{
    public static IEndpointRouteBuilder MapConsentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/elders/{elderId:guid}/consents").RequireAuthorization();
        group.MapGet("", GetAsync);
        group.MapPut("/{granteeUserId:guid}", PutAsync);
        group.MapDelete("/{granteeUserId:guid}", DeleteAsync);
        return endpoints;
    }

    private static async Task<IResult> GetAsync(
        Guid elderId,
        HttpContext httpContext,
        CommunityCareDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var actor = httpContext.User.GetActorContext();
        if (!IsSelfElder(actor, elderId))
        {
            return ForbiddenScope();
        }

        var grants = await dbContext.ConsentGrants
            .AsNoTracking()
            .Include(grant => grant.Fields)
            .Where(grant => grant.ElderId == elderId)
            .ToListAsync(cancellationToken);
        return Results.Ok(grants
            .OrderByDescending(grant => grant.GrantedAt)
            .Select(ToResponse)
            .ToList());
    }

    private static async Task<IResult> PutAsync(
        Guid elderId,
        Guid granteeUserId,
        UpdateConsentRequest request,
        HttpContext httpContext,
        CommunityCareDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var actor = httpContext.User.GetActorContext();
        if (!IsSelfElder(actor, elderId))
        {
            return ForbiddenScope();
        }

        var now = timeProvider.GetUtcNow();
        if (request.Fields is null || request.Fields.Count == 0 || request.ExpiresAt <= now)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid consent",
                extensions: new Dictionary<string, object?> { ["code"] = "INVALID_CONSENT" });
        }

        var granteeExists = await dbContext.UserAccounts.AnyAsync(
            account => account.Id == granteeUserId && account.Role == DemoRole.Family,
            cancellationToken);
        if (!granteeExists)
        {
            return Results.NotFound();
        }

        var previousGrants = await dbContext.ConsentGrants
            .Where(grant =>
                grant.ElderId == elderId &&
                grant.GranteeUserId == granteeUserId &&
                grant.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var previousGrant in previousGrants)
        {
            previousGrant.Revoke(now, actor.UserId);
        }

        var grant = ConsentGrant.Create(
            Guid.NewGuid(),
            elderId,
            granteeUserId,
            request.Fields,
            now,
            request.ExpiresAt,
            actor.UserId);
        dbContext.ConsentGrants.Add(grant);
        dbContext.AccessAuditRecords.Add(new AccessAuditRecord(
            Guid.NewGuid(),
            "CONSENT_GRANTED",
            actor.UserId,
            elderId,
            "老人授权",
            now,
            string.Join(',', request.Fields.Distinct().Order())));
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Ok(ToResponse(grant));
    }

    private static async Task<IResult> DeleteAsync(
        Guid elderId,
        Guid granteeUserId,
        HttpContext httpContext,
        CommunityCareDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var actor = httpContext.User.GetActorContext();
        if (!IsSelfElder(actor, elderId))
        {
            return ForbiddenScope();
        }

        var grants = await dbContext.ConsentGrants
            .Where(grant =>
                grant.ElderId == elderId &&
                grant.GranteeUserId == granteeUserId &&
                grant.RevokedAt == null)
            .ToListAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();
        foreach (var grant in grants)
        {
            grant.Revoke(now, actor.UserId);
        }

        dbContext.AccessAuditRecords.Add(new AccessAuditRecord(
            Guid.NewGuid(),
            "CONSENT_REVOKED",
            actor.UserId,
            elderId,
            "老人撤回授权",
            now,
            "ALL"));
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static bool IsSelfElder(ActorContext actor, Guid elderId) =>
        actor.Role == DemoRole.Elder && actor.ElderId == elderId;

    private static ConsentResponse ToResponse(ConsentGrant grant) => new(
        grant.Id,
        grant.ElderId,
        grant.GranteeUserId,
        grant.Fields.Select(field => field.Field).Order().ToList(),
        grant.GrantedAt,
        grant.ExpiresAt,
        grant.RevokedAt,
        grant.IsDemoData);

    private static IResult ForbiddenScope() => Results.Problem(
        statusCode: StatusCodes.Status403Forbidden,
        title: "Forbidden scope",
        extensions: new Dictionary<string, object?> { ["code"] = "FORBIDDEN_SCOPE" });
}
