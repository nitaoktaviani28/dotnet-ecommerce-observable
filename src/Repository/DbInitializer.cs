using Npgsql;

namespace DotnetEcommerce.Repository;

public interface IDbConnectionFactory
{
    NpgsqlConnection CreateConnection();
}

public class DbConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public DbConnectionFactory()
    {
        _connectionString = Environment.GetEnvironmentVariable("DATABASE_DSN")
            ?? "Host=postgres.app.svc.cluster.local;Database=shop;Username=postgres;Password=postgres";
    }

    public NpgsqlConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }
}

public static class DbInitializer
{
    public static async Task InitializeAsync(IDbConnectionFactory factory)
    {
        await using var conn = factory.CreateConnection();
        await conn.OpenAsync();

        // Create tables
        await using (var cmd = new NpgsqlCommand(@"
            CREATE TABLE IF NOT EXISTS products (
                id SERIAL PRIMARY KEY,
                name VARCHAR(255),
                price DECIMAL(10,2)
            )", conn))
        {
            await cmd.ExecuteNonQueryAsync();
        }

        await using (var cmd = new NpgsqlCommand(@"
            CREATE TABLE IF NOT EXISTS orders (
                id SERIAL PRIMARY KEY,
                product_id INTEGER,
                quantity INTEGER,
                total DECIMAL(10,2),
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            )", conn))
        {
            await cmd.ExecuteNonQueryAsync();
        }

        // Seed products
        await using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM products", conn))
        {
            var count = (long)(await cmd.ExecuteScalarAsync() ?? 0L);
            if (count == 0)
            {
                var products = new[]
                {
                    ("Gaming Laptop", 15000000m),
                    ("Wireless Mouse", 300000m),
                    ("Mechanical Keyboard", 800000m),
                    ("4K Monitor", 3500000m)
                };

                foreach (var (name, price) in products)
                {
                    await using var insertCmd = new NpgsqlCommand(
                        "INSERT INTO products (name, price) VALUES ($1, $2)", conn);
                    insertCmd.Parameters.AddWithValue(name);
                    insertCmd.Parameters.AddWithValue(price);
                    await insertCmd.ExecuteNonQueryAsync();
                }
                Console.WriteLine("✅ Sample products inserted");
            }
        }

        Console.WriteLine("✅ Database initialized");
    }
}
