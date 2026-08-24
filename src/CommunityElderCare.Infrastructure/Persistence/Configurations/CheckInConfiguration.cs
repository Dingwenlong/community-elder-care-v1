using CommunityElderCare.Core.CheckIns;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityElderCare.Infrastructure.Persistence.Configurations;

internal sealed class CheckInConfiguration : IEntityTypeConfiguration<CheckIn>
{
    public void Configure(EntityTypeBuilder<CheckIn> builder)
    {
        builder.ToTable("CheckIns");
        builder.HasKey(checkIn => checkIn.Id);
        builder.Property(checkIn => checkIn.Kind).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(checkIn => checkIn.ManualReason).HasMaxLength(512);
        builder.HasIndex(checkIn => new { checkIn.ElderId, checkIn.RequestId, checkIn.Kind }).IsUnique();
        builder.HasIndex(checkIn => new { checkIn.ElderId, checkIn.ReceivedAt });
    }
}

internal sealed class ReminderConfiguration : IEntityTypeConfiguration<Reminder>
{
    public void Configure(EntityTypeBuilder<Reminder> builder)
    {
        builder.ToTable("Reminders");
        builder.HasKey(reminder => reminder.Id);
        builder.Property(reminder => reminder.Type).HasConversion<string>().HasMaxLength(64).IsRequired();
        builder.Property(reminder => reminder.DemoLabel).HasMaxLength(128).IsRequired();
        builder.HasIndex(reminder => new { reminder.ElderId, reminder.NextDueAt });
    }
}

internal sealed class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("IdempotencyRecords");
        builder.HasKey(record => record.Id);
        builder.Property(record => record.Kind).HasMaxLength(96).IsRequired();
        builder.HasIndex(record => new { record.ElderId, record.RequestId, record.Kind }).IsUnique();
    }
}
