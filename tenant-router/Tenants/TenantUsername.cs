namespace TenantRouter.Tenants;

// One row per (subdomain, hashed username) pair a single-tenant instance has
// reported. The same hash can appear under multiple subdomains (a patient may
// be a patient at more than one provider office).
public class TenantUsername
{
    public required int SubdomainId { get; set; }
    public required string UsernameHash { get; set; }

    // Set on every insert and update by TenantRouterDbContext.SaveChangesAsync
    // — don't set this manually.
    public DateTime UpdatedOn { get; set; }

    public Subdomain? Subdomain { get; set; }
}
