namespace PatientPortalServer.Tenants;

// Pre-shared key every single-tenant instance must send when reporting its
// username directory, so an arbitrary caller can't poison login redirects.
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
