namespace TenantRouter.Tenants;

// One row per distinct subdomain a single-tenant instance has reported under.
// Referenced by id from TenantUsername instead of repeating the text there.
public class Subdomain
{
    public int Id { get; set; }
    public required string Name { get; set; }

    // Where a caller (e.g. patient-portal, proxying a login) reaches this
    // tenant's server directly — not necessarily the same as the tenant's
    // public origin. Nullable: rows created before this existed won't have
    // it, and lookups treat that as "not available yet."
    public string? ServerUrl { get; set; }

    // The tenant's own browser-reachable client origin — where patient-portal
    // redirects a browser after a successful proxied login, since that client
    // (not patient-portal) owns the actual session from that point on. A
    // distinct concept from ServerUrl: that's for server-to-server calls,
    // this is for the user's browser. Nullable for the same reason as above.
    public string? ClientOrigin { get; set; }
}
