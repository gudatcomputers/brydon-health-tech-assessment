namespace BrydonServer.Sync;

public sealed record TenantRouterReportingOptions
{
    public string BaseUrl { get; }

    // Doubles as the X-Tenant-Report-Key auth header value and the HMAC key
    // for hashing usernames — every single-tenant instance and tenant-router
    // must be configured with the same value.
    public string SharedSecret { get; }

    private TenantRouterReportingOptions(string baseUrl, string sharedSecret)
    {
        BaseUrl = baseUrl;
        SharedSecret = sharedSecret;
    }

    public static TenantRouterReportingOptions FromConfiguration(IConfiguration configuration)
    {
        var baseUrl = configuration["TENANT_ROUTER_URL"]
            ?? throw new InvalidOperationException("Missing TENANT_ROUTER_URL environment variable.");
        var sharedSecret = configuration["TENANT_REPORT_SECRET"]
            ?? throw new InvalidOperationException("Missing TENANT_REPORT_SECRET environment variable.");

        return new TenantRouterReportingOptions(baseUrl.TrimEnd('/'), sharedSecret);
    }
}
