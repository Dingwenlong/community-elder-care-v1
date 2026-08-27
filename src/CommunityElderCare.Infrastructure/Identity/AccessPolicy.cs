using CommunityElderCare.Core.Identity;
using CommunityElderCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommunityElderCare.Infrastructure.Identity;

public sealed class AccessPolicy(
    CommunityCareDbContext dbContext,
    TimeProvider timeProvider) : IAccessPolicy
{
    public async Task<bool> CanReadAsync(
        ActorContext actor,
        Guid elderId,
        ConsentField field,
        CancellationToken cancellationToken)
    {
        if (actor.Role == DemoRole.Elder)
        {
            return actor.ElderId == elderId;
        }

        if (actor.Role == DemoRole.Family)
        {
            if (actor.ElderId != elderId)
            {
                return false;
            }

            var now = timeProvider.GetUtcNow();
            var grants = await dbContext.ConsentGrants
                .AsNoTracking()
                .Include(grant => grant.Fields)
                .Where(grant =>
                    grant.ElderId == elderId &&
                    grant.GranteeUserId == actor.UserId &&
                    grant.RevokedAt == null)
                .ToListAsync(cancellationToken);
            return grants.Any(grant =>
                grant.IsActiveAt(now) &&
                grant.Fields.Any(grantField => grantField.Field == field));
        }

        if (actor.Role == DemoRole.ServiceWorker)
        {
            return false;
        }

        if (actor.Role == DemoRole.Administrator)
        {
            return field is ConsentField.RecentStatus
                or ConsentField.CareEventSummary
                or ConsentField.VisitSummary
                or ConsentField.ReminderCompletion;
        }

        if (actor.Role != DemoRole.CommunityStaff)
        {
            return false;
        }

        var elderArea = await dbContext.ElderProfiles
            .AsNoTracking()
            .Where(profile => profile.Id == elderId)
            .Select(profile => profile.AreaCode)
            .SingleOrDefaultAsync(cancellationToken);
        if (elderArea is not null && elderArea == actor.AreaCode)
        {
            return true;
        }

        if (!actor.AssignedTaskId.HasValue)
        {
            return false;
        }

        var currentTime = timeProvider.GetUtcNow();
        var breakGlassGrants = await dbContext.BreakGlassGrants
            .AsNoTracking()
            .Where(grant =>
                grant.ElderId == elderId &&
                grant.CommunityStaffUserId == actor.UserId &&
                grant.CareEventId == actor.AssignedTaskId.Value)
            .ToListAsync(cancellationToken);
        return breakGlassGrants.Any(grant => grant.IsActiveAt(currentTime));
    }

    public async Task<bool> CanUpdateCareProfileAsync(
        ActorContext actor,
        Guid elderId,
        CancellationToken cancellationToken)
    {
        if (actor.Role != DemoRole.CommunityStaff || string.IsNullOrWhiteSpace(actor.AreaCode))
        {
            return false;
        }

        return await dbContext.ElderProfiles
            .AsNoTracking()
            .AnyAsync(
                profile => profile.Id == elderId && profile.AreaCode == actor.AreaCode,
                cancellationToken);
    }
}
