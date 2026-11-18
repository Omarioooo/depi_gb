using Inventory.DTOs;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Inventory.Services;

/// <summary>
/// Atomic stock rules: negative-stock guard, immutable movement history, optimistic
/// concurrency through the xmin token, and idempotent replay of repeated requests.
/// </summary>
public sealed class StockService(InventoryDbContext db)
{
    private const string IdempotencyConstraint = "PK_idempotency_records";

    public async Task<(StockMovement? Movement, ApiError? Error)> StockInAsync(
        StockOperationRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var key = NormalizeKey(idempotencyKey);
        var (replayed, replayError, isReplay) = await ReplayAsync(key, cancellationToken);
        if (isReplay) return (replayed, replayError);

        var location = request.Location.Trim();
        var item = await db.StockItems
            .FirstOrDefaultAsync(s => s.ProductId == request.ProductId && s.Location == location, cancellationToken);

        if (item is null)
        {
            item = new StockItem
            {
                ProductId = request.ProductId,
                Location = location,
                Quantity = 0,
                Threshold = request.Threshold ?? 0
            };
            db.StockItems.Add(item);
        }

        item.Quantity += request.Quantity;

        var operationId = Guid.NewGuid();
        var movement = CreateMovement(
            item, MovementType.In, request.Quantity, request.Reason, request.Reference, request.PerformedBy, operationId);
        db.StockMovements.Add(movement);
        ReserveKey(key, "stock-in", movement, operationId);

        var (movements, error) = await SaveAsync([movement], key, cancellationToken);
        return (movements?[0], error);
    }

    public async Task<(StockMovement? Movement, ApiError? Error, bool NotFound)> StockOutAsync(
        StockOperationRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var key = NormalizeKey(idempotencyKey);
        var (replayed, replayError, isReplay) = await ReplayAsync(key, cancellationToken);
        if (isReplay) return (replayed, replayError, false);

        var location = request.Location.Trim();
        var item = await db.StockItems
            .FirstOrDefaultAsync(s => s.ProductId == request.ProductId && s.Location == location, cancellationToken);
        if (item is null) return (null, null, true);

        if (item.Quantity < request.Quantity)
            return (null, new ApiError("insufficient_stock", $"Available stock at '{item.Location}' is {item.Quantity}."), false);

        item.Quantity -= request.Quantity;

        var operationId = Guid.NewGuid();
        var movement = CreateMovement(
            item, MovementType.Out, -request.Quantity, request.Reason, request.Reference, request.PerformedBy, operationId);
        db.StockMovements.Add(movement);
        ReserveKey(key, "stock-out", movement, operationId);

        var (movements, error) = await SaveAsync([movement], key, cancellationToken);
        return (movements?[0], error, false);
    }

    public async Task<(StockMovement? Movement, ApiError? Error, bool NotFound)> AdjustAsync(
        AdjustStockRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var key = NormalizeKey(idempotencyKey);
        var (replayed, replayError, isReplay) = await ReplayAsync(key, cancellationToken);
        if (isReplay) return (replayed, replayError, false);

        var location = request.Location.Trim();
        var item = await db.StockItems
            .FirstOrDefaultAsync(s => s.ProductId == request.ProductId && s.Location == location, cancellationToken);
        if (item is null) return (null, null, true);

        var resultingQuantity = item.Quantity + request.QuantityDelta;
        if (resultingQuantity < 0)
            return (null, new ApiError(
                "negative_stock",
                $"Adjustment would take stock below zero: available {item.Quantity}."), false);

        item.Quantity = resultingQuantity;

        var operationId = Guid.NewGuid();
        var movement = CreateMovement(
            item, MovementType.Adjustment, request.QuantityDelta, request.Reason.Trim(), request.Reference, request.PerformedBy, operationId);
        db.StockMovements.Add(movement);
        ReserveKey(key, "adjustment", movement, operationId);

        var (movements, error) = await SaveAsync([movement], key, cancellationToken);
        return (movements?[0], error, false);
    }

