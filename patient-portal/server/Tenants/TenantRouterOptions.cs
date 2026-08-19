namespace PatientPortalServer.Tenants;

public sealed record TenantRouterOptions
{
    public string BaseUrl { get; }
    public string SharedSecret { get; }

    private TenantRouterOptions(string baseUrl, string sharedSecret)
    {
        BaseUrl = baseUrl;
        SharedSecret = sharedSecret;
    }

    public static TenantRouterOptions FromConfiguration(IConfiguration configuration)
    {
        var baseUrl = configuration["TENANT_ROUTER_URL"]
            ?? throw new InvalidOperationException("Missing TENANT_ROUTER_URL environment variable.");
        var sharedSecret = configuration["TENANT_REPORT_SECRET"]
            ?? throw new InvalidOperationException("Missing TENANT_REPORT_SECRET environment variable.");

        return new TenantRouterOptions(baseUrl.TrimEnd('/'), sharedSecret);
    }
}
