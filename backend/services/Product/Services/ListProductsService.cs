using Microsoft.EntityFrameworkCore;
using Product.DTOs;

namespace Product.Services;

public sealed class ListProductsService(ProductDbContext db)
{
    public async Task<ProductListResponse> ExecuteAsync(
        int page,
        int pageSize,
        string? sku,
        string? name,
        string? category,
        bool? isActive,
        CancellationToken cancellationToken)
    {
        var query = db.Products.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(sku)) query = query.Where(p => p.Sku.Contains(sku.Trim()));
        if (!string.IsNullOrWhiteSpace(name)) query = query.Where(p => p.Name.Contains(name.Trim()));
        if (!string.IsNullOrWhiteSpace(category)) query = query.Where(p => p.Category == category.Trim());
        if (isActive.HasValue) query = query.Where(p => p.IsActive == isActive.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(p => p.Name).ThenBy(p => p.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new ProductListResponse(items, page, pageSize, total);
    }
}
