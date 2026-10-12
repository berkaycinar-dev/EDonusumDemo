using System.IO;
using Microsoft.Data.SqlClient;
using EDonusumDemo.Domain;

namespace EDonusumDemo.Infrastructure;

public sealed class DemoDataStore
{
    private const string DefaultConnectionString =
        "Server=localhost\\SQLEXPRESS;Database=EDonusumDemo;Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True;";

    public DemoDataStore()
    {
        DataDirectory = Path.Combine(AppContext.BaseDirectory, "Data");
        XmlDirectory = Path.Combine(ResolveProjectRoot(), "XmlOrnekleri");
        ConnectionStringPath = Path.Combine(DataDirectory, "connection-string.txt");
        DataFilePath = ConnectionStringPath;
    }

    public string DataDirectory { get; }
    public string XmlDirectory { get; }
    public string ConnectionStringPath { get; }
    public string DataFilePath { get; }
    public StoreData Data { get; private set; } = new();

    public void Load()
    {
        Directory.CreateDirectory(DataDirectory);
        Directory.CreateDirectory(XmlDirectory);
        EnsureDatabase();

        Data = ReadData();

        if (Data.Customers.Count == 0 || Data.Products.Count == 0)
        {
            Data = CreateSeedData();
            Save();
            Data = ReadData();
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(DataDirectory);
        Directory.CreateDirectory(XmlDirectory);
        EnsureDatabase();

        using var connection = new SqlConnection(GetConnectionString());
        connection.Open();
        using var transaction = connection.BeginTransaction();

        foreach (var customer in Data.Customers)
        {
            Execute(connection, transaction, """
                IF EXISTS (SELECT 1 FROM dbo.Customers WHERE Id = @Id)
                    UPDATE dbo.Customers
                    SET Title = @Title,
                        TaxOffice = @TaxOffice,
                        TaxNumber = @TaxNumber,
                        Address = @Address,
                        Phone = @Phone,
                        Email = @Email,
                        IsEInvoicePayer = @IsEInvoicePayer
                    WHERE Id = @Id;
                ELSE
                    INSERT INTO dbo.Customers (Id, Title, TaxOffice, TaxNumber, Address, Phone, Email, IsEInvoicePayer)
                    VALUES (@Id, @Title, @TaxOffice, @TaxNumber, @Address, @Phone, @Email, @IsEInvoicePayer);
                """,
                ("@Id", customer.Id),
                ("@Title", customer.Title),
                ("@TaxOffice", customer.TaxOffice),
                ("@TaxNumber", customer.TaxNumber),
                ("@Address", customer.Address),
                ("@Phone", customer.Phone),
                ("@Email", customer.Email),
                ("@IsEInvoicePayer", customer.IsEInvoicePayer));
        }

        foreach (var product in Data.Products)
        {
            Execute(connection, transaction, """
                IF EXISTS (SELECT 1 FROM dbo.Products WHERE Id = @Id)
                    UPDATE dbo.Products
                    SET Code = @Code,
                        Name = @Name,
                        Unit = @Unit,
                        VatRate = @VatRate,
                        UnitPrice = @UnitPrice
                    WHERE Id = @Id;
                ELSE
                    INSERT INTO dbo.Products (Id, Code, Name, Unit, VatRate, UnitPrice)
                    VALUES (@Id, @Code, @Name, @Unit, @VatRate, @UnitPrice);
                """,
                ("@Id", product.Id),
                ("@Code", product.Code),
                ("@Name", product.Name),
                ("@Unit", product.Unit),
                ("@VatRate", product.VatRate),
                ("@UnitPrice", product.UnitPrice));
        }

        foreach (var invoice in Data.Invoices)
        {
            Execute(connection, transaction, """
                IF EXISTS (SELECT 1 FROM dbo.Invoices WHERE Id = @Id)
                    UPDATE dbo.Invoices
                    SET Number = @Number,
                        IssueDate = @IssueDate,
                        SupplierId = @SupplierId,
                        CustomerId = @CustomerId,
                        DocumentKind = @DocumentKind,
                        Description = @Description,
                        Subtotal = @Subtotal,
                        VatTotal = @VatTotal,
                        GrandTotal = @GrandTotal,
                        XmlFilePath = @XmlFilePath
                    WHERE Id = @Id;
                ELSE
                    INSERT INTO dbo.Invoices
                    (Id, Number, IssueDate, SupplierId, CustomerId, DocumentKind, Description, Subtotal, VatTotal, GrandTotal, XmlFilePath)
                    VALUES
                    (@Id, @Number, @IssueDate, @SupplierId, @CustomerId, @DocumentKind, @Description, @Subtotal, @VatTotal, @GrandTotal, @XmlFilePath);
                """,
                ("@Id", invoice.Id),
                ("@Number", invoice.Number),
                ("@IssueDate", invoice.IssueDate.Date),
                ("@SupplierId", invoice.SupplierId),
                ("@CustomerId", invoice.CustomerId),
                ("@DocumentKind", invoice.Kind.ToString()),
                ("@Description", invoice.Description),
                ("@Subtotal", invoice.Subtotal),
                ("@VatTotal", invoice.VatTotal),
                ("@GrandTotal", invoice.GrandTotal),
                ("@XmlFilePath", invoice.XmlFilePath));

            Execute(connection, transaction, "DELETE FROM dbo.InvoiceLines WHERE InvoiceId = @InvoiceId;", ("@InvoiceId", invoice.Id));
            foreach (var line in invoice.Lines)
            {
                Execute(connection, transaction, """
                    INSERT INTO dbo.InvoiceLines
                    (Id, InvoiceId, ProductId, ProductCode, ProductName, Unit, Quantity, UnitPrice, VatRate, LineExtensionAmount, VatAmount, TotalAmount)
                    VALUES
                    (@Id, @InvoiceId, @ProductId, @ProductCode, @ProductName, @Unit, @Quantity, @UnitPrice, @VatRate, @LineExtensionAmount, @VatAmount, @TotalAmount);
                    """,
                    ("@Id", Guid.NewGuid()),
                    ("@InvoiceId", invoice.Id),
                    ("@ProductId", line.ProductId),
                    ("@ProductCode", line.ProductCode),
                    ("@ProductName", line.ProductName),
                    ("@Unit", line.Unit),
                    ("@Quantity", line.Quantity),
                    ("@UnitPrice", line.UnitPrice),
                    ("@VatRate", line.VatRate),
                    ("@LineExtensionAmount", line.LineExtensionAmount),
                    ("@VatAmount", line.VatAmount),
                    ("@TotalAmount", line.TotalAmount));
            }
        }

        foreach (var waybill in Data.Waybills)
        {
            Execute(connection, transaction, """
                IF EXISTS (SELECT 1 FROM dbo.Waybills WHERE Id = @Id)
                    UPDATE dbo.Waybills
                    SET Number = @Number,
                        IssueDate = @IssueDate,
                        ShipmentDate = @ShipmentDate,
                        SenderId = @SenderId,
                        ReceiverId = @ReceiverId,
                        CarrierInfo = @CarrierInfo,
                        XmlFilePath = @XmlFilePath
                    WHERE Id = @Id;
                ELSE
                    INSERT INTO dbo.Waybills
                    (Id, Number, IssueDate, ShipmentDate, SenderId, ReceiverId, CarrierInfo, XmlFilePath)
                    VALUES
                    (@Id, @Number, @IssueDate, @ShipmentDate, @SenderId, @ReceiverId, @CarrierInfo, @XmlFilePath);
                """,
                ("@Id", waybill.Id),
                ("@Number", waybill.Number),
                ("@IssueDate", waybill.IssueDate.Date),
                ("@ShipmentDate", waybill.ShipmentDate.Date),
                ("@SenderId", waybill.SenderId),
                ("@ReceiverId", waybill.ReceiverId),
                ("@CarrierInfo", waybill.CarrierInfo),
                ("@XmlFilePath", waybill.XmlFilePath));

            Execute(connection, transaction, "DELETE FROM dbo.WaybillLines WHERE WaybillId = @WaybillId;", ("@WaybillId", waybill.Id));
            foreach (var line in waybill.Lines)
            {
                Execute(connection, transaction, """
                    INSERT INTO dbo.WaybillLines
                    (Id, WaybillId, ProductId, ProductCode, ProductName, Unit, Quantity)
                    VALUES
                    (@Id, @WaybillId, @ProductId, @ProductCode, @ProductName, @Unit, @Quantity);
                    """,
                    ("@Id", Guid.NewGuid()),
                    ("@WaybillId", waybill.Id),
                    ("@ProductId", line.ProductId),
                    ("@ProductCode", line.ProductCode),
                    ("@ProductName", line.ProductName),
                    ("@Unit", line.Unit),
                    ("@Quantity", line.Quantity));
            }
        }

        transaction.Commit();
    }

    public void DeleteInvoice(Guid id)
    {
        EnsureDatabase();

        using var connection = new SqlConnection(GetConnectionString());
        connection.Open();
        Execute(connection, null, "DELETE FROM dbo.Invoices WHERE Id = @Id;", ("@Id", id));
    }

    public void DeleteWaybill(Guid id)
    {
        EnsureDatabase();

        using var connection = new SqlConnection(GetConnectionString());
        connection.Open();
        Execute(connection, null, "DELETE FROM dbo.Waybills WHERE Id = @Id;", ("@Id", id));
    }

    public string SaveXml(string documentType, string documentNumber, string xml)
    {
        Directory.CreateDirectory(XmlDirectory);
        var safeType = MakeSafeFileName(documentType);
        var safeNumber = MakeSafeFileName(documentNumber);
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var path = Path.Combine(XmlDirectory, $"{safeType}_{safeNumber}_{timestamp}.xml");
        File.WriteAllText(path, xml);
        return path;
    }

    private StoreData ReadData()
    {
        var data = new StoreData();
        using var connection = new SqlConnection(GetConnectionString());
        connection.Open();

        using (var command = new SqlCommand("SELECT Id, Title, TaxOffice, TaxNumber, Address, Phone, Email, IsEInvoicePayer FROM dbo.Customers ORDER BY Title;", connection))
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                data.Customers.Add(new Customer
                {
                    Id = reader.GetGuid(0),
                    Title = ReadString(reader, 1),
                    TaxOffice = ReadString(reader, 2),
                    TaxNumber = ReadString(reader, 3),
                    Address = ReadString(reader, 4),
                    Phone = ReadString(reader, 5),
                    Email = ReadString(reader, 6),
                    IsEInvoicePayer = reader.GetBoolean(7)
                });
            }
        }

