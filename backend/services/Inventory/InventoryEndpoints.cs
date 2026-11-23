using Inventory.DTOs;
using Inventory.Services;
using Microsoft.AspNetCore.Mvc;

namespace Inventory;

public static class InventoryEndpoints
{
    private static readonly string[] StockStatuses = ["in-stock", "below-threshold", "out-of-stock"];

    public static RouteGroupBuilder MapInventoryEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/inventory").WithTags("Inventory");

        group.MapPost("/stock-in", async (
            StockOperationRequest request,
            StockService service,
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
            CancellationToken ct) =>
        {
            var validation = InventoryRequestValidation.ValidateStockChange(
                request.ProductId, request.Location, request.Quantity, request.Reason,
                request.Reference, request.PerformedBy, request.Threshold, idempotencyKey);
            if (validation is not null) return validation;

            var (movement, error) = await service.StockInAsync(request, idempotencyKey, ct);
            return error is null ? Results.Ok(movement) : Results.Conflict(error);
        }).WithName("StockIn").Produces<StockMovement>();

        group.MapPost("/stock-out", async (
            StockOperationRequest request,
            StockService service,
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
            CancellationToken ct) =>
        {
            var validation = InventoryRequestValidation.ValidateStockChange(
                request.ProductId, request.Location, request.Quantity, request.Reason,
                request.Reference, request.PerformedBy, request.Threshold, idempotencyKey);
            if (validation is not null) return validation;

            var (movement, error, notFound) = await service.StockOutAsync(request, idempotencyKey, ct);
            if (notFound)
                return Results.NotFound(new ApiError("stock_not_found", "No stock item exists for this product at this location."));
            return error is null ? Results.Ok(movement) : Results.Conflict(error);
        }).WithName("StockOut").Produces<StockMovement>();

        group.MapPost("/adjustments", async (
            AdjustStockRequest request,
            StockService service,
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
            CancellationToken ct) =>
        {
            var validation = InventoryRequestValidation.ValidateAdjustment(
                request.ProductId, request.Location, request.QuantityDelta, request.Reason,
                request.Reference, request.PerformedBy, idempotencyKey);
            if (validation is not null) return validation;

            var (movement, error, notFound) = await service.AdjustAsync(request, idempotencyKey, ct);
            if (notFound)
                return Results.NotFound(new ApiError("stock_not_found", "No stock item exists for this product at this location."));
            return error is null ? Results.Ok(movement) : Results.Conflict(error);
        }).WithName("AdjustStock").Produces<StockMovement>();

        group.MapPost("/transfers", async (
            TransferStockRequest request,
            StockService service,
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
            CancellationToken ct) =>
        {
            var validation = InventoryRequestValidation.ValidateTransfer(
                request.ProductId, request.FromLocation, request.ToLocation, request.Quantity,
                request.Reason, request.Reference, request.PerformedBy, idempotencyKey);
            if (validation is not null) return validation;

            var (movements, error, notFound) = await service.TransferAsync(request, idempotencyKey, ct);
            if (notFound)
                return Results.NotFound(new ApiError("stock_not_found", "No stock item exists for this product at the source location."));
            return error is null ? Results.Ok(movements) : Results.Conflict(error);
        }).WithName("TransferStock").Produces<IReadOnlyList<StockMovement>>();

        group.MapGet("/", async (
            ListStockService service,
            int? page,
            int? pageSize,
            Guid? productId,
            string? location,
            string? stockStatus,
            CancellationToken ct) =>
        {
            var currentPage = page ?? 1;
            var size = pageSize ?? 20;
            if (currentPage < 1 || size < 1 || size > 100)
                return Results.BadRequest(new ApiError("invalid_pagination", "Page must be positive and pageSize must be between 1 and 100."));

            var status = string.IsNullOrWhiteSpace(stockStatus) ? null : stockStatus.Trim().ToLowerInvariant();
            if (status is not null && !StockStatuses.Contains(status))
                return Results.BadRequest(new ApiError(
                    "invalid_stock_status", "stockStatus must be one of: in-stock, below-threshold, out-of-stock."));

            var result = await service.ExecuteAsync(currentPage, size, productId, location, status, ct);
            return Results.Ok(result);
        }).WithName("ListStock").Produces<StockListResponse>();

        group.MapGet("/items/{id:guid}", async (Guid id, GetStockItemService service, CancellationToken ct) =>
        {
            var item = await service.ExecuteAsync(id, ct);
            return item is null
                ? Results.NotFound(new ApiError("stock_not_found", "Stock item was not found."))
                : Results.Ok(item);
        }).WithName("GetStockItem").Produces<StockItem>();

