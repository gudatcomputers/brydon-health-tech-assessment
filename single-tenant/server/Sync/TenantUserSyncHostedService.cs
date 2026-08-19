namespace BrydonServer.Sync;

// Runs once in the background after the host starts. BackgroundService.StartAsync
// kicks ExecuteAsync off and returns immediately without waiting for it to
// finish, so this can't delay the app from accepting requests — unlike
// awaiting TenantRouterReportingService directly during startup, which would
// block on an HTTP call to tenant-router (up to the default 100s timeout if
// it's unreachable).
public class TenantUserSyncHostedService(TenantUserSyncTrigger trigger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken) => trigger.RunAsync();
}
