using Microsoft.EntityFrameworkCore;
using Npgsql;
using Product.DTOs;

namespace Product.Services;

public sealed class CreateProductService(ProductDbContext db)
{
    public async Task<(ProductItem? Product, bool DuplicateSku)> ExecuteAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var sku = request.Sku.Trim();
        if (await db.Products.AnyAsync(p => p.Sku == sku, cancellationToken))
            return (null, true);

        var product = new ProductItem
        {
            Sku = sku,
            Name = request.Name.Trim(),
            Category = request.Category.Trim(),
            UnitOfMeasure = request.UnitOfMeasure.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim()
        };

        db.Products.Add(product);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsDuplicateSku(exception))
        {
            return (null, true);
        }

        return (product, false);
    }

    private static bool IsDuplicateSku(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "IX_products_Sku"
        };
}
