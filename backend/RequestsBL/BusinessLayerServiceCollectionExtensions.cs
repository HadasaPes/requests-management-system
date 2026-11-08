using Microsoft.Extensions.DependencyInjection;
using RequestsBL.Services;
using RequestsDAL;

namespace RequestsBL;

public static class BusinessLayerServiceCollectionExtensions
{
    public static IServiceCollection AddBusinessLayer(this IServiceCollection services, string connectionString)
    {
        services.AddDataAccessLayer(connectionString);

        services.AddMemoryCache();
        services.AddScoped<IRequestsService, RequestsService>();

        return services;
    }
}
