using Microsoft.EntityFrameworkCore;

namespace Inventory.Services;

public sealed class GetStockItemService(InventoryDbContext db)
{
    public Task<StockItem?> ExecuteAsync(Guid id, CancellationToken cancellationToken) =>
        db.StockItems.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
}
