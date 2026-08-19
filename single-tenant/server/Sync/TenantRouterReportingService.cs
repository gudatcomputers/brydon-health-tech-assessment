using BrydonServer.Data;
using BrydonServer.Hosting;
using Microsoft.EntityFrameworkCore;
using Polly.CircuitBreaker;

namespace BrydonServer.Sync;

public class TenantRouterReportingService(
    AppDbContext db,
    HttpClient httpClient,
    TenantRouterReportingOptions options,
    DeploymentOrigin deploymentOrigin,
    ILogger<TenantRouterReportingService> logger)
{
    private record ReportRequest(string Subdomain, string ServerUrl, string ClientOrigin, List<string> UsernameHashes);

    // Reports any users not yet marked ReportedToTenantRouter, then marks
    // them on success. Never throws — if tenant-router is unreachable or
    // rejects the report, this is retried on the next startup. The injected
    // HttpClient already retries transient failures with backoff and trips a
    // circuit breaker after repeated failures — that's not visible here, it's
    // attached via .AddPolicyHandler(...) on this client's registration in
    // Program.cs (see TenantRouterRetryPolicy); this method only handles
    // what's left once that gives up.
    public async Task SynchronizeTenantUsersAsync()
    {
        var unreportedUsers = await db.Users
            .Where(u => !u.ReportedToTenantRouter)
            .ToListAsync();

        if (unreportedUsers.Count == 0)
        {
            return;
        }

        var hashes = unreportedUsers
            .Select(u => UsernameHasher.Hash(u.Username, options.SharedSecret))
            .ToList();

        var request = new HttpRequestMessage(HttpMethod.Post, $"{options.BaseUrl}/api/tenants/report")
        {
            Content = JsonContent.Create(new ReportRequest(deploymentOrigin.Subdomain, deploymentOrigin.SelfUrl, deploymentOrigin.ClientOrigin, hashes)),
        };
        request.Headers.Add("X-Tenant-Report-Key", options.SharedSecret);

        try
        {
            var response = await httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Reporting {Count} username(s) to tenant-router failed with status {Status}; will retry on next startup.",
                    unreportedUsers.Count, response.StatusCode);
                return;
            }

            foreach (var user in unreportedUsers)
            {
                user.ReportedToTenantRouter = true;
            }

            await db.SaveChangesAsync();

            logger.LogInformation("Reported {Count} username(s) to tenant-router.", unreportedUsers.Count);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Could not reach tenant-router to report usernames; will retry on next startup.");
        }
        catch (BrokenCircuitException ex)
        {
            logger.LogWarning(
                ex,
                "Circuit breaker open for tenant-router after repeated failures; skipping this attempt, will retry on next startup.");
        }
    }
}
