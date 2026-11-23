namespace Inventory.DTOs;

public sealed record MovementListResponse(
    IReadOnlyList<StockMovement> Items,
    int Page,
    int PageSize,
    int TotalCount);
