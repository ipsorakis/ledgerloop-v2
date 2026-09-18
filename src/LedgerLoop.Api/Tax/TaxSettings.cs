namespace LedgerLoop.Api.Tax;

public class TaxSettings
{
    public const string SectionName = "Tax";

    public string SellerCountry { get; set; } = "FR";

    public string SellerTaxId { get; set; } = "FR40303265045";

    public string DefaultCurrency { get; set; } = "EUR";

    /// <summary>
    /// Tolerance used when an older client posts its own line tax figures.
    /// </summary>
    public decimal ClientTaxTolerance { get; set; } = 0.02m;

    public string ManualOverrideRole { get; set; } = "accounting";
}
