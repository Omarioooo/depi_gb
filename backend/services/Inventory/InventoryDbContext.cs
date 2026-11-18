using Microsoft.EntityFrameworkCore;

namespace Inventory;

public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    public DbSet<StockItem> StockItems => Set<StockItem>();

    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var stock = modelBuilder.Entity<StockItem>();
        stock.ToTable("stock_items");
        stock.HasKey(s => s.Id);
        stock.HasIndex(s => new { s.ProductId, s.Location }).IsUnique();
        stock.Property(s => s.ProductId).IsRequired();
        stock.Property(s => s.Location).HasMaxLength(100).IsRequired();
        stock.Property(s => s.Quantity).IsRequired();
        stock.Property(s => s.Threshold).IsRequired();
        stock.Property(s => s.CreatedAt).IsRequired();
        stock.Property(s => s.UpdatedAt).IsRequired();
        stock.Property(s => s.Version).IsRowVersion();

        var movement = modelBuilder.Entity<StockMovement>();
        movement.ToTable("stock_movements");
        movement.HasKey(m => m.Id);
        movement.HasIndex(m => new { m.ProductId, m.OccurredAt });
        movement.HasIndex(m => new { m.Location, m.OccurredAt });
        movement.HasIndex(m => m.OperationId);
        movement.Property(m => m.ProductId).IsRequired();
        movement.Property(m => m.Location).HasMaxLength(100).IsRequired();
        movement.Property(m => m.Type).HasConversion<string>().HasMaxLength(32).IsRequired();
        movement.Property(m => m.Quantity).IsRequired();
        movement.Property(m => m.QuantityAfter).IsRequired();
        movement.Property(m => m.Reason).HasMaxLength(500);
        movement.Property(m => m.Reference).HasMaxLength(100);
        movement.Property(m => m.PerformedBy).HasMaxLength(200);
        movement.Property(m => m.OccurredAt).IsRequired();

        var idempotency = modelBuilder.Entity<IdempotencyRecord>();
        idempotency.ToTable("idempotency_records");
        idempotency.HasKey(i => i.Key);
        idempotency.Property(i => i.Key).HasMaxLength(128);
        idempotency.Property(i => i.Operation).HasMaxLength(32).IsRequired();
        idempotency.HasIndex(i => i.OperationId);
        idempotency.Property(i => i.CreatedAt).IsRequired();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in ChangeTracker.Entries<StockItem>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(s => s.CreatedAt).IsModified = false;
                entry.Entity.UpdatedAt = now;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
