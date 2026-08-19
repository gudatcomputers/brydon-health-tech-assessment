namespace TenantRouter.Data;

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

    // Prefixed (TENANT_ROUTER_DB_*) so this can run alongside other services'
    // own DB_* vars without colliding, in the same environment.
    public static DatabaseCredentials FromConfiguration(IConfiguration configuration)
    {
        var host = RequireValue(configuration, "TENANT_ROUTER_DB_HOST");
        var port = RequireValue(configuration, "TENANT_ROUTER_DB_PORT");
        var name = RequireValue(configuration, "TENANT_ROUTER_DB_NAME");
        var user = RequireValue(configuration, "TENANT_ROUTER_DB_USER");
        var password = RequireValue(configuration, "TENANT_ROUTER_DB_PASSWORD");

        return new DatabaseCredentials(host, port, name, user, password);
    }

    private static string RequireValue(IConfiguration configuration, string key) =>
        configuration[key] ?? throw new InvalidOperationException($"Missing {key} environment variable.");
}
