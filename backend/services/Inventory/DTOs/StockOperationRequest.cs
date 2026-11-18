namespace Inventory.DTOs;

public sealed record StockOperationRequest(
    Guid ProductId,
    string Location,
    int Quantity,
    string? Reference,
    string? PerformedBy,
    string? Reason,
    int? Threshold);
