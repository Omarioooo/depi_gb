using Inventory.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Services;

public sealed class ListMovementsService(InventoryDbContext db)
{
    public async Task<MovementListResponse> ExecuteAsync(
        int page,
        int pageSize,
        Guid? productId,
        string? location,
        MovementType? type,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        var query = db.StockMovements.AsNoTracking().AsQueryable();

        if (productId.HasValue) query = query.Where(m => m.ProductId == productId.Value);
        if (!string.IsNullOrWhiteSpace(location)) query = query.Where(m => m.Location == location.Trim());
        if (type.HasValue) query = query.Where(m => m.Type == type.Value);
        if (from.HasValue) query = query.Where(m => m.OccurredAt >= from.Value);
        if (to.HasValue) query = query.Where(m => m.OccurredAt <= to.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(m => m.OccurredAt).ThenByDescending(m => m.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new MovementListResponse(items, page, pageSize, total);
    }
}
