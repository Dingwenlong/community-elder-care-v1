using CommunityElderCare.Core.Ai;
using CommunityElderCare.Core.Elders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityElderCare.Infrastructure.Persistence.Configurations;

internal sealed class AiDraftConfiguration : IEntityTypeConfiguration<AiDraft>
{
    public void Configure(EntityTypeBuilder<AiDraft> builder)
    {
        builder.ToTable("AiDrafts");
        builder.HasKey(draft => draft.Id);
        builder.Property(draft => draft.Kind).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(draft => draft.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(draft => draft.SessionIdHash).HasMaxLength(64).IsRequired();
        builder.Property(draft => draft.GeneratedText).HasMaxLength(2000).IsRequired();
        builder.HasIndex(draft => new { draft.ElderId, draft.Status, draft.CreatedAt });
        builder.HasOne<ElderProfile>()
            .WithMany()
            .HasForeignKey(draft => draft.ElderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class MemoryCandidateConfiguration : IEntityTypeConfiguration<MemoryCandidate>
{
    public void Configure(EntityTypeBuilder<MemoryCandidate> builder)
    {
        builder.ToTable("MemoryCandidates");
        builder.HasKey(memory => memory.Id);
        builder.Property(memory => memory.SessionIdHash).HasMaxLength(64).IsRequired();
        builder.Property(memory => memory.GeneratedText).HasMaxLength(1000).IsRequired();
        builder.HasIndex(memory => new { memory.ElderId, memory.ConfirmedAt });
        builder.HasOne<ElderProfile>()
            .WithMany()
            .HasForeignKey(memory => memory.ElderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
