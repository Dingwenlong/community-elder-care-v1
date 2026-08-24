using CommunityElderCare.Core.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityElderCare.Infrastructure.Persistence.Configurations;

internal sealed class UserAccountConfiguration : IEntityTypeConfiguration<UserAccount>
{
    public void Configure(EntityTypeBuilder<UserAccount> builder)
    {
        builder.ToTable("UserAccounts");
        builder.HasKey(account => account.Id);
        builder.Property(account => account.Username).HasMaxLength(64).IsRequired();
        builder.Property(account => account.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Property(account => account.Role).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(account => account.AreaCode).HasMaxLength(16);
        builder.HasIndex(account => account.Username).IsUnique();
    }
}

internal sealed class ConsentGrantConfiguration : IEntityTypeConfiguration<ConsentGrant>
{
    public void Configure(EntityTypeBuilder<ConsentGrant> builder)
    {
        builder.ToTable("ConsentGrants");
        builder.HasKey(grant => grant.Id);
        builder.HasIndex(grant => new { grant.ElderId, grant.GranteeUserId, grant.ExpiresAt });
        builder.HasMany(grant => grant.Fields)
            .WithOne()
            .HasForeignKey(field => field.ConsentGrantId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(grant => grant.Fields).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class ConsentGrantFieldConfiguration : IEntityTypeConfiguration<ConsentGrantField>
{
    public void Configure(EntityTypeBuilder<ConsentGrantField> builder)
    {
        builder.ToTable("ConsentGrantFields");
        builder.HasKey(field => field.Id);
        builder.Property(field => field.Field).HasConversion<string>().HasMaxLength(64).IsRequired();
        builder.HasIndex(field => new { field.ConsentGrantId, field.Field }).IsUnique();
    }
}

internal sealed class BreakGlassGrantConfiguration : IEntityTypeConfiguration<BreakGlassGrant>
{
    public void Configure(EntityTypeBuilder<BreakGlassGrant> builder)
    {
        builder.ToTable("BreakGlassGrants");
        builder.HasKey(grant => grant.Id);
        builder.Property(grant => grant.Reason).HasMaxLength(256).IsRequired();
        builder.HasIndex(grant => new
        {
            grant.ElderId,
            grant.CommunityStaffUserId,
            grant.CareEventId,
            grant.ExpiresAt,
        });
    }
}

internal sealed class AccessAuditRecordConfiguration : IEntityTypeConfiguration<AccessAuditRecord>
{
    public void Configure(EntityTypeBuilder<AccessAuditRecord> builder)
    {
        builder.ToTable("AccessAuditRecords");
        builder.HasKey(record => record.Id);
        builder.Property(record => record.Action).HasMaxLength(64).IsRequired();
        builder.Property(record => record.Reason).HasMaxLength(512).IsRequired();
        builder.Property(record => record.FieldList).HasMaxLength(512).IsRequired();
        builder.HasIndex(record => new { record.ElderId, record.OccurredAt });
    }
}
