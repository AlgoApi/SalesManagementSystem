USE [SalesManagementDb];
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Products)
BEGIN
    INSERT INTO dbo.Products (Sku, Name, Category, UnitPrice, StockQuantity)
    VALUES
        (N'NB-001', N'Ноутбук 15"', N'Компьютеры', 78000.00, 12),
        (N'MN-024', N'Монитор 24"', N'Периферия', 15900.00, 30),
        (N'KB-110', N'Клавиатура офисная', N'Периферия', 2100.00, 55),
        (N'PR-200', N'Принтер лазерный', N'Оргтехника', 26500.00, 8);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Customers)
BEGIN
    INSERT INTO dbo.Customers (CompanyName, ContactName, Phone, Email, Address)
    VALUES
        (N'ООО Альфа-Снаб', N'Ирина Волкова', N'+7 343 200-10-01', N'volkova@alpha.example', N'Екатеринбург, ул. Малышева, 51'),
        (N'ИП Кузнецов', N'Павел Кузнецов', N'+7 343 300-22-18', N'pk@example.ru', N'Екатеринбург, пр. Ленина, 10');
END
GO
