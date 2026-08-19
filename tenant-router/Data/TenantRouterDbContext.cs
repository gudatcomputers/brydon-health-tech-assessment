using Microsoft.EntityFrameworkCore;
using TenantRouter.Tenants;

namespace TenantRouter.Data;

public class TenantRouterDbContext(DbContextOptions<TenantRouterDbContext> options) : DbContext(options)
{
    public DbSet<Subdomain> Subdomains => Set<Subdomain>();
    public DbSet<TenantUsername> TenantUsernames => Set<TenantUsername>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Subdomain>(entity =>
        {
            entity.ToTable("subdomains");
            entity.Property(s => s.Name).HasMaxLength(256).IsRequired();
            entity.Property(s => s.ServerUrl).HasMaxLength(512);
            entity.Property(s => s.ClientOrigin).HasMaxLength(512);
            entity.HasIndex(s => s.Name).IsUnique();
        });

        modelBuilder.Entity<TenantUsername>(entity =>
        {
            entity.ToTable("tenant_usernames");
            entity.HasKey(t => new { t.SubdomainId, t.UsernameHash });
            entity.Property(t => t.UsernameHash).HasMaxLength(512).IsRequired();
            entity.HasIndex(t => t.UsernameHash);
            entity.Property(t => t.UpdatedOn)
                .HasColumnType("timestamp without time zone")
                .HasDefaultValueSql("timezone('utc', now())")
                .IsRequired();

            entity.HasOne(t => t.Subdomain)
                .WithMany()
                .HasForeignKey(t => t.SubdomainId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    // Stamps UpdatedOn on every insert/update of a TenantUsername — the DB
    // default (see above) only covers rows written outside EF Core; this is
    // what actually keeps it current for normal application writes.
    public override int SaveChanges()
    {
        StampUpdatedOn();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampUpdatedOn();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void StampUpdatedOn()
    {
        // "timestamp without time zone" has no concept of a zone, and Npgsql
        // rejects a Kind=Utc DateTime for it outright — Unspecified is what
        // it wants. The value itself is still UTC wall-clock time; only the
        // .NET Kind tag changes, not the stored value.
        var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
        foreach (var entry in ChangeTracker.Entries<TenantUsername>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.UpdatedOn = now;
            }
        }
    }
}
