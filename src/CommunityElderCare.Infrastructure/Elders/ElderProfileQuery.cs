using CommunityElderCare.Core.Elders;
using CommunityElderCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommunityElderCare.Infrastructure.Elders;

public sealed class ElderProfileQuery(CommunityCareDbContext dbContext) : IElderProfileQuery
{
    public async Task<IReadOnlyList<ElderProfile>> ListAsync(
        CareAttentionLevel? attentionLevel,
        string? areaCode,
        CancellationToken cancellationToken)
    {
        var query = ApplyAreaFilter(dbContext.ElderProfiles.AsNoTracking(), areaCode);
        if (attentionLevel.HasValue)
        {
            query = query.Where(profile => profile.AttentionLevel == attentionLevel.Value);
        }

        return await query
            .OrderByDescending(profile => profile.AttentionLevel)
            .ThenBy(profile => profile.DemoDisplayName)
            .ToListAsync(cancellationToken);
    }

    public async Task<ElderProfile?> GetAsync(
        Guid elderId,
        string? areaCode,
        CancellationToken cancellationToken)
    {
        return await ApplyAreaFilter(dbContext.ElderProfiles.AsNoTracking(), areaCode)
            .Include(profile => profile.HealthRisks)
            .Include(profile => profile.ServiceNeeds)
            .Include(profile => profile.EmergencyContacts.OrderBy(contact => contact.ContactOrder))
            .SingleOrDefaultAsync(profile => profile.Id == elderId, cancellationToken);
    }

    private static IQueryable<ElderProfile> ApplyAreaFilter(
        IQueryable<ElderProfile> query,
        string? areaCode)
    {
        return string.IsNullOrWhiteSpace(areaCode)
            ? query
            : query.Where(profile => profile.AreaCode == areaCode);
    }
}
