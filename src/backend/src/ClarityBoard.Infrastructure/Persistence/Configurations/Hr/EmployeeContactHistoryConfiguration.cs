using ClarityBoard.Domain.Entities.Hr;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClarityBoard.Infrastructure.Persistence.Configurations.Hr;

public class EmployeeContactHistoryConfiguration : IEntityTypeConfiguration<EmployeeContactHistory>
{
    public void Configure(EntityTypeBuilder<EmployeeContactHistory> builder)
    {
        builder.ToTable("employee_contact_history", "hr");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Email).HasMaxLength(320).IsRequired();
        builder.Property(c => c.PhoneWork).HasMaxLength(50);
        builder.Property(c => c.PhoneMobile).HasMaxLength(50);
        builder.Property(c => c.EmergencyContactName).HasMaxLength(200);
        builder.Property(c => c.EmergencyContactPhone).HasMaxLength(50);
        builder.Property(c => c.ChangeReason).HasMaxLength(500).IsRequired();
    }
}
