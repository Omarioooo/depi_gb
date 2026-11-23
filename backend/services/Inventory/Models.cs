using System.ComponentModel.DataAnnotations;

namespace Inventory;

public enum MovementType
{
    In,
    Out,
    Adjustment,
    TransferOut,
    TransferIn
}

public sealed class StockItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ProductId { get; set; }

    [Required, MaxLength(100)]
    public string Location { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public int Threshold { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Maps to the PostgreSQL xmin system column; guards against lost updates.
    public uint Version { get; set; }
}

public sealed class StockMovement
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid StockItemId { get; set; }

    public Guid ProductId { get; set; }

    [Required, MaxLength(100)]
    public string Location { get; set; } = string.Empty;

    public MovementType Type { get; set; }

    /// <summary>Signed change applied to the location: negative for outgoing stock.</summary>
    public int Quantity { get; set; }

    /// <summary>Stock level at the location after the change.</summary>
    public int QuantityAfter { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }

    [MaxLength(100)]
    public string? Reference { get; set; }

    [MaxLength(200)]
    public string? PerformedBy { get; set; }

    /// <summary>Links the movements produced by one request, such as both sides of a transfer.</summary>
    public Guid OperationId { get; set; }

    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class IdempotencyRecord
{
    [Required, MaxLength(128)]
    public string Key { get; set; } = string.Empty;

    [Required, MaxLength(32)]
    public string Operation { get; set; } = string.Empty;

    public Guid MovementId { get; set; }

    public Guid OperationId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
