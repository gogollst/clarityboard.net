using ClarityBoard.Domain.Entities.Hr;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClarityBoard.Infrastructure.Persistence.Configurations.Hr;

public class LeaveTypeConfiguration : IEntityTypeConfiguration<LeaveType>
{
    public void Configure(EntityTypeBuilder<LeaveType> builder)
    {
        builder.ToTable("leave_types", "hr");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Name).HasMaxLength(100).IsRequired();
        builder.Property(l => l.Code).HasMaxLength(20).IsRequired();
        builder.Property(l => l.Color).HasMaxLength(7).IsRequired();
    }
}
