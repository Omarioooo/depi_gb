using Product.DTOs;
using Product.Services;

namespace Product;

public static class ProductEndpoints
{
    public static RouteGroupBuilder MapProductEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/products").WithTags("Products");

        group.MapPost("/", async (CreateProductRequest request, CreateProductService service, CancellationToken ct) =>
        {
            var validation = ProductRequestValidation.Validate(
                request.Sku, request.Name, request.Category, request.UnitOfMeasure, request.Description);
            if (validation is not null) return validation;

            var (product, duplicateSku) = await service.ExecuteAsync(request, ct);
            if (duplicateSku)
                return Results.Conflict(new ApiError("duplicate_sku", "A product with this SKU already exists."));
            return Results.Created($"/api/products/{product!.Id}", product);
        }).WithName("CreateProduct").Produces<ProductItem>(201);

        group.MapGet("/", async (
            ListProductsService service,
            int? page,
            int? pageSize,
            string? sku,
            string? name,
            string? category,
            bool? isActive,
            CancellationToken ct) =>
        {
            var currentPage = page ?? 1;
            var size = pageSize ?? 20;
            if (currentPage < 1 || size < 1 || size > 100)
                return Results.BadRequest(new ApiError("invalid_pagination", "Page must be positive and pageSize must be between 1 and 100."));

            var result = await service.ExecuteAsync(currentPage, size, sku, name, category, isActive, ct);
            return Results.Ok(result);
        }).WithName("ListProducts").Produces<ProductListResponse>();

        group.MapGet("/{id:guid}", async (Guid id, GetProductService service, CancellationToken ct) =>
        {
            var product = await service.ExecuteAsync(id, ct);
            return product is null
                ? Results.NotFound(new ApiError("product_not_found", "Product was not found."))
                : Results.Ok(product);
        }).WithName("GetProduct").Produces<ProductItem>();

        group.MapPut("/{id:guid}", async (Guid id, UpdateProductRequest request, UpdateProductService service, CancellationToken ct) =>
        {
            var validation = ProductRequestValidation.Validate(
                request.Sku, request.Name, request.Category, request.UnitOfMeasure, request.Description);
            if (validation is not null) return validation;

            var (product, notFound, duplicateSku) = await service.ExecuteAsync(id, request, ct);
            if (notFound) return Results.NotFound(new ApiError("product_not_found", "Product was not found."));
            if (duplicateSku)
                return Results.Conflict(new ApiError("duplicate_sku", "A product with this SKU already exists."));
            return Results.Ok(product);
        }).WithName("UpdateProduct").Produces<ProductItem>();

        group.MapDelete("/{id:guid}", async (Guid id, DeactivateProductService service, CancellationToken ct) =>
        {
            var found = await service.ExecuteAsync(id, ct);
            return found
                ? Results.NoContent()
                : Results.NotFound(new ApiError("product_not_found", "Product was not found."));
        }).WithName("DeactivateProduct").Produces(204);

        return group;
    }
}

internal static class ProductRequestValidation
{
    public static IResult? Validate(string? sku, string? name, string? category, string? unit, string? description)
    {
        if (string.IsNullOrWhiteSpace(sku) || sku.Trim().Length > 64)
            return Results.BadRequest(new ApiError("invalid_sku", "SKU is required and must be at most 64 characters."));
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 160)
            return Results.BadRequest(new ApiError("invalid_name", "Name is required and must be at most 160 characters."));
        if (string.IsNullOrWhiteSpace(category) || category.Trim().Length > 100)
            return Results.BadRequest(new ApiError("invalid_category", "Category is required and must be at most 100 characters."));
        if (string.IsNullOrWhiteSpace(unit) || unit.Trim().Length > 40)
            return Results.BadRequest(new ApiError("invalid_unit", "Unit of measure is required and must be at most 40 characters."));
        if (description?.Length > 2000)
            return Results.BadRequest(new ApiError("invalid_description", "Description must be at most 2000 characters."));
        return null;
    }
}
