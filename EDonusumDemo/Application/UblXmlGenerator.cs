using System.Xml.Linq;
using EDonusumDemo.Domain;

namespace EDonusumDemo.Application;

public sealed class UblXmlGenerator
{
    private static readonly XNamespace InvoiceNs = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2";
    private static readonly XNamespace DespatchNs = "urn:oasis:names:specification:ubl:schema:xsd:DespatchAdvice-2";
    private static readonly XNamespace Cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
    private static readonly XNamespace Cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";

    public string CreateInvoiceXml(InvoiceDocument invoice, Customer supplier, Customer customer)
    {
        var root = new XElement(InvoiceNs + "Invoice",
            NamespaceAttributes(),
            new XElement(Cbc + "UBLVersionID", "2.1"),
            new XElement(Cbc + "CustomizationID", "TR1.2"),
            new XElement(Cbc + "ProfileID", invoice.Kind == DocumentKind.EArchiveInvoice ? "EARSIVFATURA" : "TEMELFATURA"),
            new XElement(Cbc + "ID", invoice.Number),
            new XElement(Cbc + "IssueDate", invoice.IssueDate.ToString("yyyy-MM-dd")),
            new XElement(Cbc + "InvoiceTypeCode", "SATIS"),
            OptionalNote(invoice.Description),
            Party("AccountingSupplierParty", supplier),
            Party("AccountingCustomerParty", customer),
            TaxTotal(invoice.Subtotal, invoice.VatTotal),
            MonetaryTotal(invoice.Subtotal, invoice.GrandTotal),
            invoice.Lines.Select((line, index) => InvoiceLine(line, index + 1)));

        return new XDocument(new XDeclaration("1.0", "UTF-8", null), root).ToString();
    }

    public string CreateWaybillXml(WaybillDocument waybill, Customer sender, Customer receiver)
    {
        var root = new XElement(DespatchNs + "DespatchAdvice",
            NamespaceAttributes(),
            new XElement(Cbc + "UBLVersionID", "2.1"),
            new XElement(Cbc + "CustomizationID", "TR1.2"),
            new XElement(Cbc + "ProfileID", "TEMELIRSALIYE"),
            new XElement(Cbc + "ID", waybill.Number),
            new XElement(Cbc + "IssueDate", waybill.IssueDate.ToString("yyyy-MM-dd")),
            OptionalNote(waybill.CarrierInfo),
            Party("DespatchSupplierParty", sender),
            Party("DeliveryCustomerParty", receiver),
            new XElement(Cac + "Shipment",
                new XElement(Cbc + "ID", waybill.Number),
                new XElement(Cac + "Delivery",
                    new XElement(Cac + "Despatch",
                        new XElement(Cbc + "ActualDespatchDate", waybill.ShipmentDate.ToString("yyyy-MM-dd"))))),
            waybill.Lines.Select((line, index) => DespatchLine(line, index + 1)));

        return new XDocument(new XDeclaration("1.0", "UTF-8", null), root).ToString();
    }

    private static object[] NamespaceAttributes() =>
    [
        new XAttribute(XNamespace.Xmlns + "cac", Cac),
        new XAttribute(XNamespace.Xmlns + "cbc", Cbc)
    ];

    private static XElement? OptionalNote(string note) =>
        string.IsNullOrWhiteSpace(note) ? null : new XElement(Cbc + "Note", note);

    private static XElement Party(string elementName, Customer customer) =>
        new(Cac + elementName,
            new XElement(Cac + "Party",
                new XElement(Cac + "PartyName", new XElement(Cbc + "Name", customer.Title)),
                new XElement(Cac + "PostalAddress",
                    new XElement(Cbc + "StreetName", customer.Address)),
                new XElement(Cac + "PartyTaxScheme",
                    new XElement(Cac + "TaxScheme", new XElement(Cbc + "Name", customer.TaxOffice))),
                new XElement(Cac + "Contact",
                    new XElement(Cbc + "Telephone", customer.Phone),
                    new XElement(Cbc + "ElectronicMail", customer.Email)),
                new XElement(Cac + "PartyIdentification",
                    new XElement(Cbc + "ID", new XAttribute("schemeID", customer.TaxNumber.Length == 11 ? "TCKN" : "VKN"), customer.TaxNumber))));

    private static XElement TaxTotal(decimal taxableAmount, decimal vatTotal) =>
        new(Cac + "TaxTotal",
            Amount("TaxAmount", vatTotal),
            new XElement(Cac + "TaxSubtotal",
                Amount("TaxableAmount", taxableAmount),
                Amount("TaxAmount", vatTotal),
                new XElement(Cac + "TaxCategory",
                    new XElement(Cac + "TaxScheme",
                        new XElement(Cbc + "Name", "KDV"),
                        new XElement(Cbc + "TaxTypeCode", "0015")))));

    private static XElement MonetaryTotal(decimal subtotal, decimal grandTotal) =>
        new(Cac + "LegalMonetaryTotal",
            Amount("LineExtensionAmount", subtotal),
            Amount("TaxExclusiveAmount", subtotal),
            Amount("TaxInclusiveAmount", grandTotal),
            Amount("PayableAmount", grandTotal));

    private static XElement InvoiceLine(DocumentLine line, int index) =>
        new(Cac + "InvoiceLine",
            new XElement(Cbc + "ID", index),
            new XElement(Cbc + "InvoicedQuantity", new XAttribute("unitCode", line.Unit), line.Quantity),
            Amount("LineExtensionAmount", line.LineExtensionAmount),
            new XElement(Cac + "TaxTotal", Amount("TaxAmount", line.VatAmount)),
            Item(line),
            new XElement(Cac + "Price", Amount("PriceAmount", line.UnitPrice)));

    private static XElement DespatchLine(DocumentLine line, int index) =>
        new(Cac + "DespatchLine",
            new XElement(Cbc + "ID", index),
            new XElement(Cbc + "DeliveredQuantity", new XAttribute("unitCode", line.Unit), line.Quantity),
            Item(line));

    private static XElement Item(DocumentLine line) =>
        new(Cac + "Item",
            new XElement(Cbc + "Name", line.ProductName),
            new XElement(Cac + "SellersItemIdentification", new XElement(Cbc + "ID", line.ProductCode)));

    private static XElement Amount(string name, decimal value) =>
        new(Cbc + name, new XAttribute("currencyID", "TRY"), value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
}
