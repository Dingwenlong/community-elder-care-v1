using CommunityElderCare.Core.Elders;
using Microsoft.EntityFrameworkCore;

namespace CommunityElderCare.Infrastructure.Persistence;

public sealed class CommunityCareDbContext(DbContextOptions<CommunityCareDbContext> options)
    : DbContext(options)
{
    public DbSet<ElderProfile> ElderProfiles => Set<ElderProfile>();

    public DbSet<HealthRisk> HealthRisks => Set<HealthRisk>();

    public DbSet<ServiceNeed> ServiceNeeds => Set<ServiceNeed>();

    public DbSet<EmergencyContact> EmergencyContacts => Set<EmergencyContact>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CommunityCareDbContext).Assembly);
    }
}
