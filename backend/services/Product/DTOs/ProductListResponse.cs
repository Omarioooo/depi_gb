namespace Product.DTOs;

public sealed record ProductListResponse(
    IReadOnlyList<ProductItem> Items,
    int Page,
    int PageSize,
    int TotalCount);
