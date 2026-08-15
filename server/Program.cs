using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BrydonServer.Auth;
using BrydonServer.Data;
using BrydonServer.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services
    .AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException($"Missing '{JwtOptions.SectionName}' configuration section.");

var deploymentOrigin = DeploymentOrigin.FromConfiguration(builder.Configuration);
builder.Services.AddSingleton(deploymentOrigin);

var dbCredentials = DatabaseCredentials.FromConfiguration(builder.Configuration);

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(dbCredentials.ConnectionString));

builder.Services.AddScoped<UserStore>();
builder.Services.AddSingleton<TokenService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
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
    });

builder.Services.AddAuthorization();

// In production, client and server share an origin behind a reverse proxy, so
// this is mainly for local dev where the client runs on a different port. It
// also always allows the deployment's own origin as a fallback.
var clientOrigin = builder.Configuration["CLIENT_ORIGIN"] ?? "http://localhost:5173";
var corsOrigins = new[] { clientOrigin, deploymentOrigin.BaseUrl }.Distinct().ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy("ClientApp", policy =>
        policy.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("ClientApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db, app.Configuration);
}

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.RequireAuthorization();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