        using (var command = new SqlCommand("SELECT Id, Code, Name, Unit, VatRate, UnitPrice FROM dbo.Products ORDER BY Code;", connection))
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                data.Products.Add(new Product
                {
                    Id = reader.GetGuid(0),
                    Code = ReadString(reader, 1),
                    Name = ReadString(reader, 2),
                    Unit = ReadString(reader, 3),
                    VatRate = reader.GetDecimal(4),
                    UnitPrice = reader.GetDecimal(5)
                });
            }
        }

        var invoicesById = new Dictionary<Guid, InvoiceDocument>();
        using (var command = new SqlCommand("""
            SELECT Id, Number, IssueDate, SupplierId, CustomerId, DocumentKind, Description, XmlFilePath
            FROM dbo.Invoices
            ORDER BY IssueDate DESC, Number;
            """, connection))
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                var invoice = new InvoiceDocument
                {
                    Id = reader.GetGuid(0),
                    Number = ReadString(reader, 1),
                    IssueDate = reader.GetDateTime(2),
                    SupplierId = reader.GetGuid(3),
                    CustomerId = reader.GetGuid(4),
                    Kind = Enum.Parse<DocumentKind>(ReadString(reader, 5)),
                    Description = ReadString(reader, 6),
                    XmlFilePath = ReadNullableString(reader, 7)
                };
                invoicesById[invoice.Id] = invoice;
                data.Invoices.Add(invoice);
            }
        }

        using (var command = new SqlCommand("""
            SELECT InvoiceId, ProductId, ProductCode, ProductName, Unit, Quantity, UnitPrice, VatRate
            FROM dbo.InvoiceLines
            ORDER BY Id;
            """, connection))
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                var invoiceId = reader.GetGuid(0);
                if (!invoicesById.TryGetValue(invoiceId, out var invoice))
                {
                    continue;
                }

                invoice.Lines.Add(new DocumentLine
                {
                    ProductId = reader.GetGuid(1),
                    ProductCode = ReadString(reader, 2),
                    ProductName = ReadString(reader, 3),
                    Unit = ReadString(reader, 4),
                    Quantity = reader.GetDecimal(5),
                    UnitPrice = reader.GetDecimal(6),
                    VatRate = reader.GetDecimal(7)
                });
            }
        }

        var waybillsById = new Dictionary<Guid, WaybillDocument>();
        using (var command = new SqlCommand("""
            SELECT Id, Number, IssueDate, ShipmentDate, SenderId, ReceiverId, CarrierInfo, XmlFilePath
            FROM dbo.Waybills
            ORDER BY IssueDate DESC, Number;
            """, connection))
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                var waybill = new WaybillDocument
                {
                    Id = reader.GetGuid(0),
                    Number = ReadString(reader, 1),
                    IssueDate = reader.GetDateTime(2),
                    ShipmentDate = reader.GetDateTime(3),
                    SenderId = reader.GetGuid(4),
                    ReceiverId = reader.GetGuid(5),
                    CarrierInfo = ReadString(reader, 6),
                    XmlFilePath = ReadNullableString(reader, 7)
                };
                waybillsById[waybill.Id] = waybill;
                data.Waybills.Add(waybill);
            }
        }

        using (var command = new SqlCommand("""
            SELECT WaybillId, ProductId, ProductCode, ProductName, Unit, Quantity
            FROM dbo.WaybillLines
            ORDER BY Id;
            """, connection))
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                var waybillId = reader.GetGuid(0);
                if (!waybillsById.TryGetValue(waybillId, out var waybill))
                {
                    continue;
                }

                waybill.Lines.Add(new DocumentLine
                {
                    ProductId = reader.GetGuid(1),
                    ProductCode = ReadString(reader, 2),
                    ProductName = ReadString(reader, 3),
                    Unit = ReadString(reader, 4),
                    Quantity = reader.GetDecimal(5)
                });
            }
        }

        return data;
    }

    private void EnsureDatabase()
    {
        var builder = new SqlConnectionStringBuilder(GetConnectionString());
        var databaseName = string.IsNullOrWhiteSpace(builder.InitialCatalog) ? "EDonusumDemo" : builder.InitialCatalog;

        var masterBuilder = new SqlConnectionStringBuilder(builder.ConnectionString) { InitialCatalog = "master" };
        using (var connection = new SqlConnection(masterBuilder.ConnectionString))
        {
            connection.Open();
            Execute(connection, null, """
                IF DB_ID(@DatabaseName) IS NULL
                BEGIN
                    DECLARE @Sql nvarchar(max) = N'CREATE DATABASE ' + QUOTENAME(@DatabaseName);
                    EXEC (@Sql);
                END
                """, ("@DatabaseName", databaseName));
        }

        builder.InitialCatalog = databaseName;
        using var databaseConnection = new SqlConnection(builder.ConnectionString);
        databaseConnection.Open();
        Execute(databaseConnection, null, SchemaSql);
    }

    private string GetConnectionString()
    {
        Directory.CreateDirectory(DataDirectory);
        if (!File.Exists(ConnectionStringPath))
        {
            File.WriteAllText(ConnectionStringPath, DefaultConnectionString);
        }

        var connectionString = File.ReadAllText(ConnectionStringPath).Trim();
        return string.IsNullOrWhiteSpace(connectionString) ? DefaultConnectionString : connectionString;
    }

    private static string ResolveProjectRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "EDonusumDemo.csproj")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return AppContext.BaseDirectory;
    }

    private static string MakeSafeFileName(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var safeValue = new string(value
            .Select(character => invalidChars.Contains(character) ? '_' : character)
            .ToArray());

        return string.IsNullOrWhiteSpace(safeValue) ? "Belge" : safeValue.Trim();
    }

    private static void Execute(SqlConnection connection, SqlTransaction? transaction, string commandText, params (string Name, object? Value)[] parameters)
    {
        using var command = new SqlCommand(commandText, connection, transaction);
        foreach (var parameter in parameters)
        {
            command.Parameters.AddWithValue(parameter.Name, parameter.Value ?? DBNull.Value);
        }

        command.ExecuteNonQuery();
    }

    private static string ReadString(SqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);

    private static string? ReadNullableString(SqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    private static StoreData CreateSeedData()
    {
        var supplier = new Customer
        {
            Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Title = "HETABİL YAZILIM BİLİŞİM VE DANIŞMANLIK LTD. ŞTİ.",
            TaxOffice = "Yenimahalle",
            TaxNumber = "1234567890",
            Address = "Teknopark Ankara, İvedik OSB Mah. 2224. Cad No:1/116 Yenimahalle/ANKARA",
            Phone = "+90 312 911 12 57",
            Email = "info@hetabil.com",
            IsEInvoicePayer = true
        };

        var customer = new Customer
        {
            Title = "Demo Müşteri A.Ş.",
            TaxOffice = "Çankaya",
            TaxNumber = "9876543210",
            Address = "Mustafa Kemal Mah. Ankara",
            Phone = "+90 312 000 00 00",
            Email = "muhasebe@demo.com",
            IsEInvoicePayer = true
        };

        return new StoreData
        {
            Customers = [supplier, customer],
            Products =
            [
                new Product { Code = "SRV-001", Name = "Yazılım Geliştirme Hizmeti", Unit = "SAAT", VatRate = 20, UnitPrice = 1250 },
                new Product { Code = "LIS-001", Name = "Demo Lisans", Unit = "ADET", VatRate = 20, UnitPrice = 5000 },
                new Product { Code = "DAN-001", Name = "Danışmanlık", Unit = "GÜN", VatRate = 20, UnitPrice = 3500 }
            ]
        };
    }

    private const string SchemaSql = """
        IF OBJECT_ID(N'dbo.Customers', N'U') IS NULL
        BEGIN
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
        END;

        IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
        BEGIN
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
        END;

        IF OBJECT_ID(N'dbo.Invoices', N'U') IS NULL
        BEGIN
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
        END;

        IF OBJECT_ID(N'dbo.InvoiceLines', N'U') IS NULL
        BEGIN
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
        END;

        IF OBJECT_ID(N'dbo.Waybills', N'U') IS NULL
        BEGIN
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
        END;

        IF OBJECT_ID(N'dbo.WaybillLines', N'U') IS NULL
        BEGIN
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
        END;

        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Invoices_IssueDate' AND object_id = OBJECT_ID(N'dbo.Invoices'))
            CREATE INDEX IX_Invoices_IssueDate ON dbo.Invoices(IssueDate);

        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Invoices_CustomerId' AND object_id = OBJECT_ID(N'dbo.Invoices'))
            CREATE INDEX IX_Invoices_CustomerId ON dbo.Invoices(CustomerId);

        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Waybills_IssueDate' AND object_id = OBJECT_ID(N'dbo.Waybills'))
            CREATE INDEX IX_Waybills_IssueDate ON dbo.Waybills(IssueDate);

        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Waybills_ReceiverId' AND object_id = OBJECT_ID(N'dbo.Waybills'))
            CREATE INDEX IX_Waybills_ReceiverId ON dbo.Waybills(ReceiverId);
        """;
}
