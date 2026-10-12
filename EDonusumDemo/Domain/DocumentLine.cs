namespace EDonusumDemo.Domain;

public sealed class DocumentLine
{
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Unit { get; set; } = "ADET";
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal VatRate { get; set; }

    public decimal LineExtensionAmount => Math.Round(Quantity * UnitPrice, 2);
    public decimal VatAmount => Math.Round(LineExtensionAmount * VatRate / 100m, 2);
    public decimal TotalAmount => LineExtensionAmount + VatAmount;
}
