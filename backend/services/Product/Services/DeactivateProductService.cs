using Microsoft.EntityFrameworkCore;

namespace Product.Services;

public sealed class DeactivateProductService(ProductDbContext db)
{
    public async Task<bool> ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product is null) return false;

        if (product.IsActive)
        {
            product.IsActive = false;
            await db.SaveChangesAsync(cancellationToken);
        }

        return true;
    }
}
