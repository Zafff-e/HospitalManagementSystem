using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Hms.Infrastructure.Persistence;

namespace Hms.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Server=localhost,1433;Database=HmsDb;User=sa;Password=YourStrong@Passw0rd123;TrustServerCertificate=True;MultipleActiveResultSets=true";

        services.AddDbContext<HmsDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(HmsDbContext).Assembly.FullName);
                sqlOptions.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null);
            });
        });

        return services;
    }
}
