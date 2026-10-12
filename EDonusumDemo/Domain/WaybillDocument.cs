namespace EDonusumDemo.Domain;

public sealed class WaybillDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Number { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; } = DateTime.Today;
    public DateTime ShipmentDate { get; set; } = DateTime.Today;
    public Guid SenderId { get; set; }
    public Guid ReceiverId { get; set; }
    public string CarrierInfo { get; set; } = string.Empty;
    public List<DocumentLine> Lines { get; set; } = [];
    public string? XmlFilePath { get; set; }
}
