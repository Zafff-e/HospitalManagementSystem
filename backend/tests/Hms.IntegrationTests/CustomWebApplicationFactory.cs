using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Hms.Infrastructure.Identity;
using Hms.Infrastructure.Persistence;

namespace Hms.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = "HmsTestDb_" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("UseInMemoryDatabase", "true");
        builder.UseSetting("InMemoryDbName", _dbName);

        builder.ConfigureServices(services =>
        {
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<HmsDbContext>();
            var userManager = scopedServices.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scopedServices.GetRequiredService<RoleManager<ApplicationRole>>();

            db.Database.EnsureCreated();
            DbSeeder.SeedAsync(db, userManager, roleManager).GetAwaiter().GetResult();
        });
    }
}
