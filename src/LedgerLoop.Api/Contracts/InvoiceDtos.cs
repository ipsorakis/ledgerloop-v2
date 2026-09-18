using LedgerLoop.Api.Domain;

namespace LedgerLoop.Api.Contracts;

public sealed class DraftInvoiceRequest
{
    public Guid CustomerId { get; set; }

    public DateOnly? InvoiceDate { get; set; }

    public PriceMode PriceMode { get; set; } = PriceMode.Exclusive;

    public string? Currency { get; set; }

    public decimal InvoiceDiscountPercent { get; set; }

    public decimal? ManualTaxAmount { get; set; }

    public string? ManualTaxReason { get; set; }

    public string? Notes { get; set; }

    public List<InvoiceLineRequest> Lines { get; set; } = new();
}

public sealed class InvoiceLineRequest
{
    public int? LineNumber { get; set; }

    public string Description { get; set; } = string.Empty;

    public LineCategory Category { get; set; } = LineCategory.StandardGoods;

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal LineDiscountPercent { get; set; }
}

public sealed class CreditNoteRequest
{
    public Guid InvoiceId { get; set; }

    public string? Reason { get; set; }

    public DateOnly? CreditNoteDate { get; set; }

    /// <summary>
    /// Line numbers of the source invoice to credit. Empty credits everything.
    /// </summary>
    public List<int> LineNumbers { get; set; } = new();
}

public sealed class InvoiceSummaryResponse
{
    public Guid Id { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    public string DocumentKind { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateOnly InvoiceDate { get; set; }

    public string Currency { get; set; } = string.Empty;

    public decimal NetAmount { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal GrossAmount { get; set; }

    public string TaxTreatment { get; set; } = string.Empty;
}

public sealed class InvoiceDetailResponse
{
    public Guid Id { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;

    public Guid CustomerId { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string CustomerCountry { get; set; } = string.Empty;

    public string DocumentKind { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string PriceMode { get; set; } = string.Empty;

    public DateOnly InvoiceDate { get; set; }

    public string Currency { get; set; } = string.Empty;

    public decimal InvoiceDiscountPercent { get; set; }

    public decimal? ManualTaxAmount { get; set; }

    public string? Notes { get; set; }

    public decimal PostingAdjustment { get; set; }

    public DateTime? PostedAt { get; set; }

    public Guid? OriginalInvoiceId { get; set; }

    public TaxResult Totals { get; set; } = new();

    public List<InvoiceLineResponse> Lines { get; set; } = new();
}

public sealed class InvoiceLineResponse
{
    public int LineNumber { get; set; }

    public string Description { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal LineDiscountPercent { get; set; }
}

public sealed class LegacyTotalsRequest
{
    public decimal? ClientTaxTotal { get; set; }

    public List<LegacyTotalsLine> Lines { get; set; } = new();
}

public sealed class LegacyTotalsLine
{
    public int LineNumber { get; set; }

    public decimal TaxAmount { get; set; }
}

public sealed class LegacyTotalsResponse
{
    public bool ClientValuesAccepted { get; set; }

    public decimal ClientTaxTotal { get; set; }

    public decimal ServerTaxTotal { get; set; }

    public TaxResult Totals { get; set; } = new();
}
