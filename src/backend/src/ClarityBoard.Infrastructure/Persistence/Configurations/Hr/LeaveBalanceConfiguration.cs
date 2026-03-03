using ClarityBoard.Domain.Entities.Hr;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClarityBoard.Infrastructure.Persistence.Configurations.Hr;

public class LeaveBalanceConfiguration : IEntityTypeConfiguration<LeaveBalance>
{
    public void Configure(EntityTypeBuilder<LeaveBalance> builder)
    {
        builder.ToTable("leave_balances", "hr");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.EntitlementDays).HasPrecision(5, 2).IsRequired();
        builder.Property(l => l.UsedDays).HasPrecision(5, 2).IsRequired();
        builder.Property(l => l.PendingDays).HasPrecision(5, 2).IsRequired();
        builder.Property(l => l.CarryOverDays).HasPrecision(5, 2).IsRequired();
        builder.HasIndex(l => new { l.EmployeeId, l.LeaveTypeId, l.Year }).IsUnique();
        builder.Ignore(l => l.RemainingDays);
    }
}
