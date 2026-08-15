namespace BrydonServer.Data;

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

    // Assembled from discrete environment variables (12-factor style) rather
    // than a connection string baked into appsettings.json.
    public static DatabaseCredentials FromConfiguration(IConfiguration configuration)
    {
        var host = RequireValue(configuration, "DB_HOST");
        var port = RequireValue(configuration, "DB_PORT");
        var name = RequireValue(configuration, "DB_NAME");
        var user = RequireValue(configuration, "DB_USER");
        var password = RequireValue(configuration, "DB_PASSWORD");

        return new DatabaseCredentials(host, port, name, user, password);
    }

    private static string RequireValue(IConfiguration configuration, string key) =>
        configuration[key] ?? throw new InvalidOperationException($"Missing {key} environment variable.");
}
