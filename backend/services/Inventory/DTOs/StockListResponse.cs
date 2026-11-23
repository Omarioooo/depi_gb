namespace Inventory.DTOs;

public sealed record StockListResponse(
    IReadOnlyList<StockItem> Items,
    int Page,
    int PageSize,
    int TotalCount);
