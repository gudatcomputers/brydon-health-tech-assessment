namespace TenantRouter.Tenants;

// Pre-shared key every caller must send: single-tenant instances reporting
// their username directory, and patient-portal (or anyone else) looking one
// up. Also used as the HMAC key for hashing usernames.
public sealed record TenantReportSecret
{
    public string Value { get; }

    private TenantReportSecret(string value)
    {
        Value = value;
    }

    public static TenantReportSecret FromConfiguration(IConfiguration configuration)
    {
        var value = configuration["TENANT_REPORT_SECRET"]
            ?? throw new InvalidOperationException("Missing TENANT_REPORT_SECRET environment variable.");

        return new TenantReportSecret(value);
    }
}
