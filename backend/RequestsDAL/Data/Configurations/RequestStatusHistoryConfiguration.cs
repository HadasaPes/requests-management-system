using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequestsDAL.Entities;

namespace RequestsDAL.Data.Configurations;

public class RequestStatusHistoryConfiguration : IEntityTypeConfiguration<RequestStatusHistory>
{
    public void Configure(EntityTypeBuilder<RequestStatusHistory> builder)
    {
        builder.ToTable("RequestStatusHistory");

        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).UseIdentityColumn();

        builder.Property(h => h.PreviousStatus).HasColumnType("tinyint");
        builder.Property(h => h.NewStatus).HasColumnType("tinyint");

        builder.Property(h => h.ChangedAt).HasColumnType("datetime2");
        builder.Property(h => h.ChangedBy).HasMaxLength(100).IsRequired();

        builder.HasOne(h => h.Request)
            .WithMany(r => r.StatusHistory)
            .HasForeignKey(h => h.RequestId)
            .OnDelete(DeleteBehavior.Cascade);

        // History of one request in order; also serves as the index for the RequestId foreign key.
        builder.HasIndex(h => new { h.RequestId, h.ChangedAt });
    }
}
