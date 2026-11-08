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

        // Filter by status, sorted by creation date.
        builder.HasIndex(r => new { r.Status, r.CreatedAt });

        // Filter by priority, sorted by creation date; composite so both the COUNT and the page query can seek.
        builder.HasIndex(r => new { r.Priority, r.CreatedAt });

        // Search/filter by organization name (equality and prefix search).
        builder.HasIndex(r => r.OrganizationName);

        // Filter by assignee (e.g. "my requests").
        builder.HasIndex(r => r.AssignedTo);

        // Default list sort and date-range filters on creation date.
        builder.HasIndex(r => r.CreatedAt);
    }
}
