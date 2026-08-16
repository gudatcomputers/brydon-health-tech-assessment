using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PatientPortalServer.Data;

namespace PatientPortalServer.Tenants;

public record TenantReportRequest(string Subdomain, List<string> UsernameHashes);

public static class TenantReportEndpoints
{
    private const string SecretHeaderName = "X-Tenant-Report-Key";

    public static void MapTenantReportEndpoints(this WebApplication app)
    {
        app.MapPost("/api/tenants/report", async (
            TenantReportRequest request,
            HttpRequest httpRequest,
            TenantReportSecret secret,
            PatientPortalDbContext db) =>
        {
            if (!IsAuthorized(httpRequest, secret))
            {
                return Results.Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(request.Subdomain) || request.UsernameHashes is null)
            {
                return Results.BadRequest();
            }

            // Additive only: insert (subdomain, hash) pairs that don't already
            // exist, nothing is ever removed. The existence check can still
            // race a concurrent report for the same subdomain, so each insert
            // is saved (and any resulting unique-violation swallowed) one at
            // a time — a shared row either one of us inserts is fine, and a
            // failed insert can't take unrelated new hashes down with it.
            var existingHashes = await db.TenantUsernames
                .Where(t => t.Subdomain == request.Subdomain)
                .Select(t => t.UsernameHash)
                .ToListAsync();

            var newHashes = request.UsernameHashes.Distinct().Except(existingHashes);

            foreach (var hash in newHashes)
            {
                var entry = db.TenantUsernames.Add(new TenantUsername { Subdomain = request.Subdomain, UsernameHash = hash });

                try
                {
                    await db.SaveChangesAsync();
                }
                catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
                {
                    entry.State = EntityState.Detached;
                }
            }

            return Results.NoContent();
        })
        .WithName("ReportTenantUsernames");
    }

    private static bool IsAuthorized(HttpRequest request, TenantReportSecret secret)
    {
        if (!request.Headers.TryGetValue(SecretHeaderName, out var provided) || provided.Count != 1)
        {
            return false;
        }

        var providedBytes = Encoding.UTF8.GetBytes(provided[0] ?? string.Empty);
        var expectedBytes = Encoding.UTF8.GetBytes(secret.Value);

        return providedBytes.Length == expectedBytes.Length &&
            CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
    }
}
