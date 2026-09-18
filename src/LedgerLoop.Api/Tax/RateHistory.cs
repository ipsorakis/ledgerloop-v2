using LedgerLoop.Api.Domain;

namespace LedgerLoop.Api.Tax;

/// <summary>
/// Rates that were in force before the 2023 ledger revision. The billing table
/// only carries the figures used for new documents, so reprints and late
/// corrections of older periods are resolved from here first.
/// </summary>
public static class RateHistory
{
    public static readonly DateOnly ReducedRateRevision = new(2023, 1, 1);

    private static readonly (string Country, RateKind Kind, decimal Percent, DateOnly Until)[] Superseded =
    {
        ("FR", RateKind.Reduced, 7.0m, ReducedRateRevision),
        ("ES", RateKind.Reduced, 8.0m, new DateOnly(2021, 4, 1))
    };

    public static decimal? Lookup(string countryCode, RateKind kind, DateOnly asOf)
    {
        foreach (var entry in Superseded)
        {
            if (string.Equals(entry.Country, countryCode, StringComparison.OrdinalIgnoreCase)
                && entry.Kind == kind
                && asOf < entry.Until)
            {
                return entry.Percent;
            }
        }

        return null;
    }
}
