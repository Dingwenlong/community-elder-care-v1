using CommunityElderCare.Core.Elders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityElderCare.Infrastructure.Persistence.Configurations;

public sealed class ElderProfileConfiguration : IEntityTypeConfiguration<ElderProfile>
{
    public void Configure(EntityTypeBuilder<ElderProfile> builder)
    {
        builder.ToTable("ElderProfiles", table =>
            table.HasCheckConstraint("CK_ElderProfiles_IsDemoData", "\"IsDemoData\" = 1"));
        builder.HasKey(profile => profile.Id);
        builder.Property(profile => profile.DemoDisplayName).HasMaxLength(64).IsRequired();
        builder.Property(profile => profile.AreaCode).HasMaxLength(16).IsRequired();
        builder.Property(profile => profile.AttentionLevel).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(profile => profile.NextCheckInDueAt).IsRequired();
        builder.Property(profile => profile.IsDemoData).IsRequired();
        builder.HasIndex(profile => profile.AreaCode);
        builder.HasIndex(profile => profile.AttentionLevel);

        builder.HasMany(profile => profile.HealthRisks)
            .WithOne()
            .HasForeignKey(risk => risk.ElderProfileId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(profile => profile.ServiceNeeds)
            .WithOne()
            .HasForeignKey(need => need.ElderProfileId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(profile => profile.EmergencyContacts)
            .WithOne()
            .HasForeignKey(contact => contact.ElderProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(profile => profile.HealthRisks).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(profile => profile.ServiceNeeds).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(profile => profile.EmergencyContacts).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class HealthRiskConfiguration : IEntityTypeConfiguration<HealthRisk>
{
    public void Configure(EntityTypeBuilder<HealthRisk> builder)
    {
        builder.ToTable("HealthRisks");
        builder.HasKey(risk => risk.Id);
        builder.Property(risk => risk.Code).HasMaxLength(64).IsRequired();
        builder.Property(risk => risk.DemoLabel).HasMaxLength(128).IsRequired();
        builder.HasIndex(risk => new { risk.ElderProfileId, risk.Code }).IsUnique();
    }
}

internal sealed class ServiceNeedConfiguration : IEntityTypeConfiguration<ServiceNeed>
{
    public void Configure(EntityTypeBuilder<ServiceNeed> builder)
    {
        builder.ToTable("ServiceNeeds");
        builder.HasKey(need => need.Id);
        builder.Property(need => need.Code).HasMaxLength(64).IsRequired();
        builder.Property(need => need.DemoLabel).HasMaxLength(128).IsRequired();
        builder.HasIndex(need => new { need.ElderProfileId, need.Code }).IsUnique();
    }
}

internal sealed class EmergencyContactConfiguration : IEntityTypeConfiguration<EmergencyContact>
{
    public void Configure(EntityTypeBuilder<EmergencyContact> builder)
    {
        builder.ToTable("EmergencyContacts");
        builder.HasKey(contact => contact.Id);
        builder.Property(contact => contact.DemoName).HasMaxLength(64).IsRequired();
        builder.Property(contact => contact.Relationship).HasMaxLength(32).IsRequired();
        builder.Property(contact => contact.PhoneNumber).HasMaxLength(32).IsRequired();
        builder.HasIndex(contact => new { contact.ElderProfileId, contact.ContactOrder }).IsUnique();
    }
}
