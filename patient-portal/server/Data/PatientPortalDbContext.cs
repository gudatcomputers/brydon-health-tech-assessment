using Microsoft.EntityFrameworkCore;
using PatientPortalServer.Tenants;

namespace PatientPortalServer.Data;

public class PatientPortalDbContext(DbContextOptions<PatientPortalDbContext> options) : DbContext(options)
{
    public DbSet<Subdomain> Subdomains => Set<Subdomain>();
    public DbSet<TenantUsername> TenantUsernames => Set<TenantUsername>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Subdomain>(entity =>
        {
            entity.ToTable("subdomains");
            entity.Property(s => s.Name).HasMaxLength(256).IsRequired();
            entity.HasIndex(s => s.Name).IsUnique();
        });

        modelBuilder.Entity<TenantUsername>(entity =>
        {
            entity.ToTable("tenant_usernames");
            entity.HasKey(t => new { t.SubdomainId, t.UsernameHash });
            entity.Property(t => t.UsernameHash).HasMaxLength(512).IsRequired();
            entity.HasIndex(t => t.UsernameHash);

            entity.HasOne(t => t.Subdomain)
                .WithMany()
                .HasForeignKey(t => t.SubdomainId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
