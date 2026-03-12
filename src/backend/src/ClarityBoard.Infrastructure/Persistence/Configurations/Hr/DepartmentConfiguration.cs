using ClarityBoard.Domain.Entities.Hr;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClarityBoard.Infrastructure.Persistence.Configurations.Hr;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("departments", "hr");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Name).HasMaxLength(200).IsRequired();
        builder.Property(d => d.Code).HasMaxLength(50).IsRequired();
        builder.Property(d => d.Description).HasMaxLength(500);
        builder.Property(d => d.IsActive).IsRequired();
        builder.HasIndex(d => new { d.EntityId, d.Code }).IsUnique();
        builder.HasIndex(d => new { d.EntityId, d.Name }).IsUnique();

        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(d => d.ParentDepartmentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
