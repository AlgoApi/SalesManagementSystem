IF DB_ID(N'SalesManagementDb') IS NULL
BEGIN
    CREATE DATABASE [SalesManagementDb];
END
GO

USE [SalesManagementDb];
GO

IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Products
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Products PRIMARY KEY,
        Sku NVARCHAR(40) NOT NULL CONSTRAINT UQ_Products_Sku UNIQUE,
        Name NVARCHAR(160) NOT NULL,
        Category NVARCHAR(100) NOT NULL CONSTRAINT DF_Products_Category DEFAULT N'Общее',
        UnitPrice DECIMAL(18,2) NOT NULL CONSTRAINT CK_Products_UnitPrice CHECK (UnitPrice >= 0),
        StockQuantity INT NOT NULL CONSTRAINT CK_Products_StockQuantity CHECK (StockQuantity >= 0),
        IsActive BIT NOT NULL CONSTRAINT DF_Products_IsActive DEFAULT 1,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Products_CreatedAt DEFAULT SYSUTCDATETIME()
    );
END
GO

IF OBJECT_ID(N'dbo.Customers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Customers
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Customers PRIMARY KEY,
        CompanyName NVARCHAR(180) NOT NULL,
        ContactName NVARCHAR(140) NULL,
        Phone NVARCHAR(40) NULL,
        Email NVARCHAR(140) NULL,
        Address NVARCHAR(260) NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Customers_CreatedAt DEFAULT SYSUTCDATETIME()
    );
END
GO

IF OBJECT_ID(N'dbo.SalesOrders', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SalesOrders
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SalesOrders PRIMARY KEY,
        OrderNumber NVARCHAR(40) NOT NULL CONSTRAINT UQ_SalesOrders_OrderNumber UNIQUE,
        CustomerId INT NOT NULL,
        OrderDate DATETIME2(0) NOT NULL CONSTRAINT DF_SalesOrders_OrderDate DEFAULT SYSUTCDATETIME(),
        Status NVARCHAR(40) NOT NULL CONSTRAINT DF_SalesOrders_Status DEFAULT N'Новый',
        TotalAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_SalesOrders_TotalAmount DEFAULT 0,
        Comment NVARCHAR(500) NULL,
        CONSTRAINT FK_SalesOrders_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(Id)
    );
END
GO

IF OBJECT_ID(N'dbo.SalesOrderItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SalesOrderItems
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SalesOrderItems PRIMARY KEY,
        SalesOrderId INT NOT NULL,
        ProductId INT NOT NULL,
        Quantity INT NOT NULL CONSTRAINT CK_SalesOrderItems_Quantity CHECK (Quantity > 0),
        UnitPrice DECIMAL(18,2) NOT NULL CONSTRAINT CK_SalesOrderItems_UnitPrice CHECK (UnitPrice >= 0),
        LineTotal AS CONVERT(DECIMAL(18,2), Quantity * UnitPrice) PERSISTED,
        CONSTRAINT FK_SalesOrderItems_SalesOrders FOREIGN KEY (SalesOrderId) REFERENCES dbo.SalesOrders(Id) ON DELETE CASCADE,
        CONSTRAINT FK_SalesOrderItems_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products(Id)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SalesOrders_OrderDate' AND object_id = OBJECT_ID(N'dbo.SalesOrders'))
BEGIN
    CREATE INDEX IX_SalesOrders_OrderDate ON dbo.SalesOrders(OrderDate DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SalesOrderItems_ProductId' AND object_id = OBJECT_ID(N'dbo.SalesOrderItems'))
BEGIN
    CREATE INDEX IX_SalesOrderItems_ProductId ON dbo.SalesOrderItems(ProductId);
END
GO
