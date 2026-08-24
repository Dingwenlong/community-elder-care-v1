using CommunityElderCare.Core.Elders;
using CommunityElderCare.Core.Identity;
using CommunityElderCare.Core.CheckIns;
using Microsoft.EntityFrameworkCore;

namespace CommunityElderCare.Infrastructure.Persistence;

public sealed class CommunityCareDbContext(DbContextOptions<CommunityCareDbContext> options)
    : DbContext(options)
{
    public DbSet<ElderProfile> ElderProfiles => Set<ElderProfile>();

    public DbSet<HealthRisk> HealthRisks => Set<HealthRisk>();

    public DbSet<ServiceNeed> ServiceNeeds => Set<ServiceNeed>();

    public DbSet<EmergencyContact> EmergencyContacts => Set<EmergencyContact>();

    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();

    public DbSet<ConsentGrant> ConsentGrants => Set<ConsentGrant>();

    public DbSet<ConsentGrantField> ConsentGrantFields => Set<ConsentGrantField>();

    public DbSet<BreakGlassGrant> BreakGlassGrants => Set<BreakGlassGrant>();

    public DbSet<AccessAuditRecord> AccessAuditRecords => Set<AccessAuditRecord>();

    public DbSet<CheckIn> CheckIns => Set<CheckIn>();

    public DbSet<Reminder> Reminders => Set<Reminder>();

    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CommunityCareDbContext).Assembly);
    }
}
