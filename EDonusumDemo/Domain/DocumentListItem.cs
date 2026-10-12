namespace EDonusumDemo.Domain;

public sealed class DocumentListItem
{
    public Guid Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string CustomerTitle { get; set; } = string.Empty;
    public string ProductType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DocumentKind Kind { get; set; }
    public string KindName => Kind switch
    {
        DocumentKind.EInvoice => "E-Fatura",
        DocumentKind.EArchiveInvoice => "E-Arşiv Fatura",
        DocumentKind.EWaybill => "E-İrsaliye",
        _ => Kind.ToString()
    };
    public string XmlStatus { get; set; } = "Yok";
    public string? XmlFilePath { get; set; }
}
