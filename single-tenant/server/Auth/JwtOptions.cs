namespace BrydonServer.Auth;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public required string SigningKey { get; set; }
    public int ExpiryMinutes { get; set; } = 60;
}
