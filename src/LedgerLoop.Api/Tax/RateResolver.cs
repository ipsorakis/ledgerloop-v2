using LedgerLoop.Api.Domain;

namespace LedgerLoop.Api.Tax;

public interface IRateResolver
{
    decimal Resolve(string countryCode, LineCategory category, DateOnly asOf);
}

public sealed class RateResolver : IRateResolver
{
    private readonly IRateTable _table;

    public RateResolver(IRateTable table)
    {
        _table = table;
    }

    public static RateResolver Default { get; } = new(StaticRateTable.Instance);

    public decimal Resolve(string countryCode, LineCategory category, DateOnly asOf)
    {
        var kind = TaxCategoryMap.RateKindFor(category);

        var historical = RateHistory.Lookup(countryCode, kind, asOf);
        if (historical.HasValue)
        {
            return historical.Value;
        }

        var candidates = _table.Entries
            .Where(e => string.Equals(e.CountryCode, countryCode, StringComparison.OrdinalIgnoreCase)
                        && e.RateKind == kind
                        && e.EffectiveFrom <= asOf
                        && (e.EffectiveTo is null || asOf < e.EffectiveTo))
            .OrderByDescending(e => e.EffectiveFrom)
            .ToList();

        if (candidates.Count == 0)
        {
            throw new InvalidOperationException($"No {kind} rate configured for {countryCode} on {asOf:yyyy-MM-dd}.");
        }

        return candidates[0].RatePercent;
    }
}
