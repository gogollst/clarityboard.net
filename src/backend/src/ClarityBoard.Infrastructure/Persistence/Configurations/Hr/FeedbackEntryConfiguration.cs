using ClarityBoard.Domain.Entities.Hr;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClarityBoard.Infrastructure.Persistence.Configurations.Hr;

public class FeedbackEntryConfiguration : IEntityTypeConfiguration<FeedbackEntry>
{
    public void Configure(EntityTypeBuilder<FeedbackEntry> builder)
    {
        builder.ToTable("feedback_entries", "hr");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.RespondentType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(f => f.Comments).HasMaxLength(2000);
        builder.Property(f => f.CompetencyScores).HasColumnType("jsonb");
    }
}
