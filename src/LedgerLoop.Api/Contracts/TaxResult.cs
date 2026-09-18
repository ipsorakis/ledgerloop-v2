namespace LedgerLoop.Api.Contracts;

/// <summary>
/// Result shape returned by the preview endpoint, persisted on posting and
/// re-emitted by the audit export. External consumers (the finance data
/// warehouse feed and the archive reprint tool) bind to these field names.
/// </summary>
public sealed class TaxResult
{
    public string DocumentNumber { get; set; } = string.Empty;

    public string DocumentKind { get; set; } = "Invoice";

    public string Currency { get; set; } = "EUR";

    public DateOnly InvoiceDate { get; set; }

    public string TaxTreatment { get; set; } = string.Empty;

    public decimal NetAmount { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal GrossAmount { get; set; }

    public decimal PostingAdjustment { get; set; }

    public bool ManualOverrideApplied { get; set; }

    public string CalculatorVersion { get; set; } = "3.2";

    public List<TaxResultLine> Lines { get; set; } = new();
}

public sealed class TaxResultLine
{
    public int LineNumber { get; set; }

    public string Description { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal RatePercent { get; set; }

    public decimal NetAmount { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal GrossAmount { get; set; }

    public string TreatmentCode { get; set; } = string.Empty;
}
