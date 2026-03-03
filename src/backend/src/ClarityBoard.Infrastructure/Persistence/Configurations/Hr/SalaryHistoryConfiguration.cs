using ClarityBoard.Domain.Entities.Hr;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClarityBoard.Infrastructure.Persistence.Configurations.Hr;

public class SalaryHistoryConfiguration : IEntityTypeConfiguration<SalaryHistory>
{
    public void Configure(EntityTypeBuilder<SalaryHistory> builder)
    {
        builder.ToTable("salary_history", "hr");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.SalaryType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(s => s.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(s => s.BonusCurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(s => s.ChangeReason).HasMaxLength(500).IsRequired();
        // GrossAmountCents and BonusAmountCents are plain int — no HasPrecision
    }
}
