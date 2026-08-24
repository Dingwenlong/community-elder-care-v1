using CommunityElderCare.Core.CareEvents;
using CommunityElderCare.Core.Devices;
using CommunityElderCare.Core.Elders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityElderCare.Infrastructure.Persistence.Configurations;

internal sealed class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> builder)
    {
        builder.ToTable("Devices");
        builder.HasKey(device => device.Id);
        builder.Property(device => device.DisplayName).HasMaxLength(96).IsRequired();
        builder.Property(device => device.TokenHash).HasMaxLength(64);
        builder.HasIndex(device => device.ElderId);
        builder.HasOne<ElderProfile>()
            .WithMany()
            .HasForeignKey(device => device.ElderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class DeviceSignalConfiguration : IEntityTypeConfiguration<DeviceSignal>
{
    public void Configure(EntityTypeBuilder<DeviceSignal> builder)
    {
        builder.ToTable("DeviceSignals");
        builder.HasKey(signal => signal.Id);
        builder.Property(signal => signal.SignalType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(signal => signal.ButtonState).HasMaxLength(32);
        builder.HasIndex(signal => new { signal.DeviceId, signal.EventId }).IsUnique();
        builder.HasIndex(signal => new { signal.DeviceId, signal.ReceivedAt });
        builder.HasOne<Device>()
            .WithMany()
            .HasForeignKey(signal => signal.DeviceId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<CareEvent>()
            .WithMany()
            .HasForeignKey(signal => signal.CareEventId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
