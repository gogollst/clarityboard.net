using ClarityBoard.Domain.Entities.Hr;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClarityBoard.Infrastructure.Persistence.Configurations.Hr;

public class EmployeeAddressHistoryConfiguration : IEntityTypeConfiguration<EmployeeAddressHistory>
{
    public void Configure(EntityTypeBuilder<EmployeeAddressHistory> builder)
    {
        builder.ToTable("employee_address_history", "hr");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.AddressType).HasMaxLength(20).IsRequired();
        builder.Property(a => a.Street).HasMaxLength(200).IsRequired();
        builder.Property(a => a.HouseNumber).HasMaxLength(20).IsRequired();
        builder.Property(a => a.PostalCode).HasMaxLength(20).IsRequired();
        builder.Property(a => a.City).HasMaxLength(100).IsRequired();
        builder.Property(a => a.CountryCode).HasMaxLength(2).IsRequired();
        builder.Property(a => a.ChangeReason).HasMaxLength(500).IsRequired();
    }
}
