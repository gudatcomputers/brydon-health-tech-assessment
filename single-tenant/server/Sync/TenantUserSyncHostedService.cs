namespace BrydonServer.Sync;

// Runs once in the background after the host starts. BackgroundService.StartAsync
// kicks ExecuteAsync off and returns immediately without waiting for it to
// finish, so this can't delay the app from accepting requests — unlike
// awaiting PatientPortalReportingService directly during startup, which would
// block on an HTTP call to patient-portal (up to the default 100s timeout if
// it's unreachable).
public class TenantUserSyncHostedService(IServiceScopeFactory scopeFactory, ILogger<TenantUserSyncHostedService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
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
            // background service host.
            logger.LogError(ex, "Unexpected failure synchronizing tenant users with patient-portal.");
        }
    }
}
