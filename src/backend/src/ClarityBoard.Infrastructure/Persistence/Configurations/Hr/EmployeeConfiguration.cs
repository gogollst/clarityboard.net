using ClarityBoard.Domain.Entities.Hr;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClarityBoard.Infrastructure.Persistence.Configurations.Hr;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("employees", "hr");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.EntityId).IsRequired();
        builder.Property(e => e.EmployeeNumber).HasMaxLength(20).IsRequired();
        builder.Property(e => e.EmployeeType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(e => e.LastName).HasMaxLength(100).IsRequired();
        builder.Property(e => e.TaxId).HasMaxLength(50).IsRequired();
        builder.Property(e => e.SocialSecurityNumber).HasMaxLength(200);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(e => e.TerminationReason).HasMaxLength(500);
        builder.Ignore(e => e.FullName);
        builder.HasIndex(e => new { e.EntityId, e.EmployeeNumber }).IsUnique();
        builder.HasMany(e => e.SalaryHistories).WithOne().HasForeignKey(s => s.EmployeeId);
        builder.HasMany(e => e.Contracts).WithOne().HasForeignKey(c => c.EmployeeId);
        builder.HasMany(e => e.LeaveRequests).WithOne().HasForeignKey(l => l.EmployeeId);
    }
}
