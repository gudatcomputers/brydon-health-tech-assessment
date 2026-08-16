using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PatientPortalServer.Data;

// Used only by `dotnet ef` design-time tooling so migrations can be generated
// without the full app's required environment variables being set.
public class PatientPortalDbContextFactory : IDesignTimeDbContextFactory<PatientPortalDbContext>
{
    public PatientPortalDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("PATIENT_PORTAL_DB_CONNECTION_STRING")
            ?? "Host=localhost;Port=5434;Database=patient_portal;Username=patient_portal;Password=patient_portal";

        var optionsBuilder = new DbContextOptionsBuilder<PatientPortalDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new PatientPortalDbContext(optionsBuilder.Options);
    }
}
