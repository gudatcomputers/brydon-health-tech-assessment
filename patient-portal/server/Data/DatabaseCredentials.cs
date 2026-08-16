namespace PatientPortalServer.Data;

public sealed record DatabaseCredentials
{
    public string Host { get; }
    public string Port { get; }
    public string Name { get; }
    public string User { get; }
    public string Password { get; }

    private DatabaseCredentials(string host, string port, string name, string user, string password)
    {
        Host = host;
        Port = port;
        Name = name;
        User = user;
        Password = password;
    }

    public string ConnectionString =>
        $"Host={Host};Port={Port};Database={Name};Username={User};Password={Password}";

    // Prefixed (PATIENT_PORTAL_DB_*) so this can run alongside a single-tenant
    // instance's own DB_* vars without colliding, in the same environment.
    public static DatabaseCredentials FromConfiguration(IConfiguration configuration)
    {
        var host = RequireValue(configuration, "PATIENT_PORTAL_DB_HOST");
        var port = RequireValue(configuration, "PATIENT_PORTAL_DB_PORT");
        var name = RequireValue(configuration, "PATIENT_PORTAL_DB_NAME");
        var user = RequireValue(configuration, "PATIENT_PORTAL_DB_USER");
        var password = RequireValue(configuration, "PATIENT_PORTAL_DB_PASSWORD");

        return new DatabaseCredentials(host, port, name, user, password);
    }

    private static string RequireValue(IConfiguration configuration, string key) =>
        configuration[key] ?? throw new InvalidOperationException($"Missing {key} environment variable.");
}
