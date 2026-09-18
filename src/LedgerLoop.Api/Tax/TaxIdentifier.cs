using System.Text.RegularExpressions;

namespace LedgerLoop.Api.Tax;

public static class TaxIdentifier
{
    public static readonly string[] UnionCountries = { "FR", "DE", "ES", "IE", "NL", "IT", "BE", "PT" };

    private static readonly Dictionary<string, Regex> Patterns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["FR"] = new Regex("^FR[0-9]{11}$", RegexOptions.Compiled),
        ["DE"] = new Regex("^DE[0-9]{9}$", RegexOptions.Compiled),
        ["ES"] = new Regex("^ES[A-Z][0-9]{7}[A-Z0-9]$", RegexOptions.Compiled),
        ["GB"] = new Regex("^GB[0-9]{9}$", RegexOptions.Compiled),
        ["IE"] = new Regex("^IE[0-9]{7}[A-Z]{1,2}$", RegexOptions.Compiled),
        ["NL"] = new Regex("^NL[0-9]{9}B[0-9]{2}$", RegexOptions.Compiled),
        ["IT"] = new Regex("^IT[0-9]{11}$", RegexOptions.Compiled),
        ["BE"] = new Regex("^BE[0-9]{10}$", RegexOptions.Compiled),
        ["PT"] = new Regex("^PT[0-9]{9}$", RegexOptions.Compiled)
    };

    public static bool IsInUnion(string countryCode) =>
        UnionCountries.Contains(countryCode, StringComparer.OrdinalIgnoreCase);

    public static bool IsWellFormed(string countryCode, string? taxId)
    {
        if (string.IsNullOrWhiteSpace(taxId))
        {
            return false;
        }

        var normalised = taxId.Replace(" ", string.Empty).ToUpperInvariant();

        if (!normalised.StartsWith(countryCode.ToUpperInvariant(), StringComparison.Ordinal))
        {
            return false;
        }

        return Patterns.TryGetValue(countryCode, out var pattern) && pattern.IsMatch(normalised);
    }
}
