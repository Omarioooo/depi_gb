using Microsoft.EntityFrameworkCore;
using Product;
using Product.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddScoped<CreateProductService>();
builder.Services.AddScoped<ListProductsService>();
builder.Services.AddScoped<GetProductService>();
builder.Services.AddScoped<UpdateProductService>();
builder.Services.AddScoped<DeactivateProductService>();

var connectionString = builder.Configuration.GetConnectionString("Products")
    ?? builder.Configuration["PRODUCT_DB_CONNECTION"];

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Configure ConnectionStrings:Products or PRODUCT_DB_CONNECTION for the Product service.");
}

builder.Services.AddDbContext<ProductDbContext>(options => options.UseNpgsql(connectionString));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthChecks("/health");
app.MapProductEndpoints();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();
