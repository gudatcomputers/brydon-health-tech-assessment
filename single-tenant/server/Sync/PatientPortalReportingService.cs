using System.Net.Http.Json;
using BrydonServer.Data;
using BrydonServer.Hosting;
using Microsoft.EntityFrameworkCore;

namespace BrydonServer.Sync;

public class PatientPortalReportingService(
    AppDbContext db,
    HttpClient httpClient,
    PatientPortalReportingOptions options,
    DeploymentOrigin deploymentOrigin,
    ILogger<PatientPortalReportingService> logger)
{
    private record ReportRequest(string Subdomain, List<string> UsernameHashes);

    // Reports any users not yet marked ReportedToPatientPortal, then marks
    // them on success. Never throws — if patient-portal is unreachable or
    // rejects the report, this is retried on the next startup.
    public async Task SynchronizeTenantUsersAsync()
    {
        var unreportedUsers = await db.Users
            .Where(u => !u.ReportedToPatientPortal)
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
            Content = JsonContent.Create(new ReportRequest(deploymentOrigin.Subdomain, hashes))
        };
        request.Headers.Add("X-Tenant-Report-Key", options.SharedSecret);

        try
        {
            var response = await httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Reporting {Count} username(s) to patient-portal failed with status {Status}; will retry on next startup.",
                    unreportedUsers.Count, response.StatusCode);
                return;
            }

            foreach (var user in unreportedUsers)
            {
                user.ReportedToPatientPortal = true;
            }

            await db.SaveChangesAsync();

            logger.LogInformation("Reported {Count} username(s) to patient-portal.", unreportedUsers.Count);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Could not reach patient-portal to report usernames; will retry on next startup.");
        }
    }
}
