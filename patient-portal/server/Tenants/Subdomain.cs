namespace PatientPortalServer.Tenants;

// One row per distinct subdomain a single-tenant instance has reported under.
// Referenced by id from TenantUsername instead of repeating the text there.
public class Subdomain
{
    public int Id { get; set; }
    public required string Name { get; set; }
}
