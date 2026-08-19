using System.Net.Http.Json;

namespace PatientPortalServer.Tenants;

public record TenantMatch(string Subdomain, string ServerUrl, string ClientOrigin);

// Talks to tenant-router's /api/tenants/lookup — patient-portal no longer
// owns the (subdomain, hash) directory itself, that's tenant-router's job.
public class TenantRouterClient(HttpClient httpClient, TenantRouterOptions options)
{
    public async Task<List<TenantMatch>> LookupAsync(string username)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{options.BaseUrl}/api/tenants/lookup")
        {
            Content = JsonContent.Create(new { username })
        };
        request.Headers.Add("X-Tenant-Report-Key", options.SharedSecret);

        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<LookupResponseBody>();
        return body?.Matches ?? [];
    }

    private record LookupResponseBody(List<TenantMatch> Matches);
}
