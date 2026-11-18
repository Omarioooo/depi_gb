namespace Inventory.DTOs;

public sealed record TransferStockRequest(
    Guid ProductId,
    string FromLocation,
    string ToLocation,
    int Quantity,
    string? Reference,
    string? PerformedBy,
    string? Reason);
