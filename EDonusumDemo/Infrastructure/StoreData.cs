using EDonusumDemo.Domain;

namespace EDonusumDemo.Infrastructure;

public sealed class StoreData
{
    public List<Customer> Customers { get; set; } = [];
    public List<Product> Products { get; set; } = [];
    public List<InvoiceDocument> Invoices { get; set; } = [];
    public List<WaybillDocument> Waybills { get; set; } = [];
}
