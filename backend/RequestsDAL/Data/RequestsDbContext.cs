using Microsoft.EntityFrameworkCore;
using RequestsDAL.Entities;

namespace RequestsDAL.Data;

public class RequestsDbContext : DbContext
{
    public RequestsDbContext(DbContextOptions<RequestsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Request> Requests => Set<Request>();

    public DbSet<RequestStatusHistory> RequestStatusHistory => Set<RequestStatusHistory>();

    // Applies every IEntityTypeConfiguration in Data/Configurations.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RequestsDbContext).Assembly);
    }
}
