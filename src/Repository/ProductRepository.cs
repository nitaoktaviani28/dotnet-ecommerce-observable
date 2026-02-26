using DotnetEcommerce.Models;
using Npgsql;

namespace DotnetEcommerce.Repository;

/// <summary>
/// Product repository - clean business logic.
/// NO observability code here.
/// PostgreSQL queries are automatically traced by OpenTelemetry Npgsql instrumentation.
/// </summary>
public class ProductRepository
{
    private readonly IDbConnectionFactory _factory;

    public ProductRepository(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<List<Product>> GetAllAsync()
    {
        var products = new List<Product>();
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            "SELECT id, name, price FROM products ORDER BY id", conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            products.Add(new Product
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Price = reader.GetDecimal(2)
            });
        }

        return products;
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            "SELECT id, name, price FROM products WHERE id = $1", conn);
        cmd.Parameters.AddWithValue(id);
        await using var reader = await cmd.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new Product
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Price = reader.GetDecimal(2)
            };
        }

        return null;
    }
}
