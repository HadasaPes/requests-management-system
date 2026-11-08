using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequestsDAL.Entities;

namespace RequestsDAL.Data.Configurations;

public class RequestConfiguration : IEntityTypeConfiguration<Request>
{
    public void Configure(EntityTypeBuilder<Request> builder)
    {
        builder.ToTable("Requests");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).UseIdentityColumn();

        builder.Property(r => r.Title).HasMaxLength(200).IsRequired();
        builder.Property(r => r.OrganizationName).HasMaxLength(200).IsRequired();
        builder.Property(r => r.AssignedTo).HasMaxLength(100);

        builder.Property(r => r.Status).HasColumnType("tinyint");
        builder.Property(r => r.Priority).HasColumnType("tinyint");

        builder.Property(r => r.CreatedAt).HasColumnType("datetime2");
        builder.Property(r => r.UpdatedAt).HasColumnType("datetime2");

        builder.Property(r => r.RowVersion).IsRowVersion();

        builder.HasIndex(r => new { r.Status, r.CreatedAt });

        builder.HasIndex(r => new { r.Priority, r.CreatedAt });

        builder.HasIndex(r => r.OrganizationName);

        builder.HasIndex(r => r.AssignedTo);

        builder.HasIndex(r => r.CreatedAt);
    }
}