        group.MapGet("/movements", async (
            ListMovementsService service,
            int? page,
            int? pageSize,
            Guid? productId,
            string? location,
            MovementType? type,
            DateTimeOffset? from,
            DateTimeOffset? to,
            CancellationToken ct) =>
        {
            var currentPage = page ?? 1;
            var size = pageSize ?? 20;
            if (currentPage < 1 || size < 1 || size > 100)
                return Results.BadRequest(new ApiError("invalid_pagination", "Page must be positive and pageSize must be between 1 and 100."));
            if (from.HasValue && to.HasValue && from.Value > to.Value)
                return Results.BadRequest(new ApiError("invalid_date_range", "from must be earlier than or equal to to."));

            var result = await service.ExecuteAsync(currentPage, size, productId, location, type, from, to, ct);
            return Results.Ok(result);
        }).WithName("ListMovements").Produces<MovementListResponse>();

        return group;
    }
}

internal static class InventoryRequestValidation
{
    public static IResult? ValidateStockChange(
        Guid productId,
        string? location,
        int quantity,
        string? reason,
        string? reference,
        string? performedBy,
        int? threshold,
        string? idempotencyKey)
    {
        var common = ValidateCommon(productId, location, reference, performedBy, idempotencyKey);
        if (common is not null) return common;

        if (quantity <= 0)
            return Results.BadRequest(new ApiError("invalid_quantity", "Quantity must be greater than zero."));
        if (reason?.Length > 500)
            return Results.BadRequest(new ApiError("invalid_reason", "Reason must be at most 500 characters."));
        if (threshold is < 0)
            return Results.BadRequest(new ApiError("invalid_threshold", "Threshold must not be negative."));
        return null;
    }

    public static IResult? ValidateAdjustment(
        Guid productId,
        string? location,
        int quantityDelta,
        string? reason,
        string? reference,
        string? performedBy,
        string? idempotencyKey)
    {
        var common = ValidateCommon(productId, location, reference, performedBy, idempotencyKey);
        if (common is not null) return common;

        if (quantityDelta == 0)
            return Results.BadRequest(new ApiError("invalid_quantity", "QuantityDelta must not be zero."));
        if (string.IsNullOrWhiteSpace(reason))
            return Results.BadRequest(new ApiError("missing_reason", "An adjustment requires a reason."));
        if (reason.Trim().Length > 500)
            return Results.BadRequest(new ApiError("invalid_reason", "Reason must be at most 500 characters."));
        return null;
    }

    public static IResult? ValidateTransfer(
        Guid productId,
        string? fromLocation,
        string? toLocation,
        int quantity,
        string? reason,
        string? reference,
        string? performedBy,
        string? idempotencyKey)
    {
        var common = ValidateCommon(productId, fromLocation, reference, performedBy, idempotencyKey);
        if (common is not null) return common;

        if (string.IsNullOrWhiteSpace(toLocation) || toLocation.Trim().Length > 100)
            return Results.BadRequest(new ApiError(
                "invalid_location", "ToLocation is required and must be at most 100 characters."));
        if (string.Equals(fromLocation!.Trim(), toLocation.Trim(), StringComparison.OrdinalIgnoreCase))
            return Results.BadRequest(new ApiError("invalid_location", "Source and destination locations must differ."));
        if (quantity <= 0)
            return Results.BadRequest(new ApiError("invalid_quantity", "Quantity must be greater than zero."));
        if (reason?.Length > 500)
            return Results.BadRequest(new ApiError("invalid_reason", "Reason must be at most 500 characters."));
        return null;
    }

    private static IResult? ValidateCommon(
        Guid productId,
        string? location,
        string? reference,
        string? performedBy,
        string? idempotencyKey)
    {
        if (productId == Guid.Empty)
            return Results.BadRequest(new ApiError("invalid_product", "ProductId is required."));
        if (string.IsNullOrWhiteSpace(location) || location.Trim().Length > 100)
            return Results.BadRequest(new ApiError(
                "invalid_location", "Location is required and must be at most 100 characters."));
        if (reference?.Length > 100)
            return Results.BadRequest(new ApiError("invalid_reference", "Reference must be at most 100 characters."));
        if (performedBy?.Length > 200)
            return Results.BadRequest(new ApiError("invalid_user", "PerformedBy must be at most 200 characters."));
        if (idempotencyKey?.Length > 128)
            return Results.BadRequest(new ApiError(
                "invalid_idempotency_key", "Idempotency-Key must be at most 128 characters."));
        return null;
    }
}
