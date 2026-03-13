using ClarityBoard.Domain.Entities.Hr;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClarityBoard.Infrastructure.Persistence.Configurations.Hr;

public class EmployeeDocumentConfiguration : IEntityTypeConfiguration<EmployeeDocument>
{
    public void Configure(EntityTypeBuilder<EmployeeDocument> builder)
    {
        builder.ToTable("employee_documents", "hr");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.DocumentType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(e => e.Title).HasMaxLength(200).IsRequired();
        builder.Property(e => e.FileName).HasMaxLength(500).IsRequired();
        builder.Property(e => e.StoragePath).HasMaxLength(1000).IsRequired();
        builder.Property(e => e.MimeType).HasMaxLength(100).IsRequired();

        builder.HasIndex(e => e.ContractId).HasFilter("\"ContractId\" IS NOT NULL");
    }
}
