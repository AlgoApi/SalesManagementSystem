using Microsoft.Data.SqlClient;
using SalesManagementSystem.Models;

namespace SalesManagementSystem.Data;

public sealed class SalesRepository
{
    private readonly string _connectionString;

    public SalesRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public Task InitializeAsync() => new DatabaseInitializer(_connectionString).InitializeAsync();

    public async Task<DashboardSummary> GetDashboardSummaryAsync()
    {
        const string sql = """
            SELECT
                (SELECT COUNT(*) FROM dbo.Products WHERE IsActive = 1) AS ProductCount,
                (SELECT COUNT(*) FROM dbo.Customers) AS CustomerCount,
                (SELECT COUNT(*) FROM dbo.SalesOrders) AS OrderCount,
                (SELECT COALESCE(SUM(TotalAmount), 0) FROM dbo.SalesOrders) AS Revenue,
                (SELECT COUNT(*) FROM dbo.Products WHERE IsActive = 1 AND StockQuantity <= 5) AS LowStockCount;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return new DashboardSummary();
        }

        return new DashboardSummary
        {
            ProductCount = reader.GetInt32(0),
            CustomerCount = reader.GetInt32(1),
            OrderCount = reader.GetInt32(2),
            Revenue = reader.GetDecimal(3),
            LowStockCount = reader.GetInt32(4)
        };
    }

    public async Task<IReadOnlyList<Product>> GetProductsAsync()
    {
        const string sql = """
            SELECT Id, Sku, Name, Category, UnitPrice, StockQuantity, IsActive
            FROM dbo.Products
            ORDER BY IsActive DESC, Name;
            """;

        var result = new List<Product>();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            result.Add(new Product
            {
                Id = reader.GetInt32(0),
                Sku = reader.GetString(1),
                Name = reader.GetString(2),
                Category = reader.GetString(3),
                UnitPrice = reader.GetDecimal(4),
                StockQuantity = reader.GetInt32(5),
                IsActive = reader.GetBoolean(6)
            });
        }

        return result;
    }

    public async Task<int> SaveProductAsync(Product product)
    {
        ValidateProduct(product);

        const string insertSql = """
            INSERT INTO dbo.Products (Sku, Name, Category, UnitPrice, StockQuantity, IsActive)
            OUTPUT INSERTED.Id
            VALUES (@Sku, @Name, @Category, @UnitPrice, @StockQuantity, @IsActive);
            """;

        const string updateSql = """
            UPDATE dbo.Products
            SET Sku = @Sku,
                Name = @Name,
                Category = @Category,
                UnitPrice = @UnitPrice,
                StockQuantity = @StockQuantity,
                IsActive = @IsActive
            WHERE Id = @Id;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(product.Id == 0 ? insertSql : updateSql, connection);
        AddProductParameters(command, product);

        if (product.Id == 0)
        {
            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        await command.ExecuteNonQueryAsync();
        return product.Id;
    }

    public async Task<IReadOnlyList<Customer>> GetCustomersAsync()
    {
        const string sql = """
            SELECT Id, CompanyName, ContactName, Phone, Email, Address
            FROM dbo.Customers
            ORDER BY CompanyName;
            """;

        var result = new List<Customer>();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            result.Add(new Customer
            {
                Id = reader.GetInt32(0),
                CompanyName = reader.GetString(1),
                ContactName = GetNullableString(reader, 2),
                Phone = GetNullableString(reader, 3),
                Email = GetNullableString(reader, 4),
                Address = GetNullableString(reader, 5)
            });
        }

        return result;
    }

