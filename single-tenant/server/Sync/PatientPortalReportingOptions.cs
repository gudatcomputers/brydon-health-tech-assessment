namespace BrydonServer.Sync;

public sealed record PatientPortalReportingOptions
{
    public string BaseUrl { get; }

    // Doubles as the X-Tenant-Report-Key auth header value and the HMAC key
    // for hashing usernames — every single-tenant instance and patient-portal
    // must be configured with the same value.
    public string SharedSecret { get; }

    private PatientPortalReportingOptions(string baseUrl, string sharedSecret)
    {
        BaseUrl = baseUrl;
        SharedSecret = sharedSecret;
    }

    public static PatientPortalReportingOptions FromConfiguration(IConfiguration configuration)
    {
        var baseUrl = configuration["PATIENT_PORTAL_URL"]
            ?? throw new InvalidOperationException("Missing PATIENT_PORTAL_URL environment variable.");
        var sharedSecret = configuration["TENANT_REPORT_SECRET"]
            ?? throw new InvalidOperationException("Missing TENANT_REPORT_SECRET environment variable.");

        return new PatientPortalReportingOptions(baseUrl.TrimEnd('/'), sharedSecret);
    }
}
