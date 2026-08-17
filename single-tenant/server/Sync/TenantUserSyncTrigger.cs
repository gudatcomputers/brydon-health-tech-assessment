namespace BrydonServer.Sync;

// Runs one background sync attempt against patient-portal in its own DI
// scope, so it can outlive whatever triggered it — the host's startup
// sequence, or an HTTP request that shouldn't block on an external call.
// Never throws. Shared by TenantUserSyncHostedService (once at startup) and
// the registration endpoint (so a new user doesn't have to wait for the next
// restart to show up in patient-portal).
public class TenantUserSyncTrigger(IServiceScopeFactory scopeFactory, ILogger<TenantUserSyncTrigger> logger)
{
    public async Task RunAsync()
    {
        using var scope = scopeFactory.CreateScope();
        var reportingService = scope.ServiceProvider.GetRequiredService<PatientPortalReportingService>();

        try
        {
            await reportingService.SynchronizeTenantUsersAsync();
        }
        catch (Exception ex)
        {
            // SynchronizeTenantUsersAsync already handles its expected failure
            // modes (network errors, non-success responses); this is a
            // last-resort guard so an unanticipated exception can't crash the
            // caller (a background host, or a fire-and-forget HTTP handler).
            logger.LogError(ex, "Unexpected failure synchronizing tenant users with patient-portal.");
        }
    }
}
