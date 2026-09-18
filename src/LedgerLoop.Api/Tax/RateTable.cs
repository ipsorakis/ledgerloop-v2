using LedgerLoop.Api.Domain;

namespace LedgerLoop.Api.Tax;

public sealed record RateTableEntry(string CountryCode, RateKind RateKind, decimal RatePercent, DateOnly EffectiveFrom, DateOnly? EffectiveTo);

public interface IRateTable
{
    IReadOnlyList<RateTableEntry> Entries { get; }
}

/// <summary>
/// Fallback rate table. The billing database is the source of truth in the API,
/// but the pricing tools and the fixture harness run without a database.
/// </summary>
public sealed class StaticRateTable : IRateTable
{
    public static readonly DateOnly Epoch = new(2015, 1, 1);

    public static readonly StaticRateTable Instance = new();

    public IReadOnlyList<RateTableEntry> Entries { get; } = new List<RateTableEntry>
    {
        new("FR", RateKind.Standard, 20.0m, Epoch, null),
        new("FR", RateKind.Reduced, 10.0m, Epoch, null),
        new("FR", RateKind.SuperReduced, 2.1m, Epoch, null),
        new("DE", RateKind.Standard, 19.0m, Epoch, null),
        new("DE", RateKind.Reduced, 7.0m, Epoch, null),
        new("DE", RateKind.SuperReduced, 5.0m, Epoch, null),
        new("ES", RateKind.Standard, 21.0m, Epoch, null),
        new("ES", RateKind.Reduced, 10.0m, Epoch, null),
        new("ES", RateKind.SuperReduced, 4.0m, Epoch, null),
        new("GB", RateKind.Standard, 20.0m, Epoch, null),
        new("GB", RateKind.Reduced, 5.0m, Epoch, null),
        new("GB", RateKind.SuperReduced, 0.0m, Epoch, null)
    };
}
