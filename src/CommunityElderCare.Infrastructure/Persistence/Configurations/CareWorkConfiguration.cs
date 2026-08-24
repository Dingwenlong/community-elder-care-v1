using CommunityElderCare.Core.CareWork;
using CommunityElderCare.Core.CareEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityElderCare.Infrastructure.Persistence.Configurations;

internal sealed class VisitTaskConfiguration : IEntityTypeConfiguration<VisitTask>
{
    public void Configure(EntityTypeBuilder<VisitTask> builder)
    {
        builder.ToTable("VisitTasks");
        builder.HasKey(visit => visit.Id);
        builder.Property(visit => visit.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(visit => visit.RawStaffNote).HasMaxLength(2000);
        builder.Property(visit => visit.ConfirmedSummary).HasMaxLength(1000);
        builder.Property(visit => visit.Result).HasMaxLength(1000);
        builder.Property(visit => visit.CancellationReason).HasMaxLength(1000);
        builder.HasIndex(visit => new { visit.CareEventId, visit.Status });
        builder.HasIndex(visit => new { visit.AssignedStaffUserId, visit.ScheduledStartAt });
        builder.HasOne<CareEvent>()
            .WithMany()
            .HasForeignKey(visit => visit.CareEventId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ServiceOrderConfiguration : IEntityTypeConfiguration<ServiceOrder>
{
    public void Configure(EntityTypeBuilder<ServiceOrder> builder)
    {
        builder.ToTable("ServiceOrders");
        builder.HasKey(order => order.Id);
        builder.Property(order => order.ServiceType).HasMaxLength(96).IsRequired();
        builder.Property(order => order.ScheduledWindow).HasMaxLength(128).IsRequired();
        builder.Property(order => order.ContactInstruction).HasMaxLength(256).IsRequired();
        builder.Property(order => order.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(order => order.Result).HasMaxLength(1000);
        builder.Property(order => order.CancellationReason).HasMaxLength(1000);
        builder.HasIndex(order => new { order.CareEventId, order.Status });
        builder.HasIndex(order => new { order.AssignedWorkerUserId, order.Status });
        builder.HasOne<CareEvent>()
            .WithMany()
            .HasForeignKey(order => order.CareEventId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class FollowUpConfiguration : IEntityTypeConfiguration<FollowUp>
{
    public void Configure(EntityTypeBuilder<FollowUp> builder)
    {
        builder.ToTable("FollowUps");
        builder.HasKey(followUp => followUp.Id);
        builder.Property(followUp => followUp.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(followUp => followUp.Result).HasMaxLength(1000);
        builder.HasIndex(followUp => new { followUp.CareEventId, followUp.Status });
        builder.HasIndex(followUp => new { followUp.AssignedStaffUserId, followUp.DueAt });
        builder.HasOne<CareEvent>()
            .WithMany()
            .HasForeignKey(followUp => followUp.CareEventId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
