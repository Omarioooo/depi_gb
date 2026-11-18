namespace Product.DTOs;

public sealed record CreateProductRequest(
    string Sku,
    string Name,
    string Category,
    string UnitOfMeasure,
    string? Description);
