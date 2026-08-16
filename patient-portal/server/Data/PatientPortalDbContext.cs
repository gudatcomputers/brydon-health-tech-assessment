using Microsoft.EntityFrameworkCore;
using PatientPortalServer.Tenants;

namespace PatientPortalServer.Data;

public class PatientPortalDbContext(DbContextOptions<PatientPortalDbContext> options) : DbContext(options)
{
    public DbSet<TenantUsername> TenantUsernames => Set<TenantUsername>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TenantUsername>(entity =>
        {
            entity.ToTable("tenant_usernames");
            entity.HasKey(t => new { t.Subdomain, t.UsernameHash });
            entity.Property(t => t.Subdomain).HasMaxLength(256).IsRequired();
            entity.Property(t => t.UsernameHash).HasMaxLength(512).IsRequired();
            entity.HasIndex(t => t.UsernameHash);
        });
    }
}
