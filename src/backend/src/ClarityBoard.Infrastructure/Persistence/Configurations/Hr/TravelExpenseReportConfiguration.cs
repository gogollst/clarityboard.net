using ClarityBoard.Domain.Entities.Hr;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClarityBoard.Infrastructure.Persistence.Configurations.Hr;

public class TravelExpenseReportConfiguration : IEntityTypeConfiguration<TravelExpenseReport>
{
    public void Configure(EntityTypeBuilder<TravelExpenseReport> builder)
    {
        builder.ToTable("travel_expense_reports", "hr");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(t => t.Title).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Destination).HasMaxLength(200).IsRequired();
        builder.Property(t => t.BusinessPurpose).HasMaxLength(500).IsRequired();
        builder.Property(t => t.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.HasMany(t => t.Items).WithOne().HasForeignKey(i => i.ReportId);
        builder.HasIndex(t => t.EmployeeId);
    }
}
