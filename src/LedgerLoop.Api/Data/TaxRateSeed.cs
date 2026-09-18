using LedgerLoop.Api.Domain;

namespace LedgerLoop.Api.Data;

/// <summary>
/// Billing rate table shipped with the schema. Finance maintains new rows here
/// through the migration pipeline.
/// </summary>
public static class TaxRateSeed
{
    private static readonly DateOnly Epoch = new(2015, 1, 1);

    public static TaxRate[] Rows { get; } =
    {
        new() { Id = 1, CountryCode = "FR", RateKind = RateKind.Standard, RatePercent = 20.0m, EffectiveFrom = Epoch },
        new() { Id = 2, CountryCode = "FR", RateKind = RateKind.Reduced, RatePercent = 10.0m, EffectiveFrom = Epoch },
        new() { Id = 3, CountryCode = "FR", RateKind = RateKind.SuperReduced, RatePercent = 2.1m, EffectiveFrom = Epoch },
        new() { Id = 4, CountryCode = "DE", RateKind = RateKind.Standard, RatePercent = 19.0m, EffectiveFrom = Epoch },
        new() { Id = 5, CountryCode = "DE", RateKind = RateKind.Reduced, RatePercent = 7.0m, EffectiveFrom = Epoch },
        new() { Id = 6, CountryCode = "DE", RateKind = RateKind.SuperReduced, RatePercent = 5.0m, EffectiveFrom = Epoch },
        new() { Id = 7, CountryCode = "ES", RateKind = RateKind.Standard, RatePercent = 21.0m, EffectiveFrom = Epoch },
        new() { Id = 8, CountryCode = "ES", RateKind = RateKind.Reduced, RatePercent = 10.0m, EffectiveFrom = Epoch },
        new() { Id = 9, CountryCode = "ES", RateKind = RateKind.SuperReduced, RatePercent = 4.0m, EffectiveFrom = Epoch },
        new() { Id = 10, CountryCode = "GB", RateKind = RateKind.Standard, RatePercent = 20.0m, EffectiveFrom = Epoch },
        new() { Id = 11, CountryCode = "GB", RateKind = RateKind.Reduced, RatePercent = 5.0m, EffectiveFrom = Epoch },
        new() { Id = 12, CountryCode = "GB", RateKind = RateKind.SuperReduced, RatePercent = 0.0m, EffectiveFrom = Epoch }
    };
}
