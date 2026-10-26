using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RequestsDAL.Data;
using RequestsDAL.Repositories;

namespace RequestsDAL;

public static class DataAccessLayerServiceCollectionExtensions
{
    public static IServiceCollection AddDataAccessLayer(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        // AddDbContext registers the context with a Scoped lifetime (one per HTTP request).
        services.AddDbContext<RequestsDbContext>(options => options.UseSqlServer(connectionString));

        services.AddScoped<IRequestsRepository, RequestsRepository>();

        return services;
    }
}
