using ClarityBoard.Domain.Entities.Hr;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClarityBoard.Infrastructure.Persistence.Configurations.Hr;

public class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> builder)
    {
        builder.ToTable("leave_requests", "hr");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(l => l.WorkingDays).HasPrecision(5, 2).IsRequired();
        builder.Property(l => l.RejectionReason).HasMaxLength(500);
        builder.Property(l => l.Notes).HasMaxLength(1000);
    }
}
