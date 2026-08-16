namespace PatientPortalServer.Tenants;

// One row per (subdomain, hashed username) pair a single-tenant instance has
// reported. The same hash can appear under multiple subdomains (a patient may
// be a patient at more than one provider office).
public class TenantUsername
{
    public required string Subdomain { get; set; }
    public required string UsernameHash { get; set; }
}
