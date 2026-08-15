using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace BrydonServer.Auth;

public record LoginRequest(string Username, string Password);

public record LoginResponse(string Token, DateTime ExpiresAt);

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/api/auth/login", async (LoginRequest request, UserStore userStore, TokenService tokenService) =>
        {
            var user = await userStore.FindByUsernameAsync(request.Username);
            if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
            {
                return Results.Unauthorized();
            }

            var (token, expiresAt) = tokenService.GenerateToken(user);
            return Results.Ok(new LoginResponse(token, expiresAt));
        })
        .WithName("Login")
        .AllowAnonymous();

        app.MapPost("/api/auth/logout", async (ClaimsPrincipal user, UserStore userStore) =>
        {
            var subject = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (subject is null || !Guid.TryParse(subject, out var userId))
            {
                return Results.BadRequest();
            }

            await userStore.IncrementTokenVersionAsync(userId);

            return Results.NoContent();
        })
        .WithName("Logout")
        .RequireAuthorization();
    }
}
