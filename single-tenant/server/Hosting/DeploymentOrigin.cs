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
    public string SelfUrl { get; }
    public string ClientOrigin { get; }

    private DeploymentOrigin(string subdomain, string appDomain, string baseUrl, string selfUrl, string clientOrigin)
    {
        Subdomain = subdomain;
        AppDomain = appDomain;
        BaseUrl = baseUrl;
        SelfUrl = selfUrl;
        ClientOrigin = clientOrigin;
    }

    // Local dev has no real domain to derive an origin from, so SUBDOMAIN/
    // APP_DOMAIN are optional and fall back to the local dev server's origin.
    public static DeploymentOrigin FromConfiguration(IConfiguration configuration)
    {
        var subdomain = configuration["SUBDOMAIN"];
        var appDomain = configuration["APP_DOMAIN"];

        var baseUrl = subdomain is null || appDomain is null
            ? "http://localhost:5251"
            : $"https://{subdomain}.{appDomain}";

        // Usually identical to BaseUrl — in production the same reverse-proxied
        // origin serves both the public client and the API. Overridden when
        // another backend needs a different, internally-reachable address to
        // reach this one directly (e.g. patient-portal proxying a login call
        // to a Docker service hostname, which isn't the public origin).
        var selfUrl = configuration["SELF_URL"] ?? baseUrl;

        // Where a browser reaches this tenant's *client*, not its API — used
        // for CORS (this is the origin the client actually runs on) and
        // reported to tenant-router so patient-portal knows where to redirect
        // a browser after a successful proxied login. Diverges from BaseUrl
        // in local dev and the Docker simulation, where client and API run on
        // different ports with no reverse proxy combining them.
        var clientOrigin = configuration["CLIENT_ORIGIN"] ?? "http://localhost:5173";

        return new DeploymentOrigin(subdomain ?? "localhost", appDomain ?? "localhost", baseUrl, selfUrl, clientOrigin);
    }
}
