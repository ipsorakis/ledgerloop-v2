using LedgerLoop.Api.Domain;

namespace LedgerLoop.Api.Tax;

public static class TreatmentCodes
{
    public const string Domestic = "DOMESTIC";
    public const string ReverseCharge = "REVERSE_CHARGE";
    public const string Exempt = "EXEMPT";
    public const string Export = "EXPORT";
}

public static class CustomerTaxProfile
{
    private static readonly string[] ExemptFlags = { "1", "EXEMPT", "EXEMPT_V2" };

    private static readonly string[] ForcedUnionFlags = { "2", "RC_FORCE_1" };

    public static string Resolve(Customer customer, string sellerCountry)
    {
        var flag = (customer.TaxOverride ?? string.Empty).Trim().ToUpperInvariant();
        var domestic = string.Equals(customer.CountryCode, sellerCountry, StringComparison.OrdinalIgnoreCase);

        if (ExemptFlags.Contains(flag))
        {
            return TreatmentCodes.Exempt;
        }

        if (ForcedUnionFlags.Contains(flag) && !domestic && TaxIdentifier.IsInUnion(customer.CountryCode))
        {
            return TreatmentCodes.ReverseCharge;
        }

        if (domestic)
        {
            return TreatmentCodes.Domestic;
        }

        if (TaxIdentifier.IsInUnion(customer.CountryCode))
        {
            return TaxIdentifier.IsWellFormed(customer.CountryCode, customer.TaxId)
                ? TreatmentCodes.ReverseCharge
                : TreatmentCodes.Domestic;
        }

        return TreatmentCodes.Export;
    }
}
