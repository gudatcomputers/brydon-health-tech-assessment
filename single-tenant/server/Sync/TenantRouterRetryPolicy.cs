using Polly;
using Polly.Extensions.Http;

namespace BrydonServer.Sync;

// Applied to the HttpClient TenantRouterReportingService uses. Retry wraps
// the circuit breaker (Microsoft's documented ordering for this combination),
// so each retry attempt still respects the breaker's open state instead of
// hammering a known-down tenant-router for the full retry budget.
public static class TenantRouterRetryPolicy
{
    // 3 attempts with exponential backoff, on 5xx/408 responses or a thrown
    // HttpRequestException (e.g. connection refused).
    public static IAsyncPolicy<HttpResponseMessage> Retry() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));

    // After 5 consecutive failures, stop attempting calls for 30s and fail
    // fast instead — avoids piling up retries against a tenant-router that's
    // actually down rather than just having a transient blip.
    public static IAsyncPolicy<HttpResponseMessage> CircuitBreaker() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
}
