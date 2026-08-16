namespace BrydonServer.Auth;

public class User
{
    public Guid Id { get; set; }
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }

    // Bumped on logout; embedded in issued JWTs so tokens minted before the
    // bump fail validation. Avoids tracking revoked tokens individually.
    public int TokenVersion { get; set; }

    // Whether this user's hashed username has been included in a successful
    // report to patient-portal's /api/tenants/report.
    public bool ReportedToPatientPortal { get; set; }
}
