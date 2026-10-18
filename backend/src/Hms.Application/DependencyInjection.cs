using Microsoft.Extensions.DependencyInjection;

namespace Hms.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Application layer services will be registered here
        return services;
    }
}
