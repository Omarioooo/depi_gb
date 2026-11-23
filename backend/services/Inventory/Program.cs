using Inventory;
using Inventory.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddScoped<StockService>();
builder.Services.AddScoped<ListStockService>();
builder.Services.AddScoped<GetStockItemService>();
builder.Services.AddScoped<ListMovementsService>();

var connectionString = builder.Configuration.GetConnectionString("Inventory")
    ?? builder.Configuration["INVENTORY_DB_CONNECTION"];

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Configure ConnectionStrings:Inventory or INVENTORY_DB_CONNECTION for the Inventory service.");
}

builder.Services.AddDbContext<InventoryDbContext>(options => options.UseNpgsql(connectionString));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthChecks("/health");
app.MapInventoryEndpoints();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();
