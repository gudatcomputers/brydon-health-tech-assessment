using Microsoft.EntityFrameworkCore;
using Npgsql;
using PatientPortalServer.Data;

namespace PatientPortalServer.Tenants;

public class SubdomainStore(PatientPortalDbContext db)
{
    // Same insert-if-not-exists shape as TenantReportEndpoints' hash inserts:
    // look up first, and if two concurrent reports for a brand-new subdomain
    // race to create it, swallow the loser's unique-violation and re-read.
    public async Task<int> GetOrCreateIdAsync(string name)
    {
        var existingId = await db.Subdomains
            .Where(s => s.Name == name)
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync();

        if (existingId is not null)
        {
            return existingId.Value;
        }

        var subdomain = new Subdomain { Name = name };
        db.Subdomains.Add(subdomain);

        try
        {
            await db.SaveChangesAsync();
            return subdomain.Id;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            db.Entry(subdomain).State = EntityState.Detached;

            return await db.Subdomains
                .Where(s => s.Name == name)
                .Select(s => s.Id)
                .FirstAsync();
        }
    }
}
