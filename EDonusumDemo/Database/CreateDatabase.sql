IF DB_ID(N'EDonusumDemo') IS NULL
BEGIN
    CREATE DATABASE EDonusumDemo;
END
GO

USE EDonusumDemo;
GO

CREATE TABLE dbo.Customers
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Customers PRIMARY KEY,
    Title NVARCHAR(250) NOT NULL,
    TaxOffice NVARCHAR(100) NULL,
    TaxNumber NVARCHAR(11) NOT NULL,
    Address NVARCHAR(500) NULL,
    Phone NVARCHAR(50) NULL,
    Email NVARCHAR(150) NULL,
    IsEInvoicePayer BIT NOT NULL CONSTRAINT DF_Customers_IsEInvoicePayer DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Customers_CreatedAt DEFAULT SYSUTCDATETIME()
);
GO

CREATE TABLE dbo.Products
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Products PRIMARY KEY,
    Code NVARCHAR(50) NOT NULL,
    Name NVARCHAR(250) NOT NULL,
    Unit NVARCHAR(20) NOT NULL,
    VatRate DECIMAL(5, 2) NOT NULL,
    UnitPrice DECIMAL(18, 2) NOT NULL,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Products_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_Products_Code UNIQUE (Code)
);
GO

CREATE TABLE dbo.Invoices
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Invoices PRIMARY KEY,
    Number NVARCHAR(50) NOT NULL,
    IssueDate DATE NOT NULL,
    SupplierId UNIQUEIDENTIFIER NOT NULL,
    CustomerId UNIQUEIDENTIFIER NOT NULL,
    DocumentKind NVARCHAR(30) NOT NULL,
    Description NVARCHAR(500) NULL,
    Subtotal DECIMAL(18, 2) NOT NULL,
    VatTotal DECIMAL(18, 2) NOT NULL,
    GrandTotal DECIMAL(18, 2) NOT NULL,
    XmlFilePath NVARCHAR(500) NULL,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Invoices_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Invoices_Supplier FOREIGN KEY (SupplierId) REFERENCES dbo.Customers(Id),
    CONSTRAINT FK_Invoices_Customer FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(Id),
    CONSTRAINT CK_Invoices_DocumentKind CHECK (DocumentKind IN (N'EInvoice', N'EArchiveInvoice')),
    CONSTRAINT UQ_Invoices_Number_Kind UNIQUE (Number, DocumentKind)
);
GO

CREATE TABLE dbo.InvoiceLines
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_InvoiceLines PRIMARY KEY,
    InvoiceId UNIQUEIDENTIFIER NOT NULL,
    ProductId UNIQUEIDENTIFIER NOT NULL,
    ProductCode NVARCHAR(50) NOT NULL,
    ProductName NVARCHAR(250) NOT NULL,
    Unit NVARCHAR(20) NOT NULL,
    Quantity DECIMAL(18, 4) NOT NULL,
    UnitPrice DECIMAL(18, 2) NOT NULL,
    VatRate DECIMAL(5, 2) NOT NULL,
    LineExtensionAmount DECIMAL(18, 2) NOT NULL,
    VatAmount DECIMAL(18, 2) NOT NULL,
    TotalAmount DECIMAL(18, 2) NOT NULL,
    CONSTRAINT FK_InvoiceLines_Invoice FOREIGN KEY (InvoiceId) REFERENCES dbo.Invoices(Id) ON DELETE CASCADE,
    CONSTRAINT FK_InvoiceLines_Product FOREIGN KEY (ProductId) REFERENCES dbo.Products(Id)
);
GO

CREATE TABLE dbo.Waybills
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Waybills PRIMARY KEY,
    Number NVARCHAR(50) NOT NULL,
    IssueDate DATE NOT NULL,
    ShipmentDate DATE NOT NULL,
    SenderId UNIQUEIDENTIFIER NOT NULL,
    ReceiverId UNIQUEIDENTIFIER NOT NULL,
    CarrierInfo NVARCHAR(250) NULL,
    XmlFilePath NVARCHAR(500) NULL,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Waybills_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Waybills_Sender FOREIGN KEY (SenderId) REFERENCES dbo.Customers(Id),
    CONSTRAINT FK_Waybills_Receiver FOREIGN KEY (ReceiverId) REFERENCES dbo.Customers(Id),
    CONSTRAINT UQ_Waybills_Number UNIQUE (Number)
);
GO

CREATE TABLE dbo.WaybillLines
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_WaybillLines PRIMARY KEY,
    WaybillId UNIQUEIDENTIFIER NOT NULL,
    ProductId UNIQUEIDENTIFIER NOT NULL,
    ProductCode NVARCHAR(50) NOT NULL,
    ProductName NVARCHAR(250) NOT NULL,
    Unit NVARCHAR(20) NOT NULL,
    Quantity DECIMAL(18, 4) NOT NULL,
    CONSTRAINT FK_WaybillLines_Waybill FOREIGN KEY (WaybillId) REFERENCES dbo.Waybills(Id) ON DELETE CASCADE,
    CONSTRAINT FK_WaybillLines_Product FOREIGN KEY (ProductId) REFERENCES dbo.Products(Id)
);
GO

CREATE INDEX IX_Invoices_IssueDate ON dbo.Invoices(IssueDate);
CREATE INDEX IX_Invoices_CustomerId ON dbo.Invoices(CustomerId);
CREATE INDEX IX_Waybills_IssueDate ON dbo.Waybills(IssueDate);
CREATE INDEX IX_Waybills_ReceiverId ON dbo.Waybills(ReceiverId);
GO

INSERT INTO dbo.Customers (Id, Title, TaxOffice, TaxNumber, Address, Phone, Email, IsEInvoicePayer)
VALUES
('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', N'HETABİL YAZILIM BİLİŞİM VE DANIŞMANLIK LTD. ŞTİ.', N'Yenimahalle', N'1234567890', N'Teknopark Ankara, İvedik OSB Mah. 2224. Cad No:1/116 Yenimahalle/ANKARA', N'+90 312 911 12 57', N'info@hetabil.com', 1),
(NEWID(), N'Demo Müşteri A.Ş.', N'Çankaya', N'9876543210', N'Mustafa Kemal Mah. Ankara', N'+90 312 000 00 00', N'muhasebe@demo.com', 1);
GO

INSERT INTO dbo.Products (Id, Code, Name, Unit, VatRate, UnitPrice)
VALUES
(NEWID(), N'SRV-001', N'Yazılım Geliştirme Hizmeti', N'SAAT', 20, 1250),
(NEWID(), N'LIS-001', N'Demo Lisans', N'ADET', 20, 5000),
(NEWID(), N'DAN-001', N'Danışmanlık', N'GÜN', 20, 3500);
GO
