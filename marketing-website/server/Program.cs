using MarketingWebsiteServer.Auth;
using MarketingWebsiteServer.Tenants;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSingleton(TenantRouterOptions.FromConfiguration(builder.Configuration));
builder.Services.AddHttpClient<TenantRouterClient>();

// Used to proxy a login to a tenant's server. No retry/circuit breaker here
// (unlike single-tenant's outbound calls to tenant-router) — this is an
// interactive login request, so failing fast is the right behavior, not
// backing off and retrying while the user waits.
builder.Services.AddHttpClient(MarketingLoginEndpoints.LoginProxyClientName, c => c.Timeout = TimeSpan.FromSeconds(10));

var clientOrigin = builder.Configuration["CLIENT_ORIGIN"] ?? "http://localhost:5175";

builder.Services.AddCors(options =>
{
    options.AddPolicy("ClientApp", policy =>
        policy.WithOrigins(clientOrigin).AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("ClientApp");

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .WithName("HealthCheck")
    .AllowAnonymous();

app.MapMarketingLoginEndpoints();

app.Run();
