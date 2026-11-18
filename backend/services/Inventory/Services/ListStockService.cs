using Inventory.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Services;

public sealed class ListStockService(InventoryDbContext db)
{
    public async Task<StockListResponse> ExecuteAsync(
        int page,
        int pageSize,
        Guid? productId,
        string? location,
        string? stockStatus,
        CancellationToken cancellationToken)
    {
        var query = db.StockItems.AsNoTracking().AsQueryable();

        if (productId.HasValue) query = query.Where(s => s.ProductId == productId.Value);
        if (!string.IsNullOrWhiteSpace(location)) query = query.Where(s => s.Location == location.Trim());

        query = stockStatus switch
        {
            "out-of-stock" => query.Where(s => s.Quantity == 0),
            "below-threshold" => query.Where(s => s.Quantity > 0 && s.Quantity < s.Threshold),
            "in-stock" => query.Where(s => s.Quantity >= s.Threshold),
            _ => query
        };

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(s => s.Location).ThenBy(s => s.ProductId)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new StockListResponse(items, page, pageSize, total);
    }
}
