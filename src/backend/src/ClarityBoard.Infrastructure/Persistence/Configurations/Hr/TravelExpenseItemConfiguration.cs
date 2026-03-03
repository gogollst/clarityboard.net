using ClarityBoard.Domain.Entities.Hr;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClarityBoard.Infrastructure.Persistence.Configurations.Hr;

public class TravelExpenseItemConfiguration : IEntityTypeConfiguration<TravelExpenseItem>
{
    public void Configure(EntityTypeBuilder<TravelExpenseItem> builder)
    {
        builder.ToTable("travel_expense_items", "hr");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.ExpenseType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(500).IsRequired();
        builder.Property(t => t.OriginalCurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(t => t.ExchangeRate).HasPrecision(18, 6).IsRequired();
        builder.Property(t => t.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(t => t.VatRatePercent).HasPrecision(5, 2);
        // OriginalAmountCents and AmountCents are plain int — no HasPrecision
    }
}
