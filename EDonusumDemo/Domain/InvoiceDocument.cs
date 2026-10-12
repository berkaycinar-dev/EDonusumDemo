namespace EDonusumDemo.Domain;

public sealed class InvoiceDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Number { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; } = DateTime.Today;
    public Guid SupplierId { get; set; }
    public Guid CustomerId { get; set; }
    public DocumentKind Kind { get; set; } = DocumentKind.EInvoice;
    public string Description { get; set; } = string.Empty;
    public List<DocumentLine> Lines { get; set; } = [];
    public string? XmlFilePath { get; set; }

    public decimal Subtotal => Lines.Sum(line => line.LineExtensionAmount);
    public decimal VatTotal => Lines.Sum(line => line.VatAmount);
    public decimal GrandTotal => Lines.Sum(line => line.TotalAmount);
}