    public async Task<(IReadOnlyList<StockMovement>? Movements, ApiError? Error, bool NotFound)> TransferAsync(
        TransferStockRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var key = NormalizeKey(idempotencyKey);
        var (replayed, replayError, isReplay) = await ReplayAsync(key, cancellationToken);
        if (isReplay) return (replayed is null ? null : [replayed], replayError, false);

        var fromLocation = request.FromLocation.Trim();
        var toLocation = request.ToLocation.Trim();

        var source = await db.StockItems
            .FirstOrDefaultAsync(s => s.ProductId == request.ProductId && s.Location == fromLocation, cancellationToken);
        if (source is null) return (null, null, true);

        if (source.Quantity < request.Quantity)
            return (null, new ApiError(
                "insufficient_stock",
                $"Available stock at '{source.Location}' is {source.Quantity}."), false);

        var destination = await db.StockItems
            .FirstOrDefaultAsync(s => s.ProductId == request.ProductId && s.Location == toLocation, cancellationToken);
        if (destination is null)
        {
            destination = new StockItem
            {
                ProductId = request.ProductId,
                Location = toLocation,
                Quantity = 0
            };
            db.StockItems.Add(destination);
        }

        source.Quantity -= request.Quantity;
        destination.Quantity += request.Quantity;

        var operationId = Guid.NewGuid();
        var outbound = CreateMovement(
            source, MovementType.TransferOut, -request.Quantity, request.Reason, request.Reference, request.PerformedBy, operationId);
        var inbound = CreateMovement(
            destination, MovementType.TransferIn, request.Quantity, request.Reason, request.Reference, request.PerformedBy, operationId);

        db.StockMovements.AddRange(outbound, inbound);
        ReserveKey(key, "transfer", outbound, operationId);

        var (movements, error) = await SaveAsync([outbound, inbound], key, cancellationToken);
        return (movements, error, false);
    }

    private async Task<(IReadOnlyList<StockMovement>? Movements, ApiError? Error)> SaveAsync(
        IReadOnlyList<StockMovement> movements,
        string? key,
        CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return (movements, null);
        }
        catch (DbUpdateConcurrencyException)
        {
            db.ChangeTracker.Clear();
            return (null, ConcurrentUpdate());
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            db.ChangeTracker.Clear();

            if (key is not null && exception.InnerException is PostgresException { ConstraintName: IdempotencyConstraint })
            {
                var (movement, error, isReplay) = await ReplayAsync(key, cancellationToken);
                if (isReplay)
                    return (movement is null ? null : [movement], error);
            }

            return (null, ConcurrentUpdate());
        }
    }

    private async Task<(StockMovement? Movement, ApiError? Error, bool IsReplay)> ReplayAsync(
        string? key,
        CancellationToken cancellationToken)
    {
        if (key is null) return (null, null, false);

        var record = await db.IdempotencyRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Key == key, cancellationToken);
        if (record is null) return (null, null, false);

        var movement = await db.StockMovements
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == record.MovementId, cancellationToken);

        return movement is null
            ? (null, new ApiError("idempotency_key_reused", "This idempotency key was already processed."), true)
            : (movement, null, true);
    }

    private void ReserveKey(string? key, string operation, StockMovement movement, Guid operationId)
    {
        if (key is null) return;

        db.IdempotencyRecords.Add(new IdempotencyRecord
        {
            Key = key,
            Operation = operation,
            MovementId = movement.Id,
            OperationId = operationId,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }

    private static StockMovement CreateMovement(
        StockItem item,
        MovementType type,
        int quantity,
        string? reason,
        string? reference,
        string? performedBy,
        Guid operationId) =>
        new()
        {
            StockItemId = item.Id,
            ProductId = item.ProductId,
            Location = item.Location,
            Type = type,
            Quantity = quantity,
            QuantityAfter = item.Quantity,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            Reference = string.IsNullOrWhiteSpace(reference) ? null : reference.Trim(),
            PerformedBy = string.IsNullOrWhiteSpace(performedBy) ? null : performedBy.Trim(),
            OperationId = operationId,
            OccurredAt = DateTimeOffset.UtcNow
        };

    private static string? NormalizeKey(string? key) =>
        string.IsNullOrWhiteSpace(key) ? null : key.Trim();

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    private static ApiError ConcurrentUpdate() =>
        new("concurrent_update", "The stock item changed while this request was processed. Retry the operation.");
}
