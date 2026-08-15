namespace BrydonServer.Hosting;

// Each client/server pair is deployed to its own subdomain of a shared domain
// (e.g. acme.example.com), reverse-proxied so client and API share an origin.
// SUBDOMAIN/APP_DOMAIN identify that origin; used as the JWT issuer/audience,
// the CORS allow-list, and for any absolute URLs the server needs to build.
public sealed record DeploymentOrigin
{
    public string Subdomain { get; }
    public string AppDomain { get; }
    public string BaseUrl { get; }

    private DeploymentOrigin(string subdomain, string appDomain, string baseUrl)
    {
        Subdomain = subdomain;
        AppDomain = appDomain;
        BaseUrl = baseUrl;
    }

    // Local dev has no real domain to derive an origin from, so SUBDOMAIN/
    // APP_DOMAIN are optional and fall back to the local dev server's origin.
    public static DeploymentOrigin FromConfiguration(IConfiguration configuration)
    {
        var subdomain = configuration["SUBDOMAIN"];
        var appDomain = configuration["APP_DOMAIN"];

        if (subdomain is null || appDomain is null)
        {
            return new DeploymentOrigin("localhost", "localhost", "http://localhost:5251");
        }

        return new DeploymentOrigin(subdomain, appDomain, $"https://{subdomain}.{appDomain}");
    }
}
