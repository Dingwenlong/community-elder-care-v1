using CommunityElderCare.Core.CareEvents;
using CommunityElderCare.Core.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityElderCare.Infrastructure.Persistence.Configurations;

internal sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("AuditEntries");
        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.ActorKind).HasMaxLength(48).IsRequired();
        builder.Property(entry => entry.Action).HasMaxLength(64).IsRequired();
        builder.Property(entry => entry.EntityType).HasMaxLength(64).IsRequired();
        builder.Property(entry => entry.Reason).HasMaxLength(512).IsRequired();
        builder.Property(entry => entry.BeforeStatus).HasMaxLength(64);
        builder.Property(entry => entry.AfterStatus).HasMaxLength(64);
        builder.HasIndex(entry => new { entry.EntityType, entry.EntityId, entry.OccurredAt });
        builder.HasIndex(entry => entry.OccurredAt);
    }
}

internal sealed class BackgroundJobRunConfiguration : IEntityTypeConfiguration<BackgroundJobRun>
{
    public void Configure(EntityTypeBuilder<BackgroundJobRun> builder)
    {
        builder.ToTable("BackgroundJobRuns");
        builder.HasKey(run => run.Id);
        builder.Property(run => run.JobName).HasMaxLength(96).IsRequired();
        builder.Property(run => run.Result).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(run => run.ErrorCode).HasMaxLength(96);
        builder.HasIndex(run => new { run.JobName, run.StartedAt });
    }
}

internal sealed class NotificationAttemptConfiguration : IEntityTypeConfiguration<NotificationAttempt>
{
    public void Configure(EntityTypeBuilder<NotificationAttempt> builder)
    {
        builder.ToTable("NotificationAttempts");
        builder.HasKey(attempt => attempt.Id);
        builder.Property(attempt => attempt.Channel).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(attempt => attempt.RecipientRole).HasMaxLength(48).IsRequired();
        builder.Property(attempt => attempt.Outcome).HasMaxLength(64).IsRequired();
        builder.HasIndex(attempt => new { attempt.CareEventId, attempt.RequestId }).IsUnique();
        builder.HasOne<CareEvent>()
            .WithMany()
            .HasForeignKey(attempt => attempt.CareEventId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
