using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Net.Mail;
using System.Windows;
using System.Windows.Controls;
using EDonusumDemo.Application;
using EDonusumDemo.Domain;
using EDonusumDemo.Infrastructure;

namespace EDonusumDemo;

public partial class MainWindow : Window
{
    private readonly DemoDataStore _store = new();
    private readonly UblXmlGenerator _xmlGenerator = new();
    private readonly CultureInfo _culture = new("tr-TR");

    private ObservableCollection<Customer> _customers = [];
    private ObservableCollection<Product> _products = [];
    private readonly ObservableCollection<DocumentLine> _invoiceLines = [];
    private readonly ObservableCollection<DocumentLine> _waybillLines = [];
    private readonly ObservableCollection<DocumentListItem> _documentItems = [];
    private Guid? _editingInvoiceId;
    private Guid? _editingWaybillId;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _store.Load();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"SQL Server bağlantısı kurulamadı.\n\nBağlantı dosyası:\n{_store.ConnectionStringPath}\n\nHata:\n{ex.Message}",
                "SQL Server Bağlantı Hatası",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            StatusText.Text = "SQL Server bağlantısı kurulamadı.";
            return;
        }

        _customers = new ObservableCollection<Customer>(_store.Data.Customers);
        _products = new ObservableCollection<Product>(_store.Data.Products);

        BindDataSources();
        ResetCustomerForm();
        ResetProductForm();
        ResetInvoiceForm();
        ResetWaybillForm();
        RefreshDocumentList();

        StatusText.Text = $"SQL Server bağlı. Bağlantı dosyası: {_store.ConnectionStringPath}";
    }

    private void BindDataSources()
    {
        CustomersGrid.ItemsSource = _customers;
        ProductsGrid.ItemsSource = _products;
        InvoiceLinesGrid.ItemsSource = _invoiceLines;
        WaybillLinesGrid.ItemsSource = _waybillLines;
        DocumentsGrid.ItemsSource = _documentItems;

        InvoiceSupplierCombo.ItemsSource = _customers;
        InvoiceCustomerCombo.ItemsSource = _customers;
        WaybillSenderCombo.ItemsSource = _customers;
        WaybillReceiverCombo.ItemsSource = _customers;
        FilterCustomerCombo.ItemsSource = _customers;

        InvoiceProductCombo.ItemsSource = _products;
        WaybillProductCombo.ItemsSource = _products;
    }

    private void CustomersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CustomersGrid.SelectedItem is not Customer customer)
        {
            return;
        }

        CustomerTitleBox.Text = customer.Title;
        CustomerTaxOfficeBox.Text = customer.TaxOffice;
        CustomerTaxNumberBox.Text = customer.TaxNumber;
        CustomerAddressBox.Text = customer.Address;
        CustomerPhoneBox.Text = customer.Phone;
        CustomerEmailBox.Text = customer.Email;
        CustomerEInvoiceCheck.IsChecked = customer.IsEInvoicePayer;
    }

    private void NewCustomer_Click(object sender, RoutedEventArgs e) => ResetCustomerForm();

    private void SaveCustomer_Click(object sender, RoutedEventArgs e)
    {
        if (!TryValidateCustomerForm(out var normalizedTaxNumber, out var normalizedPhone, out var normalizedEmail))
        {
            return;
        }

        var customer = CustomersGrid.SelectedItem as Customer ?? new Customer();
        customer.Title = CustomerTitleBox.Text.Trim();
        customer.TaxOffice = CustomerTaxOfficeBox.Text.Trim();
        customer.TaxNumber = normalizedTaxNumber;
        customer.Address = CustomerAddressBox.Text.Trim();
        customer.Phone = normalizedPhone;
        customer.Email = normalizedEmail;
        customer.IsEInvoicePayer = CustomerEInvoiceCheck.IsChecked == true;

        if (!_store.Data.Customers.Any(item => item.Id == customer.Id))
        {
            _store.Data.Customers.Add(customer);
            _customers.Add(customer);
        }

        _store.Save();
        RefreshCustomerDisplays();
        ResetCustomerForm();
        SetStatus("Cari kayıt kaydedildi. Yeni kayıt için form temizlendi.");
    }

    private void ProductsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProductsGrid.SelectedItem is not Product product)
        {
            return;
        }

        ProductCodeBox.Text = product.Code;
        ProductNameBox.Text = product.Name;
        SelectProductUnit(product.Unit);
        ProductVatBox.Text = product.VatRate.ToString("0.##", _culture);
        ProductPriceBox.Text = product.UnitPrice.ToString("0.##", _culture);
    }

    private void NewProduct_Click(object sender, RoutedEventArgs e) => ResetProductForm();

    private void SaveProduct_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ProductCodeBox.Text) || string.IsNullOrWhiteSpace(ProductNameBox.Text))
        {
            ShowWarning("Ürün kodu ve ürün adı zorunludur.");
            return;
        }

        if (!TryGetDecimal(ProductVatBox.Text, out var vatRate) || !TryGetDecimal(ProductPriceBox.Text, out var unitPrice))
        {
            ShowWarning("KDV oranı ve birim fiyat sayısal olmalıdır.");
            return;
        }

        if (vatRate is < 0 or > 100)
        {
            ShowWarning("KDV oranı 0 ile 100 arasında olmalıdır.");
            ProductVatBox.Focus();
            return;
        }

        if (unitPrice <= 0)
        {
            ShowWarning("Birim fiyat sıfırdan büyük olmalıdır.");
            ProductPriceBox.Focus();
            return;
        }

        var product = ProductsGrid.SelectedItem as Product ?? new Product();
        product.Code = ProductCodeBox.Text.Trim();
        product.Name = ProductNameBox.Text.Trim();
        product.Unit = GetSelectedProductUnit();
        product.VatRate = vatRate;
        product.UnitPrice = unitPrice;

        if (!_store.Data.Products.Any(item => item.Id == product.Id))
        {
            _store.Data.Products.Add(product);
            _products.Add(product);
        }

        _store.Save();
        RefreshProductDisplays();
        ResetProductForm();
        SetStatus("Stok kayıt kaydedildi.");
    }

    private void InvoiceProductCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (InvoiceProductCombo.SelectedItem is not Product product)
        {
            return;
        }

        InvoiceQuantityBox.Text = "1";
        InvoiceUnitPriceBox.Text = product.UnitPrice.ToString("0.##", _culture);
        InvoiceVatBox.Text = product.VatRate.ToString("0.##", _culture);
    }

    private void AddInvoiceLine_Click(object sender, RoutedEventArgs e)
    {
        if (InvoiceProductCombo.SelectedItem is not Product product)
        {
            ShowWarning("Fatura kalemi için ürün seçin.");
            return;
        }

        if (!TryGetDecimal(InvoiceQuantityBox.Text, out var quantity) ||
            !TryGetDecimal(InvoiceUnitPriceBox.Text, out var unitPrice) ||
            !TryGetDecimal(InvoiceVatBox.Text, out var vatRate) ||
            quantity <= 0)
        {
            ShowWarning("Miktar, birim fiyat ve KDV alanlarını kontrol edin.");
            return;
        }

        _invoiceLines.Add(CreateLine(product, quantity, unitPrice, vatRate));
        UpdateInvoiceTotals();
    }

    private void RemoveInvoiceLine_Click(object sender, RoutedEventArgs e)
    {
        if (InvoiceLinesGrid.SelectedItem is DocumentLine line)
        {
            _invoiceLines.Remove(line);
            UpdateInvoiceTotals();
        }
    }

    private void SaveInvoice_Click(object sender, RoutedEventArgs e)
    {
        if (!TryBuildInvoice(out var invoice, out var customerPair))
        {
            return;
        }

        var xml = _xmlGenerator.CreateInvoiceXml(invoice, customerPair.Supplier, customerPair.Customer);
        var documentType = invoice.Kind == DocumentKind.EArchiveInvoice ? "EArsivFatura" : "EFatura";
        invoice.XmlFilePath = _store.SaveXml(documentType, invoice.Number, xml);
        UpsertInvoice(invoice);
        _store.Save();
        RefreshDocumentList();
        ResetInvoiceForm();
        SetStatus($"Fatura kaydedildi ve XML oluşturuldu: {invoice.XmlFilePath}");
    }

    private void AddWaybillLine_Click(object sender, RoutedEventArgs e)
    {
        if (WaybillProductCombo.SelectedItem is not Product product)
        {
            ShowWarning("İrsaliye kalemi için ürün seçin.");
            return;
        }

        if (!TryGetDecimal(WaybillQuantityBox.Text, out var quantity) || quantity <= 0)
        {
            ShowWarning("Miktar pozitif bir sayı olmalıdır.");
            return;
        }

        _waybillLines.Add(CreateLine(product, quantity, product.UnitPrice, product.VatRate));
    }

    private void RemoveWaybillLine_Click(object sender, RoutedEventArgs e)
    {
        if (WaybillLinesGrid.SelectedItem is DocumentLine line)
        {
            _waybillLines.Remove(line);
        }
    }

    private void SaveWaybill_Click(object sender, RoutedEventArgs e)
    {
        if (!TryBuildWaybill(out var waybill, out var customerPair))
        {
            return;
        }

        var xml = _xmlGenerator.CreateWaybillXml(waybill, customerPair.Supplier, customerPair.Customer);
        waybill.XmlFilePath = _store.SaveXml("EIrsaliye", waybill.Number, xml);
        UpsertWaybill(waybill);
        _store.Save();
        RefreshDocumentList();
        ResetWaybillForm();
        SetStatus($"İrsaliye kaydedildi ve XML oluşturuldu: {waybill.XmlFilePath}");
    }

    private void ApplyDocumentFilter_Click(object sender, RoutedEventArgs e) => RefreshDocumentList();

    private void ClearDocumentFilter_Click(object sender, RoutedEventArgs e)
    {
        FilterStartDatePicker.SelectedDate = null;
        FilterEndDatePicker.SelectedDate = null;
        FilterCustomerCombo.SelectedIndex = -1;
        FilterKindCombo.SelectedIndex = 0;
        RefreshDocumentList();
    }

    private void ShowSelectedDocumentXml_Click(object sender, RoutedEventArgs e)
    {
        if (DocumentsGrid.SelectedItem is not DocumentListItem item)
        {
            ShowWarning("XML görüntülemek için bir belge seçin.");
            return;
        }

        if (string.IsNullOrWhiteSpace(item.XmlFilePath) || !File.Exists(item.XmlFilePath))
        {
            ShowWarning("Seçili belge için XML dosyası oluşturulmamış.");
            return;
        }

        DocumentXmlPreviewBox.Text = File.ReadAllText(item.XmlFilePath);
    }

    private void OpenSelectedDocument_Click(object sender, RoutedEventArgs e)
    {
        if (DocumentsGrid.SelectedItem is not DocumentListItem item)
        {
            ShowWarning("Açmak için bir belge seçin.");
            return;
        }

        if (item.Kind == DocumentKind.EWaybill)
        {
            var waybill = _store.Data.Waybills.FirstOrDefault(document => document.Id == item.Id);
            if (waybill is null)
            {
                ShowWarning("Seçili irsaliye bulunamadı.");
                return;
            }

            LoadWaybillForEdit(waybill);
            return;
        }

        var invoice = _store.Data.Invoices.FirstOrDefault(document => document.Id == item.Id);
        if (invoice is null)
        {
            ShowWarning("Seçili fatura bulunamadı.");
            return;
        }

        LoadInvoiceForEdit(invoice);
    }

    private void DeleteSelectedDocument_Click(object sender, RoutedEventArgs e)
    {
        if (DocumentsGrid.SelectedItem is not DocumentListItem item)
        {
            ShowWarning("Silmek için bir belge seçin.");
            return;
        }

        var result = MessageBox.Show(
            $"{item.Number} numaralı belge silinsin mi?",
            "Belge Sil",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            if (item.Kind == DocumentKind.EWaybill)
            {
                var waybill = _store.Data.Waybills.FirstOrDefault(document => document.Id == item.Id);
                if (waybill is null)
                {
                    ShowWarning("Seçili irsaliye bulunamadı.");
                    return;
                }

                _store.DeleteWaybill(waybill.Id);
                _store.Data.Waybills.Remove(waybill);
                TryDeleteXmlFile(waybill.XmlFilePath);

                if (_editingWaybillId == waybill.Id)
                {
                    ResetWaybillForm();
                }
            }
            else
            {
                var invoice = _store.Data.Invoices.FirstOrDefault(document => document.Id == item.Id);
                if (invoice is null)
                {
                    ShowWarning("Seçili fatura bulunamadı.");
                    return;
                }

                _store.DeleteInvoice(invoice.Id);
                _store.Data.Invoices.Remove(invoice);
                TryDeleteXmlFile(invoice.XmlFilePath);

                if (_editingInvoiceId == invoice.Id)
                {
                    ResetInvoiceForm();
                }
            }

            RefreshDocumentList();
            DocumentXmlPreviewBox.Clear();
            SetStatus($"Belge silindi: {item.Number}");
        }
        catch (Exception ex)
        {
            ShowWarning($"Belge silinemedi: {ex.Message}");
        }
    }

    private bool TryBuildInvoice(out InvoiceDocument invoice, out (Customer Supplier, Customer Customer) customers)
    {
        invoice = new InvoiceDocument();
        customers = default;

        if (string.IsNullOrWhiteSpace(InvoiceNumberBox.Text))
        {
            ShowWarning("Fatura no zorunludur.");
            return false;
        }

        if (InvoiceSupplierCombo.SelectedItem is not Customer supplier || InvoiceCustomerCombo.SelectedItem is not Customer customer)
        {
            ShowWarning("Satıcı ve alıcı seçilmelidir.");
            return false;
        }

        if (_invoiceLines.Count == 0)
        {
            ShowWarning("Faturaya en az bir kalem ekleyin.");
            return false;
        }

        invoice.Id = _editingInvoiceId ?? Guid.NewGuid();
        invoice.Number = InvoiceNumberBox.Text.Trim();
        invoice.IssueDate = InvoiceDatePicker.SelectedDate ?? DateTime.Today;
        invoice.SupplierId = supplier.Id;
        invoice.CustomerId = customer.Id;
        invoice.Kind = GetSelectedInvoiceKind();
        invoice.Description = InvoiceDescriptionBox.Text.Trim();
        invoice.Lines = _invoiceLines.Select(CloneLine).ToList();
        var invoiceId = invoice.Id;
        var invoiceNumber = invoice.Number;
        var invoiceKind = invoice.Kind;
        invoice.XmlFilePath = _store.Data.Invoices
            .FirstOrDefault(item => item.Id == invoiceId || (item.Number == invoiceNumber && item.Kind == invoiceKind))
            ?.XmlFilePath;
        customers = (supplier, customer);
        return true;
    }

    private bool TryBuildWaybill(out WaybillDocument waybill, out (Customer Supplier, Customer Customer) customers)
    {
        waybill = new WaybillDocument();
        customers = default;

        if (string.IsNullOrWhiteSpace(WaybillNumberBox.Text))
        {
            ShowWarning("İrsaliye no zorunludur.");
            return false;
        }

        if (WaybillSenderCombo.SelectedItem is not Customer sender || WaybillReceiverCombo.SelectedItem is not Customer receiver)
        {
            ShowWarning("Gönderen ve alıcı seçilmelidir.");
            return false;
        }

        if (_waybillLines.Count == 0)
        {
            ShowWarning("İrsaliyeye en az bir kalem ekleyin.");
            return false;
        }

        waybill.Id = _editingWaybillId ?? Guid.NewGuid();
        waybill.Number = WaybillNumberBox.Text.Trim();
        waybill.IssueDate = WaybillDatePicker.SelectedDate ?? DateTime.Today;
        waybill.ShipmentDate = ShipmentDatePicker.SelectedDate ?? DateTime.Today;
        waybill.SenderId = sender.Id;
        waybill.ReceiverId = receiver.Id;
        waybill.CarrierInfo = CarrierInfoBox.Text.Trim();
        waybill.Lines = _waybillLines.Select(CloneLine).ToList();
        var waybillId = waybill.Id;
        var waybillNumber = waybill.Number;
        waybill.XmlFilePath = _store.Data.Waybills
            .FirstOrDefault(item => item.Id == waybillId || item.Number == waybillNumber)
            ?.XmlFilePath;
        customers = (sender, receiver);
        return true;
    }

    private void UpsertInvoice(InvoiceDocument invoice)
    {
        var existing = _store.Data.Invoices.FirstOrDefault(item => item.Id == invoice.Id)
            ?? _store.Data.Invoices.FirstOrDefault(item => item.Number == invoice.Number && item.Kind == invoice.Kind);
        if (existing is null)
        {
            _store.Data.Invoices.Add(invoice);
            return;
        }

        invoice.Id = existing.Id;
        existing.Number = invoice.Number;
        existing.IssueDate = invoice.IssueDate;
        existing.SupplierId = invoice.SupplierId;
        existing.CustomerId = invoice.CustomerId;
        existing.Kind = invoice.Kind;
        existing.Description = invoice.Description;
        existing.Lines = invoice.Lines;
        existing.XmlFilePath = invoice.XmlFilePath;
    }

    private void UpsertWaybill(WaybillDocument waybill)
    {
        var existing = _store.Data.Waybills.FirstOrDefault(item => item.Id == waybill.Id)
            ?? _store.Data.Waybills.FirstOrDefault(item => item.Number == waybill.Number);
        if (existing is null)
        {
            _store.Data.Waybills.Add(waybill);
            return;
        }

        waybill.Id = existing.Id;
        existing.Number = waybill.Number;
        existing.IssueDate = waybill.IssueDate;
        existing.ShipmentDate = waybill.ShipmentDate;
        existing.SenderId = waybill.SenderId;
        existing.ReceiverId = waybill.ReceiverId;
        existing.CarrierInfo = waybill.CarrierInfo;
        existing.Lines = waybill.Lines;
        existing.XmlFilePath = waybill.XmlFilePath;
    }

    private void RefreshDocumentList()
    {
        _documentItems.Clear();
        var startDate = FilterStartDatePicker.SelectedDate?.Date;
        var endDate = FilterEndDatePicker.SelectedDate?.Date;
        var selectedCustomer = FilterCustomerCombo.SelectedItem as Customer;
        var selectedKind = (FilterKindCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "All";

        var items = new List<DocumentListItem>();
        items.AddRange(_store.Data.Invoices.Select(invoice =>
        {
            var customer = FindCustomer(invoice.CustomerId);
            return new DocumentListItem
            {
                Id = invoice.Id,
                Number = invoice.Number,
                Date = invoice.IssueDate,
                CustomerTitle = customer?.Title ?? "-",
                ProductType = SummarizeProductTypes(invoice.Lines),
                Amount = invoice.GrandTotal,
                Kind = invoice.Kind,
                XmlStatus = string.IsNullOrWhiteSpace(invoice.XmlFilePath) ? "Yok" : "Var",
                XmlFilePath = invoice.XmlFilePath
            };
        }));

        items.AddRange(_store.Data.Waybills.Select(waybill =>
        {
            var customer = FindCustomer(waybill.ReceiverId);
            return new DocumentListItem
            {
                Id = waybill.Id,
                Number = waybill.Number,
                Date = waybill.IssueDate,
                CustomerTitle = customer?.Title ?? "-",
                ProductType = SummarizeProductTypes(waybill.Lines),
                Amount = 0,
                Kind = DocumentKind.EWaybill,
                XmlStatus = string.IsNullOrWhiteSpace(waybill.XmlFilePath) ? "Yok" : "Var",
                XmlFilePath = waybill.XmlFilePath
            };
        }));

        foreach (var item in items
                     .Where(item => startDate is null || item.Date.Date >= startDate.Value)
                     .Where(item => endDate is null || item.Date.Date <= endDate.Value)
                     .Where(item => selectedCustomer is null || item.CustomerTitle == selectedCustomer.Title)
                     .Where(item => selectedKind == "All" || item.Kind.ToString() == selectedKind)
                     .OrderByDescending(item => item.Date)
                     .ThenBy(item => item.Number))
        {
            _documentItems.Add(item);
        }
    }

    private void LoadInvoiceForEdit(InvoiceDocument invoice)
    {
        _editingInvoiceId = invoice.Id;
        InvoiceNumberBox.Text = invoice.Number;
        InvoiceDatePicker.SelectedDate = invoice.IssueDate;
        SelectComboCustomer(InvoiceSupplierCombo, invoice.SupplierId);
        SelectComboCustomer(InvoiceCustomerCombo, invoice.CustomerId);
        SelectInvoiceKind(invoice.Kind);
        InvoiceDescriptionBox.Text = invoice.Description;
        InvoiceProductCombo.SelectedIndex = _products.Count > 0 ? 0 : -1;

        _invoiceLines.Clear();
        foreach (var line in invoice.Lines.Select(CloneLine))
        {
            _invoiceLines.Add(line);
        }

        InvoiceXmlPreviewBox.Text = !string.IsNullOrWhiteSpace(invoice.XmlFilePath) && File.Exists(invoice.XmlFilePath)
            ? File.ReadAllText(invoice.XmlFilePath)
            : string.Empty;

        UpdateInvoiceTotals();
        MainTabs.SelectedItem = InvoiceTabItem;
        SetStatus($"Fatura düzenleme için açıldı: {invoice.Number}");
    }

    private void LoadWaybillForEdit(WaybillDocument waybill)
    {
        _editingWaybillId = waybill.Id;
        WaybillNumberBox.Text = waybill.Number;
        WaybillDatePicker.SelectedDate = waybill.IssueDate;
        ShipmentDatePicker.SelectedDate = waybill.ShipmentDate;
        SelectComboCustomer(WaybillSenderCombo, waybill.SenderId);
        SelectComboCustomer(WaybillReceiverCombo, waybill.ReceiverId);
        WaybillProductCombo.SelectedIndex = _products.Count > 0 ? 0 : -1;
        CarrierInfoBox.Text = waybill.CarrierInfo;

        _waybillLines.Clear();
        foreach (var line in waybill.Lines.Select(CloneLine))
        {
            _waybillLines.Add(line);
        }

        WaybillXmlPreviewBox.Text = !string.IsNullOrWhiteSpace(waybill.XmlFilePath) && File.Exists(waybill.XmlFilePath)
            ? File.ReadAllText(waybill.XmlFilePath)
            : string.Empty;

        MainTabs.SelectedItem = WaybillTabItem;
        SetStatus($"İrsaliye düzenleme için açıldı: {waybill.Number}");
    }

    private static string SummarizeProductTypes(IEnumerable<DocumentLine> lines)
    {
        var productNames = lines
            .Select(line => line.ProductName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        return productNames.Count switch
        {
            0 => "-",
            1 => productNames[0],
            _ => $"{productNames[0]} + {productNames.Count - 1}"
        };
    }

    private void ResetCustomerForm()
    {
        CustomersGrid.SelectedIndex = -1;
        CustomerTitleBox.Clear();
        CustomerTaxOfficeBox.Clear();
        CustomerTaxNumberBox.Clear();
        CustomerAddressBox.Clear();
        CustomerPhoneBox.Clear();
        CustomerEmailBox.Clear();
        CustomerEInvoiceCheck.IsChecked = false;
    }

    private void ResetProductForm()
    {
        ProductsGrid.SelectedIndex = -1;
        ProductCodeBox.Clear();
        ProductNameBox.Clear();
        ProductUnitCombo.SelectedIndex = 0;
        ProductVatBox.Text = "20";
        ProductPriceBox.Text = "0";
    }

    private void ResetInvoiceForm()
    {
        _editingInvoiceId = null;
        InvoiceNumberBox.Text = $"FTR{DateTime.Today:yyyyMMdd}-{_store.Data.Invoices.Count + 1:000}";
        InvoiceDatePicker.SelectedDate = DateTime.Today;
        InvoiceSupplierCombo.SelectedIndex = _customers.Count > 0 ? 0 : -1;
        InvoiceCustomerCombo.SelectedIndex = _customers.Count > 1 ? 1 : 0;
        InvoiceKindCombo.SelectedIndex = 0;
        InvoiceDescriptionBox.Clear();
        InvoiceProductCombo.SelectedIndex = _products.Count > 0 ? 0 : -1;
        _invoiceLines.Clear();
        InvoiceXmlPreviewBox.Clear();
        UpdateInvoiceTotals();
    }

    private void ResetWaybillForm()
    {
        _editingWaybillId = null;
        WaybillNumberBox.Text = $"IRS{DateTime.Today:yyyyMMdd}-{_store.Data.Waybills.Count + 1:000}";
        WaybillDatePicker.SelectedDate = DateTime.Today;
        ShipmentDatePicker.SelectedDate = DateTime.Today;
        WaybillSenderCombo.SelectedIndex = _customers.Count > 0 ? 0 : -1;
        WaybillReceiverCombo.SelectedIndex = _customers.Count > 1 ? 1 : 0;
        WaybillProductCombo.SelectedIndex = _products.Count > 0 ? 0 : -1;
        CarrierInfoBox.Clear();
        _waybillLines.Clear();
        WaybillXmlPreviewBox.Clear();
    }

    private void UpdateInvoiceTotals()
    {
        var subtotal = _invoiceLines.Sum(line => line.LineExtensionAmount);
        var vatTotal = _invoiceLines.Sum(line => line.VatAmount);
        var grandTotal = _invoiceLines.Sum(line => line.TotalAmount);

        InvoiceSubtotalText.Text = $"Ara Toplam: {subtotal.ToString("N2", _culture)} TL";
        InvoiceVatTotalText.Text = $"KDV Toplamı: {vatTotal.ToString("N2", _culture)} TL";
        InvoiceGrandTotalText.Text = $"Genel Toplam: {grandTotal.ToString("N2", _culture)} TL";
    }

    private void RefreshCustomerDisplays()
    {
        CustomersGrid.Items.Refresh();
        InvoiceSupplierCombo.Items.Refresh();
        InvoiceCustomerCombo.Items.Refresh();
        WaybillSenderCombo.Items.Refresh();
        WaybillReceiverCombo.Items.Refresh();
        FilterCustomerCombo.Items.Refresh();
    }

    private void RefreshProductDisplays()
    {
        ProductsGrid.Items.Refresh();
        InvoiceProductCombo.Items.Refresh();
        WaybillProductCombo.Items.Refresh();
    }

    private string GetSelectedProductUnit() =>
        (ProductUnitCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "ADET";

    private void SelectProductUnit(string unit)
    {
        foreach (var item in ProductUnitCombo.Items.OfType<ComboBoxItem>())
        {
            if (string.Equals(item.Content?.ToString(), unit, StringComparison.OrdinalIgnoreCase))
            {
                ProductUnitCombo.SelectedItem = item;
                return;
            }
        }

        ProductUnitCombo.SelectedIndex = 0;
    }

    private static void SelectComboCustomer(ComboBox comboBox, Guid customerId)
    {
        foreach (var customer in comboBox.Items.OfType<Customer>())
        {
            if (customer.Id == customerId)
            {
                comboBox.SelectedItem = customer;
                return;
            }
        }

        comboBox.SelectedIndex = -1;
    }

    private void SelectInvoiceKind(DocumentKind kind)
    {
        foreach (var item in InvoiceKindCombo.Items.OfType<ComboBoxItem>())
        {
            if (string.Equals(item.Tag?.ToString(), kind.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                InvoiceKindCombo.SelectedItem = item;
                return;
            }
        }

        InvoiceKindCombo.SelectedIndex = 0;
    }

    private bool TryValidateCustomerForm(out string normalizedTaxNumber, out string normalizedPhone, out string normalizedEmail)
    {
        normalizedTaxNumber = string.Empty;
        normalizedPhone = string.Empty;
        normalizedEmail = string.Empty;

        if (string.IsNullOrWhiteSpace(CustomerTitleBox.Text))
        {
            ShowWarning("Cari unvan zorunludur.");
            CustomerTitleBox.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(CustomerTaxNumberBox.Text))
        {
            ShowWarning("Vergi no / TCKN zorunludur.");
            CustomerTaxNumberBox.Focus();
            return false;
        }

        normalizedTaxNumber = OnlyDigits(CustomerTaxNumberBox.Text);
        if (normalizedTaxNumber.Length is not (10 or 11) || normalizedTaxNumber != CustomerTaxNumberBox.Text.Trim())
        {
            ShowWarning("Vergi no 10 haneli VKN veya 11 haneli TCKN olmalı ve yalnızca rakam içermelidir.");
            CustomerTaxNumberBox.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(CustomerPhoneBox.Text) || !TryNormalizeTurkishPhone(CustomerPhoneBox.Text, out normalizedPhone))
        {
            ShowWarning("Telefon numarası Türkiye formatında olmalıdır. Örnek: 03129111257, 3129111257 veya +903129111257.");
            CustomerPhoneBox.Focus();
            return false;
        }

        if (!TryNormalizeEmail(CustomerEmailBox.Text, out normalizedEmail))
        {
            ShowWarning("E-posta adresi geçerli formatta olmalıdır. Örnek: muhasebe@firma.com");
            CustomerEmailBox.Focus();
            return false;
        }

        return true;
    }

    private DocumentKind GetSelectedInvoiceKind()
    {
        var tag = (InvoiceKindCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        return tag == nameof(DocumentKind.EArchiveInvoice) ? DocumentKind.EArchiveInvoice : DocumentKind.EInvoice;
    }

    private Customer? FindCustomer(Guid id) => _store.Data.Customers.FirstOrDefault(customer => customer.Id == id);

    private static DocumentLine CreateLine(Product product, decimal quantity, decimal unitPrice, decimal vatRate) =>
        new()
        {
            ProductId = product.Id,
            ProductCode = product.Code,
            ProductName = product.Name,
            Unit = product.Unit,
            Quantity = quantity,
            UnitPrice = unitPrice,
            VatRate = vatRate
        };

    private static DocumentLine CloneLine(DocumentLine line) =>
        new()
        {
            ProductId = line.ProductId,
            ProductCode = line.ProductCode,
            ProductName = line.ProductName,
            Unit = line.Unit,
            Quantity = line.Quantity,
            UnitPrice = line.UnitPrice,
            VatRate = line.VatRate
        };

    private bool TryGetDecimal(string value, out decimal result) =>
        decimal.TryParse(value, NumberStyles.Number, _culture, out result) ||
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result);

    private static bool TryNormalizeTurkishPhone(string value, out string normalizedPhone)
    {
        normalizedPhone = string.Empty;
        var digits = OnlyDigits(value);

        if (digits.StartsWith("90", StringComparison.Ordinal) && digits.Length == 12)
        {
            digits = "0" + digits[2..];
        }
        else if (digits.Length == 10)
        {
            digits = "0" + digits;
        }

        if (digits.Length != 11 || digits[0] != '0' || digits[1] is < '2' or > '5')
        {
            return false;
        }

        normalizedPhone = string.Format(CultureInfo.InvariantCulture, "0 ({0}) {1} {2} {3}",
            digits.Substring(1, 3),
            digits.Substring(4, 3),
            digits.Substring(7, 2),
            digits.Substring(9, 2));

        return true;
    }

    private static bool TryNormalizeEmail(string value, out string normalizedEmail)
    {
        normalizedEmail = string.Empty;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        try
        {
            var address = new MailAddress(value.Trim());
            if (address.Address != value.Trim() || !address.Host.Contains('.', StringComparison.Ordinal))
            {
                return false;
            }

            normalizedEmail = address.Address.ToLowerInvariant();
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string OnlyDigits(string value) =>
        new(value.Where(char.IsDigit).ToArray());

    private static void TryDeleteXmlFile(string? xmlFilePath)
    {
        if (!string.IsNullOrWhiteSpace(xmlFilePath) && File.Exists(xmlFilePath))
        {
            File.Delete(xmlFilePath);
        }
    }

    private void SetStatus(string message) => StatusText.Text = message;

    private static void ShowWarning(string message) =>
        MessageBox.Show(message, "Kontrol", MessageBoxButton.OK, MessageBoxImage.Warning);
}
