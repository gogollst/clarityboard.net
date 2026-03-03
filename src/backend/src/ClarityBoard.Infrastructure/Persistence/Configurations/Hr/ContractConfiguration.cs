using ClarityBoard.Domain.Entities.Hr;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClarityBoard.Infrastructure.Persistence.Configurations.Hr;

public class ContractConfiguration : IEntityTypeConfiguration<Contract>
{
    public void Configure(EntityTypeBuilder<Contract> builder)
    {
        builder.ToTable("contracts", "hr");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.ContractType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(c => c.WeeklyHours).HasPrecision(5, 2).IsRequired();
        builder.Property(c => c.ChangeReason).HasMaxLength(500).IsRequired();
    }
}
