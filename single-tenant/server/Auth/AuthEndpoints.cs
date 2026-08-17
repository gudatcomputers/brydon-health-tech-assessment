using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BrydonServer.Sync;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BrydonServer.Auth;

public record LoginRequest(string Username, string Password);

public record RegisterRequest(string Username, string Password);

public record LoginResponse(string Token, DateTime ExpiresAt);

public static class AuthEndpoints
{
    private const int MinPasswordLength = 8;

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

        app.MapPost("/api/auth/register", async (
            RegisterRequest request,
            UserStore userStore,
            TokenService tokenService,
            TenantUserSyncTrigger syncTrigger) =>
        {
            if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Length > 256 ||
                request.Password.Length < MinPasswordLength)
            {
                return Results.BadRequest();
            }

            if (await userStore.FindByUsernameAsync(request.Username) is not null)
            {
                // return BadRequest instead of Conflict to not leak user data
                return Results.BadRequest();
            }

            User user;
            try
            {
                user = await userStore.CreateAsync(request.Username, PasswordHasher.Hash(request.Password));
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                // Lost a race with another registration for the same username
                // between the check above and this insert.
                return Results.Conflict();
            }

            var (token, expiresAt) = tokenService.GenerateToken(user);

            // Don't make the caller wait on an external HTTP call — report in
            // the background so this user shows up in patient-portal without
            // needing a full server restart.
            _ = syncTrigger.RunAsync();

            return Results.Ok(new LoginResponse(token, expiresAt));
        })
        .WithName("Register")
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
