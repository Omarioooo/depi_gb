using Microsoft.EntityFrameworkCore;

namespace Product.Services;

public sealed class GetProductService(ProductDbContext db)
{
    public Task<ProductItem?> ExecuteAsync(Guid id, CancellationToken cancellationToken) =>
        db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
}
