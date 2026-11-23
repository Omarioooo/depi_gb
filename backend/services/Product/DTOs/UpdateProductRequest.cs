namespace Product.DTOs;

public sealed record UpdateProductRequest(
    string Sku,
    string Name,
    string Category,
    string UnitOfMeasure,
    string? Description);
