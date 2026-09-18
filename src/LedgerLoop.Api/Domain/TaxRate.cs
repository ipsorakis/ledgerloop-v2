namespace LedgerLoop.Api.Domain;

public class TaxRate
{
    public int Id { get; set; }

    public string CountryCode { get; set; } = string.Empty;

    public RateKind RateKind { get; set; }

    public decimal RatePercent { get; set; }

    public DateOnly EffectiveFrom { get; set; }

    public DateOnly? EffectiveTo { get; set; }
}
