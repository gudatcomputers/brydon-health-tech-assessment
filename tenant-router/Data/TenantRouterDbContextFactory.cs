using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TenantRouter.Data;

// Used only by `dotnet ef` design-time tooling so migrations can be generated
// without the full app's required environment variables being set.
public class TenantRouterDbContextFactory : IDesignTimeDbContextFactory<TenantRouterDbContext>
{
    public TenantRouterDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("TENANT_ROUTER_DB_CONNECTION_STRING")
            ?? "Host=localhost;Port=5435;Database=tenant_router;Username=tenant_router;Password=tenant_router";

        var optionsBuilder = new DbContextOptionsBuilder<TenantRouterDbContext>();
        optionsBuilder.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();

        return new TenantRouterDbContext(optionsBuilder.Options);
    }
}
