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

        services.AddScoped<IRequestsService, RequestsService>();

        return services;
    }
}
