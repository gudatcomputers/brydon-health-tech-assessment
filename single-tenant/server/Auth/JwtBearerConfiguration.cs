using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BrydonServer.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace BrydonServer.Auth;

public static class JwtBearerConfiguration
{
    public static void Configure(JwtBearerOptions options, JwtOptions jwtOptions, DeploymentOrigin deploymentOrigin)
    {
        // Without this, the handler remaps well-known claim types on the way in
        // (e.g. "sub" -> ClaimTypes.NameIdentifier), so reading the raw JWT claim
        // names below would otherwise come back null.
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = deploymentOrigin.BaseUrl,
            ValidateAudience = true,
            ValidAudience = deploymentOrigin.BaseUrl,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var subject = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);
                var tokenVersionClaim = context.Principal?.FindFirstValue("ver");

                if (subject is null || !Guid.TryParse(subject, out var userId) ||
                    tokenVersionClaim is null || !int.TryParse(tokenVersionClaim, out var tokenVersion))
                {
                    context.Fail("Token is missing required claims.");
                    return;
                }

                var userStore = context.HttpContext.RequestServices.GetRequiredService<UserStore>();
                var currentVersion = await userStore.GetTokenVersionAsync(userId);

                if (currentVersion is null || currentVersion != tokenVersion)
                {
                    context.Fail("Token has been revoked.");
                }
            }
        };
    }
}
