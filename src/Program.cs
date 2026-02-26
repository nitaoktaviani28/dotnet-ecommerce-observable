using DotnetEcommerce.Observability;
using DotnetEcommerce.Repository;

var builder = WebApplication.CreateBuilder(args);

// =========================
// OBSERVABILITY (SINGLE ENTRY POINT)
// =========================
ObservabilityBootstrap.Initialize(builder);

// =========================
// SERVICES
// =========================
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
builder.Services.AddScoped<ProductRepository>();
builder.Services.AddScoped<OrderRepository>();

var app = builder.Build();

// =========================
// DATABASE INITIALIZATION
// =========================
using (var scope = app.Services.CreateScope())
{
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
    await DbInitializer.InitializeAsync(dbFactory);
}

// =========================
// MIDDLEWARE
// =========================
app.UseRouting();
app.MapControllers();

// Prometheus metrics endpoint
app.MapMetrics();

Console.WriteLine("🚀 .NET E-commerce app starting on :8080");
app.Run("http://0.0.0.0:8080");
