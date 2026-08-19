using Microsoft.EntityFrameworkCore;
using Npgsql;
using TenantRouter.Data;

namespace TenantRouter.Tenants;

public class SubdomainStore(TenantRouterDbContext db)
{
    // Same insert-if-not-exists shape as TenantReportEndpoints' hash inserts:
    // look up first, and if two concurrent reports for a brand-new subdomain
    // race to create it, swallow the loser's unique-violation and re-read.
    // Also keeps ServerUrl and ClientOrigin current — a tenant's reachable
    // addresses can change between deploys, and every report carries their
    // latest values.
    public async Task<int> GetOrCreateIdAsync(string name, string serverUrl, string clientOrigin)
    {
        var existing = await db.Subdomains.FirstOrDefaultAsync(s => s.Name == name);

        if (existing is not null)
        {
            if (existing.ServerUrl != serverUrl || existing.ClientOrigin != clientOrigin)
            {
                existing.ServerUrl = serverUrl;
                existing.ClientOrigin = clientOrigin;
                await db.SaveChangesAsync();
            }

            return existing.Id;
        }

        var subdomain = new Subdomain { Name = name, ServerUrl = serverUrl, ClientOrigin = clientOrigin };
        db.Subdomains.Add(subdomain);

        try
        {
            await db.SaveChangesAsync();
            return subdomain.Id;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            db.Entry(subdomain).State = EntityState.Detached;

            var winner = await db.Subdomains.FirstAsync(s => s.Name == name);
            return winner.Id;
        }
    }
}
