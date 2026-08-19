using Microsoft.EntityFrameworkCore;
using TenantRouter.Data;

namespace TenantRouter.Tenants;

public record TenantLookupRequest(string Username);
public record TenantLookupMatch(string Subdomain, string ServerUrl, string ClientOrigin);
public record TenantLookupResponse(List<TenantLookupMatch> Matches);

public static class TenantLookupEndpoints
{
    public static void MapTenantLookupEndpoints(this WebApplication app)
    {
        // Internal, service-to-service only (protected by the same shared
        // secret as reporting) — callers like patient-portal use this to
        // resolve a username to the tenant(s) it belongs to. Unlike
        // patient-portal's own public-facing /api/auth/login, this doesn't
        // need to hide whether a username exists: it's not reachable from a
        // browser, and the anti-enumeration protection belongs at the public
        // boundary, not here.
        app.MapPost("/api/tenants/lookup", async (
            TenantLookupRequest request,
            HttpRequest httpRequest,
            TenantReportSecret secret,
            TenantRouterDbContext db) =>
        {
            if (!TenantReportEndpoints.IsAuthorized(httpRequest, secret))
            {
                return Results.Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(request.Username))
            {
                return Results.BadRequest();
            }

            var hash = UsernameHasher.Hash(request.Username, secret.Value);

            // Only return a match patient-portal can actually finish acting
            // on: it proxies credentials to ServerUrl, then redirects the
            // browser to ClientOrigin. Missing either makes the match
            // unusable end-to-end.
            var matches = await db.TenantUsernames
                .Where(t => t.UsernameHash == hash)
                .Join(db.Subdomains, t => t.SubdomainId, s => s.Id, (_, s) => s)
                .Where(s => s.ServerUrl != null && s.ClientOrigin != null)
                .Select(s => new TenantLookupMatch(s.Name, s.ServerUrl!, s.ClientOrigin!))
                .ToListAsync();

            return Results.Ok(new TenantLookupResponse(matches));
        })
        .WithName("LookupTenants");
    }
}
