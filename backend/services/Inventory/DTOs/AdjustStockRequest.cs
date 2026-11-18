namespace Inventory.DTOs;

public sealed record AdjustStockRequest(
    Guid ProductId,
    string Location,
    int QuantityDelta,
    string Reason,
    string? Reference,
    string? PerformedBy);
