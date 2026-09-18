namespace LedgerLoop.Api.Domain;

public class Invoice
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string InvoiceNumber { get; set; } = string.Empty;

    public Guid CustomerId { get; set; }

    public Customer? Customer { get; set; }

    public DocumentKind DocumentKind { get; set; } = DocumentKind.Invoice;

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    public PriceMode PriceMode { get; set; } = PriceMode.Exclusive;

    /// <summary>
    /// Date the document takes accounting effect on. Drives the rate schedule.
    /// </summary>
    public DateOnly InvoiceDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    public string Currency { get; set; } = "EUR";

    public decimal InvoiceDiscountPercent { get; set; }

    public decimal? ManualTaxAmount { get; set; }

    public string? ManualTaxReason { get; set; }

    public Guid? OriginalInvoiceId { get; set; }

    public string? Notes { get; set; }

    public decimal NetAmount { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal GrossAmount { get; set; }

    public decimal PostingAdjustment { get; set; }

    public string TaxTreatment { get; set; } = string.Empty;

    public DateTime? PostedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<InvoiceLine> Lines { get; set; } = new();
}
