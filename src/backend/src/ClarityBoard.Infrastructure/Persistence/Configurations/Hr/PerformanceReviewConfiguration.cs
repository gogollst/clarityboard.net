using ClarityBoard.Domain.Entities.Hr;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClarityBoard.Infrastructure.Persistence.Configurations.Hr;

public class PerformanceReviewConfiguration : IEntityTypeConfiguration<PerformanceReview>
{
    public void Configure(EntityTypeBuilder<PerformanceReview> builder)
    {
        builder.ToTable("performance_reviews", "hr");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.ReviewType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(p => p.StrengthsNotes).HasMaxLength(2000);
        builder.Property(p => p.ImprovementNotes).HasMaxLength(2000);
        builder.Property(p => p.GoalsNotes).HasMaxLength(2000);
        builder.HasMany(p => p.FeedbackEntries).WithOne().HasForeignKey(f => f.ReviewId);
    }
}
