using Microsoft.EntityFrameworkCore;

namespace Product;

public sealed class ProductDbContext(DbContextOptions<ProductDbContext> options) : DbContext(options)
{
    public DbSet<ProductItem> Products => Set<ProductItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var product = modelBuilder.Entity<ProductItem>();
        product.ToTable("products");
        product.HasKey(p => p.Id);
        product.HasIndex(p => p.Sku).IsUnique();
        product.Property(p => p.Sku).HasMaxLength(64).IsRequired();
        product.Property(p => p.Name).HasMaxLength(160).IsRequired();
        product.Property(p => p.Category).HasMaxLength(100).IsRequired();
        product.Property(p => p.UnitOfMeasure).HasMaxLength(40).IsRequired();
        product.Property(p => p.Description).HasMaxLength(2000);
        product.Property(p => p.CreatedAt).IsRequired();
        product.Property(p => p.UpdatedAt).IsRequired();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in ChangeTracker.Entries<ProductItem>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(p => p.CreatedAt).IsModified = false;
                entry.Entity.UpdatedAt = now;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
