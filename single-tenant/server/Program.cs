using BrydonServer.Auth;
using BrydonServer.Data;
using BrydonServer.Hosting;
using BrydonServer.Sync;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

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

builder.Services.AddSingleton(PatientPortalReportingOptions.FromConfiguration(builder.Configuration));
builder.Services.AddHttpClient<PatientPortalReportingService>()
    .AddPolicyHandler(PatientPortalRetryPolicy.Retry())
    .AddPolicyHandler(PatientPortalRetryPolicy.CircuitBreaker());
builder.Services.AddHostedService<TenantUserSyncHostedService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => JwtBearerConfiguration.Configure(options, jwtOptions, deploymentOrigin));

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

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .WithName("HealthCheck")
    .AllowAnonymous();

app.Run();
