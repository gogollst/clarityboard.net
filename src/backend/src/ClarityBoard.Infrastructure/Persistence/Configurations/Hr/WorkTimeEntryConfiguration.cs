using ClarityBoard.Domain.Entities.Hr;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClarityBoard.Infrastructure.Persistence.Configurations.Hr;

public class WorkTimeEntryConfiguration : IEntityTypeConfiguration<WorkTimeEntry>
{
    public void Configure(EntityTypeBuilder<WorkTimeEntry> builder)
    {
        builder.ToTable("work_time_entries", "hr");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.EntryType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(w => w.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(w => w.ProjectCode).HasMaxLength(50);
        builder.Property(w => w.Notes).HasMaxLength(500);
        builder.HasIndex(w => new { w.EmployeeId, w.Date });
    }
}
