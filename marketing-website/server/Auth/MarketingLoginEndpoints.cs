using System.Net;
using System.Net.Http.Json;
using MarketingWebsiteServer.Tenants;

namespace MarketingWebsiteServer.Auth;

// Same login proxy as patient-portal/server's — a marketing site's "Sign in"
// link routes existing customers into their own tenant's app without them
// needing to know which subdomain they're on. Deliberately duplicated, not
// shared: see PatientLoginEndpoints in patient-portal/server for the same
// shape, and this project's UsernameHasher for the established precedent of
// independent per-service copies over a shared package.
public record LoginRequest(string Username, string Password, string? Subdomain);

// ClientOrigin is the tenant's own browser-reachable client — marketing-website
// never hosts a session itself, it proxies the credential check then hands
// the browser off to whichever tenant actually owns it.
public record LoginResponse(string Token, DateTime ExpiresAt, string ClientOrigin);

// Returned (with HTTP 300) when the username matches more than one tenant and
// the caller didn't say which one — resubmit with one of these as Subdomain.
public record MultipleTenantsResponse(List<string> Subdomains);

// The tenant's own /api/auth/login response shape.
public record TenantLoginResponse(string Token, DateTime ExpiresAt);

public static class MarketingLoginEndpoints
{
    public const string LoginProxyClientName = "tenant-login-proxy";

    public static void MapMarketingLoginEndpoints(this WebApplication app)
    {
        app.MapPost("/api/auth/login", async (
            LoginRequest request,
            TenantRouterClient tenantRouterClient,
            IHttpClientFactory httpClientFactory) =>
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest();
            }

            List<TenantMatch> matches;
            try
            {
                matches = await tenantRouterClient.LookupAsync(request.Username);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                return Results.Problem("Could not resolve your account right now.", statusCode: StatusCodes.Status502BadGateway);
            }

            if (request.Subdomain is not null)
            {
                // Caller already knows which tenant (either they picked one
                // from a prior MultipleTenantsResponse, or a single-match
                // login already told them). Verify the pair is genuinely a
                // match before proxying — never trust it blindly.
                var match = matches.FirstOrDefault(m => m.Subdomain == request.Subdomain);

                if (match is null)
                {
                    // Same response as a wrong password (see ProxyLoginAsync)
                    // — don't let the subdomain-mismatch case double as an
                    // existence check.
                    return Results.BadRequest();
                }

                return await ProxyLoginAsync(httpClientFactory, match, request.Username, request.Password);
            }

            if (matches.Count == 0)
            {
                // Same response as a wrong password (see ProxyLoginAsync) —
                // returning Unauthorized specifically here would itself be a
                // giveaway that no such user exists at all.
                return Results.BadRequest();
            }

            if (matches.Count > 1)
            {
                // Ambiguous — don't guess, and don't check the password against
                // any of them yet (that would mean picking one arbitrarily).
                // The client resubmits the same username/password plus the
                // chosen Subdomain.
                return Results.Json(
                    new MultipleTenantsResponse(matches.Select(m => m.Subdomain).ToList()),
                    statusCode: StatusCodes.Status300MultipleChoices);
            }

            return await ProxyLoginAsync(httpClientFactory, matches[0], request.Username, request.Password);
        })
        .WithName("MarketingLogin")
        .AllowAnonymous();
    }

    // Proxies the credential check to the tenant's own /api/auth/login and
    // relays its outcome. Never stores or verifies passwords itself — the
    // tenant's server is the only thing that can.
    private static async Task<IResult> ProxyLoginAsync(
        IHttpClientFactory httpClientFactory, TenantMatch match, string username, string password)
    {
        var httpClient = httpClientFactory.CreateClient(LoginProxyClientName);
        HttpResponseMessage response;

        try
        {
            response = await httpClient.PostAsJsonAsync(
                $"{match.ServerUrl}/api/auth/login",
                new { username, password });
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return Results.Problem("Could not reach that provider right now.", statusCode: StatusCodes.Status502BadGateway);
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            // Same BadRequest as "no matching user"/"wrong subdomain" above —
            // if this were Unauthorized while those were BadRequest, the
            // status code itself would tell an attacker whether the account
            // exists. All three must stay indistinguishable.
            return Results.BadRequest();
        }

        if (!response.IsSuccessStatusCode)
        {
            return Results.Problem("That provider rejected the login.", statusCode: StatusCodes.Status502BadGateway);
        }

        var tenantResponse = await response.Content.ReadFromJsonAsync<TenantLoginResponse>();
        if (tenantResponse is null)
        {
            return Results.Problem("Unexpected response from that provider.", statusCode: StatusCodes.Status502BadGateway);
        }

        return Results.Ok(new LoginResponse(tenantResponse.Token, tenantResponse.ExpiresAt, match.ClientOrigin));
    }
}