    public async Task<int> SaveCustomerAsync(Customer customer)
    {
        if (string.IsNullOrWhiteSpace(customer.CompanyName))
        {
            throw new InvalidOperationException("Укажите наименование клиента.");
        }

        const string insertSql = """
            INSERT INTO dbo.Customers (CompanyName, ContactName, Phone, Email, Address)
            OUTPUT INSERTED.Id
            VALUES (@CompanyName, @ContactName, @Phone, @Email, @Address);
            """;

        const string updateSql = """
            UPDATE dbo.Customers
            SET CompanyName = @CompanyName,
                ContactName = @ContactName,
                Phone = @Phone,
                Email = @Email,
                Address = @Address
            WHERE Id = @Id;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(customer.Id == 0 ? insertSql : updateSql, connection);
        command.Parameters.AddWithValue("@Id", customer.Id);
        command.Parameters.AddWithValue("@CompanyName", customer.CompanyName.Trim());
        command.Parameters.AddWithValue("@ContactName", DbValue(customer.ContactName));
        command.Parameters.AddWithValue("@Phone", DbValue(customer.Phone));
        command.Parameters.AddWithValue("@Email", DbValue(customer.Email));
        command.Parameters.AddWithValue("@Address", DbValue(customer.Address));

        if (customer.Id == 0)
        {
            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        await command.ExecuteNonQueryAsync();
        return customer.Id;
    }

    public async Task<IReadOnlyList<SalesOrder>> GetOrdersAsync()
    {
        const string sql = """
            SELECT o.Id, o.OrderNumber, o.CustomerId, c.CompanyName, o.OrderDate, o.Status, o.TotalAmount, o.Comment
            FROM dbo.SalesOrders o
            INNER JOIN dbo.Customers c ON c.Id = o.CustomerId
            ORDER BY o.OrderDate DESC, o.Id DESC;
            """;

        var result = new List<SalesOrder>();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            result.Add(new SalesOrder
            {
                Id = reader.GetInt32(0),
                OrderNumber = reader.GetString(1),
                CustomerId = reader.GetInt32(2),
                CustomerName = reader.GetString(3),
                OrderDate = reader.GetDateTime(4),
                Status = reader.GetString(5),
                TotalAmount = reader.GetDecimal(6),
                Comment = GetNullableString(reader, 7)
            });
        }

        return result;
    }

    public async Task<IReadOnlyList<SalesOrderItem>> GetOrderItemsAsync(int orderId)
    {
        const string sql = """
            SELECT i.Id, i.SalesOrderId, i.ProductId, p.Name, i.Quantity, i.UnitPrice
            FROM dbo.SalesOrderItems i
            INNER JOIN dbo.Products p ON p.Id = i.ProductId
            WHERE i.SalesOrderId = @OrderId
            ORDER BY i.Id;
            """;

        var result = new List<SalesOrderItem>();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@OrderId", orderId);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            result.Add(new SalesOrderItem
            {
                Id = reader.GetInt32(0),
                SalesOrderId = reader.GetInt32(1),
                ProductId = reader.GetInt32(2),
                ProductName = reader.GetString(3),
                Quantity = reader.GetInt32(4),
                UnitPrice = reader.GetDecimal(5)
            });
        }

        return result;
    }

    public async Task<int> CreateOrderAsync(int customerId, IEnumerable<OrderDraftItem> items, string comment)
    {
        var lines = items.ToList();
        if (customerId <= 0)
        {
            throw new InvalidOperationException("Выберите клиента для заказа.");
        }

        if (lines.Count == 0)
        {
            throw new InvalidOperationException("Добавьте хотя бы одну позицию заказа.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            foreach (var item in lines.GroupBy(item => item.ProductId)
                         .Select(group => new { ProductId = group.Key, Quantity = group.Sum(item => item.Quantity) }))
            {
                await EnsureStockAsync(connection, (SqlTransaction)transaction, item.ProductId, item.Quantity);
            }

            var total = lines.Sum(item => item.LineTotal);
            var orderNumber = $"SO-{DateTime.Now:yyyyMMdd-HHmmssfff}";

            await using var orderCommand = new SqlCommand("""
                INSERT INTO dbo.SalesOrders (OrderNumber, CustomerId, OrderDate, Status, TotalAmount, Comment)
                OUTPUT INSERTED.Id
                VALUES (@OrderNumber, @CustomerId, SYSDATETIME(), N'Новый', @TotalAmount, @Comment);
                """, connection, (SqlTransaction)transaction);
            orderCommand.Parameters.AddWithValue("@OrderNumber", orderNumber);
            orderCommand.Parameters.AddWithValue("@CustomerId", customerId);
            orderCommand.Parameters.AddWithValue("@TotalAmount", total);
            orderCommand.Parameters.AddWithValue("@Comment", DbValue(comment));
            var orderId = Convert.ToInt32(await orderCommand.ExecuteScalarAsync());

            foreach (var item in lines)
            {
                await using var itemCommand = new SqlCommand("""
                    INSERT INTO dbo.SalesOrderItems (SalesOrderId, ProductId, Quantity, UnitPrice)
                    VALUES (@SalesOrderId, @ProductId, @Quantity, @UnitPrice);

                    UPDATE dbo.Products
                    SET StockQuantity = StockQuantity - @Quantity
                    WHERE Id = @ProductId;
                    """, connection, (SqlTransaction)transaction);
                itemCommand.Parameters.AddWithValue("@SalesOrderId", orderId);
                itemCommand.Parameters.AddWithValue("@ProductId", item.ProductId);
                itemCommand.Parameters.AddWithValue("@Quantity", item.Quantity);
                itemCommand.Parameters.AddWithValue("@UnitPrice", item.UnitPrice);
                await itemCommand.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
            return orderId;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static async Task EnsureStockAsync(SqlConnection connection, SqlTransaction transaction, int productId, int quantity)
    {
        await using var command = new SqlCommand("""
            SELECT Name, StockQuantity, IsActive
            FROM dbo.Products WITH (UPDLOCK, HOLDLOCK)
            WHERE Id = @ProductId;
            """, connection, transaction);
        command.Parameters.AddWithValue("@ProductId", productId);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            throw new InvalidOperationException("Товар не найден.");
        }

        var name = reader.GetString(0);
        var stock = reader.GetInt32(1);
        var isActive = reader.GetBoolean(2);
        await reader.CloseAsync();

        if (!isActive)
        {
            throw new InvalidOperationException($"Товар \"{name}\" не активен.");
        }

        if (stock < quantity)
        {
            throw new InvalidOperationException($"Недостаточно товара \"{name}\" на складе. Доступно: {stock}.");
        }
    }

    private static void AddProductParameters(SqlCommand command, Product product)
    {
        command.Parameters.AddWithValue("@Id", product.Id);
        command.Parameters.AddWithValue("@Sku", product.Sku.Trim());
        command.Parameters.AddWithValue("@Name", product.Name.Trim());
        command.Parameters.AddWithValue("@Category", string.IsNullOrWhiteSpace(product.Category) ? "Общее" : product.Category.Trim());
        command.Parameters.AddWithValue("@UnitPrice", product.UnitPrice);
        command.Parameters.AddWithValue("@StockQuantity", product.StockQuantity);
        command.Parameters.AddWithValue("@IsActive", product.IsActive);
    }

    private static void ValidateProduct(Product product)
    {
        if (string.IsNullOrWhiteSpace(product.Sku))
        {
            throw new InvalidOperationException("Укажите артикул товара.");
        }

        if (string.IsNullOrWhiteSpace(product.Name))
        {
            throw new InvalidOperationException("Укажите наименование товара.");
        }

        if (product.UnitPrice < 0)
        {
            throw new InvalidOperationException("Цена не может быть отрицательной.");
        }

        if (product.StockQuantity < 0)
        {
            throw new InvalidOperationException("Остаток не может быть отрицательным.");
        }
    }

    private static object DbValue(string? value) => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();

    private static string GetNullableString(SqlDataReader reader, int index) => reader.IsDBNull(index) ? string.Empty : reader.GetString(index);
}
