namespace LedgerLoop.Api.Domain;

public class InvoiceLine
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid InvoiceId { get; set; }

    public Invoice? Invoice { get; set; }

    public int LineNumber { get; set; }

    public string Description { get; set; } = string.Empty;

    public LineCategory Category { get; set; } = LineCategory.StandardGoods;

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal LineDiscountPercent { get; set; }

    /// <summary>
    /// Rate applied the last time the document was calculated. Persisted so the
    /// audit export and reprints stay stable after a rate change.
    /// </summary>
    public decimal RatePercent { get; set; }

    public decimal NetAmount { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal GrossAmount { get; set; }

    public string TreatmentCode { get; set; } = string.Empty;
}
