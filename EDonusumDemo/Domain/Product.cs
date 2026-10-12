namespace EDonusumDemo.Domain;

public sealed class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = "ADET";
    public decimal VatRate { get; set; } = 20;
    public decimal UnitPrice { get; set; }

    public override string ToString() => $"{Code} - {Name}";
}
