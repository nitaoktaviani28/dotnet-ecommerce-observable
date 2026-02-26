using System;
using System.Threading.Tasks;
using DotnetEcommerce.Models;
using DotnetEcommerce.Observability;
using Npgsql;

namespace DotnetEcommerce.Repository;

/// <summary>
/// Order repository - clean business logic.
/// NO observability code here (except metric increment).
/// PostgreSQL queries are automatically traced by OpenTelemetry Npgsql instrumentation.
/// </summary>
public class OrderRepository
{
    private readonly IDbConnectionFactory _factory;

    public OrderRepository(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<int> CreateAsync(int productId, int quantity, decimal total)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            "INSERT INTO orders (product_id, quantity, total) VALUES ($1, $2, $3) RETURNING id",
            conn);

        cmd.Parameters.AddWithValue(productId);
        cmd.Parameters.AddWithValue(quantity);
        cmd.Parameters.AddWithValue(total);

        var orderId = (int)(await cmd.ExecuteScalarAsync() ?? 0);

        // Increment custom metric
        ObservabilityMetrics.OrdersCreatedTotal.Inc();

        return orderId;
    }

    public async Task<Order?> GetByIdAsync(int id)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            "SELECT id, product_id, quantity, total, created_at FROM orders WHERE id = $1",
            conn);

        cmd.Parameters.AddWithValue(id);

        await using var reader = await cmd.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new Order
            {
                Id = reader.GetInt32(0),
                ProductId = reader.GetInt32(1),
                Quantity = reader.GetInt32(2),
                Total = reader.GetDecimal(3),
                CreatedAt = reader.GetDateTime(4)
            };
        }

        return null;
    }
}
