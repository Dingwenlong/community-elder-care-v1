using CommunityElderCare.Core.CareEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityElderCare.Infrastructure.Persistence.Configurations;

internal sealed class CareEventConfiguration : IEntityTypeConfiguration<CareEvent>
{
    public void Configure(EntityTypeBuilder<CareEvent> builder)
    {
        builder.ToTable("CareEvents");
        builder.HasKey(careEvent => careEvent.Id);
        builder.Property(careEvent => careEvent.Category)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(careEvent => careEvent.Level)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(careEvent => careEvent.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(careEvent => careEvent.Source)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(careEvent => careEvent.SourceEventId).HasMaxLength(160).IsRequired();
        builder.Property(careEvent => careEvent.Summary).HasMaxLength(512).IsRequired();
        builder.Property(careEvent => careEvent.ResponsibilityQueue).HasMaxLength(96).IsRequired();
        builder.Property(careEvent => careEvent.Resolution).HasMaxLength(1000);
        builder.HasIndex(careEvent => new { careEvent.ElderId, careEvent.SourceEventId }).IsUnique();
        builder.HasIndex(careEvent => new { careEvent.ElderId, careEvent.Status });

        builder.HasMany(careEvent => careEvent.Evidence)
            .WithOne()
            .HasForeignKey(evidence => evidence.CareEventId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(careEvent => careEvent.Evidence)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(careEvent => careEvent.Transitions)
            .WithOne()
            .HasForeignKey(transition => transition.CareEventId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(careEvent => careEvent.Transitions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(careEvent => careEvent.ContactAttempts)
            .WithOne()
            .HasForeignKey(attempt => attempt.CareEventId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(careEvent => careEvent.ContactAttempts)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class CareEventEvidenceConfiguration : IEntityTypeConfiguration<CareEventEvidence>
{
    public void Configure(EntityTypeBuilder<CareEventEvidence> builder)
    {
        builder.ToTable("CareEventEvidence");
        builder.HasKey(evidence => evidence.Id);
        builder.Property(evidence => evidence.Kind).HasMaxLength(64).IsRequired();
        builder.Property(evidence => evidence.Summary).HasMaxLength(512).IsRequired();
        builder.Property(evidence => evidence.SourceEventId).HasMaxLength(160);
        builder.HasIndex(evidence => new { evidence.CareEventId, evidence.RecordedAt });
        builder.HasIndex(evidence => new { evidence.CareEventId, evidence.SourceEventId })
            .IsUnique();
    }
}

internal sealed class CareEventTransitionConfiguration : IEntityTypeConfiguration<CareEventTransition>
{
    public void Configure(EntityTypeBuilder<CareEventTransition> builder)
    {
        builder.ToTable("CareEventTransitions");
        builder.HasKey(transition => transition.Id);
        builder.Property(transition => transition.FromStatus)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(transition => transition.ToStatus)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(transition => transition.ActorKind)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(transition => transition.Reason).HasMaxLength(1000);
        builder.HasIndex(transition => new { transition.CareEventId, transition.OccurredAt });
    }
}

internal sealed class ContactAttemptConfiguration : IEntityTypeConfiguration<ContactAttempt>
{
    public void Configure(EntityTypeBuilder<ContactAttempt> builder)
    {
        builder.ToTable("ContactAttempts");
        builder.HasKey(attempt => attempt.Id);
        builder.Property(attempt => attempt.DeterministicAttemptId).HasMaxLength(160).IsRequired();
        builder.Property(attempt => attempt.Kind)
            .HasConversion<string>()
            .HasMaxLength(48)
            .IsRequired();
        builder.Property(attempt => attempt.TargetLabel).HasMaxLength(128).IsRequired();
        builder.Property(attempt => attempt.Outcome).HasMaxLength(256).IsRequired();
        builder.HasIndex(attempt => new { attempt.CareEventId, attempt.DeterministicAttemptId })
            .IsUnique();
    }
}
