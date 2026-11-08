using Microsoft.Extensions.DependencyInjection;
using RequestsBL.Services;
using RequestsDAL;

namespace RequestsBL;

public static class BusinessLayerServiceCollectionExtensions
{
    public static IServiceCollection AddBusinessLayer(this IServiceCollection services, string connectionString)
    {
        // The BL owns the DAL registration, so the API never needs to reference RequestsDAL.
        services.AddDataAccessLayer(connectionString);

        // Safe to call even if the host already added it.
        services.AddMemoryCache();
        services.AddScoped<IRequestsService, RequestsService>();

        return services;
    }
}
