using Microsoft.EntityFrameworkCore;
using PatientPortalServer.Data;
using PatientPortalServer.Tenants;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var dbCredentials = DatabaseCredentials.FromConfiguration(builder.Configuration);
builder.Services.AddDbContext<PatientPortalDbContext>(options => options.UseNpgsql(dbCredentials.ConnectionString));

builder.Services.AddSingleton(TenantReportSecret.FromConfiguration(builder.Configuration));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .WithName("HealthCheck")
    .AllowAnonymous();

app.MapTenantReportEndpoints();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PatientPortalDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();
