using ClarityBoard.Domain.Entities.Hr;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClarityBoard.Infrastructure.Persistence.Configurations.Hr;

public class DataAccessLogConfiguration : IEntityTypeConfiguration<DataAccessLog>
{
    public void Configure(EntityTypeBuilder<DataAccessLog> builder)
    {
        builder.ToTable("data_access_logs", "hr");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.AccessType).HasMaxLength(50).IsRequired();
        builder.Property(d => d.ResourceType).HasMaxLength(50).IsRequired();
        builder.Property(d => d.IpAddress).HasMaxLength(45);
        builder.Property(d => d.UserAgent).HasMaxLength(500);
    }
}
