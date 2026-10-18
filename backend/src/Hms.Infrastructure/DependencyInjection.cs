using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Hms.Application.Common.Interfaces;
using Hms.Infrastructure.Authorization;
using Hms.Infrastructure.Identity;
using Hms.Infrastructure.Persistence;
using Hms.Infrastructure.Services;

namespace Hms.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Server=localhost,1433;Database=HmsDb;User=sa;Password=YourStrong@Passw0rd123;TrustServerCertificate=True;MultipleActiveResultSets=true";

        if (configuration.GetValue<bool>("UseInMemoryDatabase"))
        {
            var inMemoryDbName = configuration["InMemoryDbName"] ?? "HmsInMemoryDb";
            services.AddDbContext<HmsDbContext>(options =>
            {
                options.UseInMemoryDatabase(inMemoryDbName);
            });
        }
        else
        {
            services.AddDbContext<HmsDbContext>(options =>
            {
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly(typeof(HmsDbContext).Assembly.FullName);
                    sqlOptions.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null);
                });
            });
        }

        // ─── Identity Configuration ───
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            // Password policy
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 6;

            // Account Lockout: 5 failed attempts locks for 15 minutes
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<HmsDbContext>()
        .AddDefaultTokenProviders();

        // ─── Infrastructure Services ───
        services.AddHttpContextAccessor();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAuditService, AuditService>();

        // ─── Policy-based Authorization ───
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        return services;
    }
}
