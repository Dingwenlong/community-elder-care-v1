using CommunityElderCare.Core.Elders;
using CommunityElderCare.Core.Identity;
using CommunityElderCare.Core.CheckIns;
using CommunityElderCare.Core.CareEvents;
using CommunityElderCare.Core.CareWork;
using CommunityElderCare.Core.Ai;
using CommunityElderCare.Core.Devices;
using CommunityElderCare.Core.Common;
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

    public DbSet<CareEvent> CareEvents => Set<CareEvent>();

    public DbSet<CareEventEvidence> CareEventEvidence => Set<CareEventEvidence>();

    public DbSet<CareEventTransition> CareEventTransitions => Set<CareEventTransition>();

    public DbSet<ContactAttempt> ContactAttempts => Set<ContactAttempt>();

    public DbSet<VisitTask> VisitTasks => Set<VisitTask>();

    public DbSet<ServiceOrder> ServiceOrders => Set<ServiceOrder>();

    public DbSet<FollowUp> FollowUps => Set<FollowUp>();

    public DbSet<AiDraft> AiDrafts => Set<AiDraft>();

    public DbSet<MemoryCandidate> MemoryCandidates => Set<MemoryCandidate>();

    public DbSet<Device> Devices => Set<Device>();

    public DbSet<DeviceSignal> DeviceSignals => Set<DeviceSignal>();

    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    public DbSet<BackgroundJobRun> BackgroundJobRuns => Set<BackgroundJobRun>();

    public DbSet<NotificationAttempt> NotificationAttempts => Set<NotificationAttempt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CommunityCareDbContext).Assembly);
    }
}
