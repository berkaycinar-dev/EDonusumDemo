**🌐 Language / Dil:** English | [Türkçe](README.tr.md)

# E-Dönüşüm Demo — e-Invoice, e-Archive Invoice and e-Waybill (UBL-TR XML) Generator

![C#](https://img.shields.io/badge/C%23-239120?logo=csharp&logoColor=white)
![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet&logoColor=white)
![WPF](https://img.shields.io/badge/WPF-0078D6?logo=windows&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL%20Server-CC2927?logo=microsoftsqlserver&logoColor=white)
![UBL](https://img.shields.io/badge/UBL-2.1%20(TR1.2)-orange)
![Platform](https://img.shields.io/badge/Platform-Windows-lightgrey)

**E-Dönüşüm Demo** ("e-Transformation Demo") is a Windows desktop application that demonstrates the electronic document workflow used in Turkey. You manage customers and stock items, create **e-Invoices (E-Fatura)**, **e-Archive Invoices (E-Arşiv Fatura)** and **e-Waybills (E-İrsaliye)**, store them in SQL Server, and generate a **UBL-TR style XML file** for each document.

The project was developed during an internship at Hetabil Yazılım.

> **Important:** This is a demo. The generated XML follows the UBL 2.1 / TR1.2 structure in a simplified form. The project does **not** digitally sign documents, does **not** send anything to the Revenue Administration (GİB) or to an integrator, and does **not** validate the XML against the official schemas. Do not use the output as a legally valid e-document. See [Known limitations](#10-known-limitations).

---

## Table of Contents

1. [Features](#1-features)
2. [How it works](#2-how-it-works)
3. [The five tabs](#3-the-five-tabs)
4. [Generated XML](#4-generated-xml)
5. [Validation and calculation rules](#5-validation-and-calculation-rules)
6. [Technologies used](#6-technologies-used)
7. [Project structure](#7-project-structure)
8. [Database](#8-database)
9. [Installation and running](#9-installation-and-running)
10. [Known limitations](#10-known-limitations)

---

## 1. Features

- **Customer cards (Cari):** Create and edit customers or suppliers with title, tax office, tax number (VKN/TCKN), address, phone, email and an e-Invoice flag.
- **Stock / materials:** Maintain products and services with code, name, unit, VAT rate and unit price.
- **Invoices:** Build an invoice from line items, choose **E-Fatura** or **E-Arşiv Fatura**, and see the subtotal, VAT total and grand total update live.
- **Waybills:** Build an **E-İrsaliye** with sender, receiver, shipment date, carrier information and line items.
- **UBL-TR XML output:** Every saved document produces an XML file, previewable inside the application.
- **Document list:** All documents in one place with filters by date range, customer and document type.
- **Edit and delete:** Reopen a saved document for editing, or delete it together with its XML file.
- **SQL Server storage:** The database and tables are created automatically on first run, with sample data.
- **Input checks:** Turkish tax number, phone and email formats are checked before saving.

---

## 2. How it works

1. On startup the application reads the SQL Server connection string from a text file (and creates it with a default if missing).
2. It creates the `EDonusumDemo` database and its tables if they do not exist, and loads sample customers and products on an empty database.
3. You create customers, products, invoices and waybills in the tabs.
4. When you press **Kaydet** on an invoice or waybill:
   1. The document is validated.
   2. `UblXmlGenerator` builds the UBL XML.
   3. The XML is written to the `XmlOrnekleri` folder.
   4. The document, its lines and the XML path are saved to SQL Server.
   5. The form is reset for the next document.

Suggested document numbers are generated automatically in the form `FTR20261012-001` for invoices and `IRS20261012-001` for waybills (prefix + date + running count). You can edit them.

---

## 3. The five tabs

The window title is **E-Dönüşüm Demo**. The interface is in Turkish.

| Tab | Purpose | Main controls |
|-----|---------|---------------|
| **Cari Kartlar** | Customers and suppliers | List, form (*Cari Unvan, Vergi Dairesi, Vergi No / TCKN, Adres, Telefon, E-posta*, e-Invoice checkbox), **Yeni**, **Kaydet** |
| **Stok / Malzeme** | Products and services | List, form (*Ürün Kodu, Ürün Adı, Birim, KDV Oranı, Birim Fiyat*), **Yeni**, **Kaydet** |
| **Fatura** | Create or edit an invoice | *Fatura No, Fatura Tarihi, Satıcı, Alıcı, Belge Tipi, Açıklama*, line item entry (**Kalem Ekle**, **Seçileni Sil**), totals, XML preview, **Kaydet** |
| **İrsaliye** | Create or edit a waybill | *İrsaliye No, Tarih, Sevk Tarihi, Gönderen, Alıcı, Taşıyıcı Bilgisi*, line item entry, XML preview, **Kaydet** |
| **Belgeler** | Browse all documents | Filters (*Başlangıç Tarihi, Bitiş Tarihi, Cari, Belge Tipi*), **Filtrele**, **Temizle**, **Belgeyi Aç**, **Sil**, **XML Göster** |

**Units available for products:** `ADET`, `SAAT`, `GÜN`, `KG`, `LT`, `METRE`, `PAKET`.

**Document types in the filter:** Tümü (all), E-Fatura, E-Arşiv Fatura, E-İrsaliye.

---

## 4. Generated XML

| Document | Root element | `ProfileID` |
|----------|--------------|-------------|
| E-Fatura | `Invoice` (UBL Invoice-2) | `TEMELFATURA` |
| E-Arşiv Fatura | `Invoice` (UBL Invoice-2) | `EARSIVFATURA` |
| E-İrsaliye | `DespatchAdvice` (UBL DespatchAdvice-2) | `TEMELIRSALIYE` |

Fixed values used by the generator:

| Element | Value |
|---------|-------|
| `UBLVersionID` | `2.1` |
| `CustomizationID` | `TR1.2` |
| `InvoiceTypeCode` | `SATIS` (sales) |
| Currency (`currencyID`) | `TRY` |
| VAT tax scheme | `KDV`, code `0015` |
| Party identifier scheme | `TCKN` for an 11-digit number, otherwise `VKN` |

**File naming:** `{EFatura | EArsivFatura | EIrsaliye}_{number}_{yyyyMMdd_HHmmss}.xml`, for example `EFatura_FTR20260703-007_20260703_121600.xml`.

**Output folder:** `XmlOrnekleri` inside the project folder (the application walks up from its output directory until it finds `EDonusumDemo.csproj`). If the project file cannot be found, for example when running a published copy, it falls back to a `XmlOrnekleri` folder next to the executable.

This folder is excluded from Git (see `.gitignore`) because it only holds generated output.

Ready-made examples are in the [`Samples`](EDonusumDemo/Samples) folder: `sample-invoice.xml` and `sample-waybill.xml`.

---

## 5. Validation and calculation rules

### Customer card

| Field | Rule |
|-------|------|
| Title | Required |
| Tax number | Required; exactly 10 digits (VKN) or 11 digits (TCKN); digits only |
| Phone | Required; Turkish format. `03129111257`, `3129111257` and `+903129111257` are accepted and stored as `0 (312) 911 12 57` |
| Email | Required; valid address whose domain contains a dot; stored in lower case |

### Product

| Field | Rule |
|-------|------|
| Code and name | Required |
| VAT rate | Number between 0 and 100 |
| Unit price | Number greater than 0 |

### Invoice and waybill

- Document number is required.
- Seller/buyer (invoice) or sender/receiver (waybill) must be selected.
- At least one line item is required.
- Quantity must be greater than 0. Type decimals in Turkish format, with a **comma** as the decimal separator (`1250,50`). A dot is read as a thousands separator, so `1250.50` would be read as 125050.

### Calculations

| Value | Formula |
|-------|---------|
| Line amount | `round(quantity × unit price, 2)` |
| Line VAT | `round(line amount × VAT rate / 100, 2)` |
| Line total | line amount + line VAT |
| Invoice subtotal / VAT total / grand total | Sums of the line values |

Example: 2 units at 5,000 TRY with 20% VAT gives a line amount of 10,000.00, VAT of 2,000.00 and a total of 12,000.00.

---

## 6. Technologies used

| Technology | Purpose |
|------------|---------|
| **C# with .NET 9** (`net9.0-windows`) | Application language and runtime |
| **WPF** (XAML) | Desktop user interface |
| **Microsoft.Data.SqlClient 5.2.2** | SQL Server access |
| **SQL Server** (Express by default) | Data storage |
| **System.Xml.Linq** (`XDocument`) | UBL XML generation |

---

## 7. Project structure

```text
EDonusumDemo/
├── README.md                          # English documentation
├── README.tr.md                       # Turkish documentation
├── .gitignore
└── EDonusumDemo/
    ├── EDonusumDemo.csproj
    ├── App.xaml / App.xaml.cs         # Application entry
    ├── MainWindow.xaml / .xaml.cs     # All five tabs and their logic
    ├── AssemblyInfo.cs
    ├── Application/
    │   └── UblXmlGenerator.cs         # Builds invoice and waybill XML
    ├── Domain/                        # Plain model classes
    │   ├── Customer.cs
    │   ├── Product.cs
    │   ├── InvoiceDocument.cs
    │   ├── WaybillDocument.cs
    │   ├── DocumentLine.cs            # Line with amount and VAT calculations
    │   ├── DocumentKind.cs            # EInvoice, EArchiveInvoice, EWaybill
    │   └── DocumentListItem.cs        # Row model for the document list
    ├── Infrastructure/
    │   ├── DemoDataStore.cs           # SQL Server access, schema, seed data, XML saving
    │   └── StoreData.cs               # In-memory data container
    ├── Database/
    │   └── CreateDatabase.sql         # Optional manual database script
    └── Samples/
        ├── sample-invoice.xml
        └── sample-waybill.xml
```

---

## 8. Database

**Database name:** `EDonusumDemo`

| Table | Content |
|-------|---------|
| `Customers` | Customers and suppliers |
| `Products` | Products and services (unique `Code`) |
| `Invoices` | Invoice headers (unique `Number` + `DocumentKind`; kind is `EInvoice` or `EArchiveInvoice`) |
| `InvoiceLines` | Invoice lines (deleted together with the invoice) |
| `Waybills` | Waybill headers (unique `Number`) |
| `WaybillLines` | Waybill lines (deleted together with the waybill) |

**Automatic setup:** The application creates the database and all tables itself on first run, so running the SQL script is **not** required.

**Sample data:** On an empty database the application adds two customers (a sample supplier and "Demo Müşteri A.Ş.", both with placeholder tax numbers) and three products: `SRV-001` Yazılım Geliştirme Hizmeti (SAAT, 1,250), `LIS-001` Demo Lisans (ADET, 5,000) and `DAN-001` Danışmanlık (GÜN, 3,500), all with 20% VAT.

**Connection string:** Stored in `Data\connection-string.txt` next to the executable (for example `bin\Debug\net9.0-windows\Data\connection-string.txt`). It is created on first run with this default:

```text
Server=localhost\SQLEXPRESS;Database=EDonusumDemo;Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True;
```

Edit this file to use another server. For example, for SQL Server LocalDB:

```text
Server=(localdb)\MSSQLLocalDB;Database=EDonusumDemo;Trusted_Connection=True;
```

> **Optional script:** `Database/CreateDatabase.sql` creates the same schema manually. It is **not** safe to run after the application has created the tables (it will fail with "object already exists"). Use it only on an empty server, or skip it.

---

## 9. Installation and running

> **Requirements:**
> - Windows 10 or 11 (WPF runs on Windows only)
> - [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
> - SQL Server Express with a default instance named `SQLEXPRESS` and Windows Authentication, or another SQL Server you point the connection string to
> - Optional: Visual Studio 2022 version 17.12 or later with the ".NET desktop development" workload

### Method A — Visual Studio

1. Clone the repository:
   ```bash
   git clone https://github.com/berkaycinar-dev/EDonusumDemo.git
   ```
2. In Visual Studio choose **File → Open → Project/Solution**.
3. Select `EDonusumDemo/EDonusumDemo.csproj`. There is no `.sln` file; opening the project file is enough.
4. Make sure SQL Server is running (see below).
5. Press **F5**.

### Method B — Command line

1. Clone the repository and go into the project folder:
   ```bash
   git clone https://github.com/berkaycinar-dev/EDonusumDemo.git
   cd EDonusumDemo/EDonusumDemo
   ```
2. Restore the packages:
   ```bash
   dotnet restore
   ```
3. Run the application:
   ```bash
   dotnet run
   ```

### Checking that SQL Server is ready

1. Open **Services** (`services.msc`) and check that **SQL Server (SQLEXPRESS)** is *Running*.
2. If you use another server or instance name, edit `Data\connection-string.txt` after the first run (the application shows an error dialog with the file path if it cannot connect), then start it again.
3. On a successful start the status line shows "SQL Server bağlı" (SQL Server connected) followed by the path of the connection file.

### First use

1. Open the **Cari Kartlar** tab and check the two sample customers (or add your own).
2. Open **Stok / Malzeme** and check the three sample products.
3. Open **Fatura**, pick a seller, a buyer and a document type, choose a product, press **Kalem Ekle**, then **Kaydet**.
4. Open **Belgeler**, select the new row and press **XML Göster** to view the XML.
5. Find the XML file in the `XmlOrnekleri` folder.

---

## 10. Known limitations

- **Simplified UBL-TR:** The XML is not digitally signed, has no UUID, and is not validated against the official GİB schemas or business rules.
- **No integration:** Nothing is sent to GİB or to an e-invoice integrator; the project only produces local files.
- **Single VAT block:** The invoice has one `TaxSubtotal` for the total VAT, even if the lines use different VAT rates. Exemptions and withholding are not supported.
- **Fixed values:** The currency is always `TRY` and the invoice type is always `SATIS`.
- **E-Fatura checkbox:** The "e-Invoice payer" flag on a customer is stored and shown in the list, but it is not used when generating documents.
- **Waybill amount:** Waybills carry no prices, so the document list shows an amount of 0 for them.
- **Old XML files remain:** Saving an edited document writes a new timestamped XML file; the previous file is not removed (it is only removed when the document is deleted).
- **No delete for customers and products:** Only documents can be deleted from the interface.
- **Whole-store saving:** Every save writes all customers, products and documents to the database in one transaction. This is fine for a demo but not efficient for large data.
- **Decimal input:** Numbers are parsed with the Turkish culture first, so a dot is treated as a thousands separator. Use a comma for decimals.
- **Windows only.**
