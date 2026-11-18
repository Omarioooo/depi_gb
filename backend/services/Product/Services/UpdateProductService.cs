using Microsoft.EntityFrameworkCore;
using Npgsql;
using Product.DTOs;

namespace Product.Services;

public sealed class UpdateProductService(ProductDbContext db)
{
    public async Task<(ProductItem? Product, bool NotFound, bool DuplicateSku)> ExecuteAsync(
        Guid id,
        UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product is null) return (null, true, false);

        var sku = request.Sku.Trim();
        if (await db.Products.AnyAsync(p => p.Sku == sku && p.Id != id, cancellationToken))
            return (null, false, true);

        product.Sku = sku;
        product.Name = request.Name.Trim();
        product.Category = request.Category.Trim();
        product.UnitOfMeasure = request.UnitOfMeasure.Trim();
        product.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsDuplicateSku(exception))
        {
            return (null, false, true);
        }

        return (product, false, false);
    }

    private static bool IsDuplicateSku(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "IX_products_Sku"
        };
}
