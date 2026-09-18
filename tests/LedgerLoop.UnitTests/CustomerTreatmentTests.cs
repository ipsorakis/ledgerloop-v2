using FluentAssertions;
using LedgerLoop.Api.Domain;
using LedgerLoop.Api.Tax;

namespace LedgerLoop.UnitTests;

public class CustomerTreatmentTests
{
    private static Customer Customer(string country, string? taxId = null, string? taxOverride = null) => new()
    {
        Name = "Test",
        CountryCode = country,
        TaxId = taxId,
        TaxOverride = taxOverride
    };

    [Fact]
    public void Domestic_customers_are_taxed_at_home_rates() =>
        CustomerTaxProfile.Resolve(Customer("FR", "FR12345678901"), "FR").Should().Be(TreatmentCodes.Domestic);

    [Fact]
    public void Union_customers_with_a_usable_identifier_are_reverse_charged() =>
        CustomerTaxProfile.Resolve(Customer("DE", "DE123456789"), "FR").Should().Be(TreatmentCodes.ReverseCharge);

    [Fact]
    public void Union_customers_without_a_usable_identifier_stay_domestic() =>
        CustomerTaxProfile.Resolve(Customer("DE", "DE1234"), "FR").Should().Be(TreatmentCodes.Domestic);

    [Fact]
    public void Customers_outside_the_union_are_treated_as_exports() =>
        CustomerTaxProfile.Resolve(Customer("GB", "GB123456789"), "FR").Should().Be(TreatmentCodes.Export);

    [Theory]
    [InlineData("1")]
    [InlineData("EXEMPT")]
    [InlineData("exempt_v2")]
    public void Recorded_exemptions_win_over_the_country_rules(string flag) =>
        CustomerTaxProfile.Resolve(Customer("FR", "FR12345678901", flag), "FR").Should().Be(TreatmentCodes.Exempt);

    [Fact]
    public void An_unrelated_flag_value_leaves_the_country_rules_in_place() =>
        CustomerTaxProfile.Resolve(Customer("FR", "FR12345678901", "STD"), "FR").Should().Be(TreatmentCodes.Domestic);

    [Theory]
    [InlineData("DE", "DE123456789", true)]
    [InlineData("DE", "DE12345678", false)]
    [InlineData("FR", "FR12345678901", true)]
    [InlineData("FR", "12345678901", false)]
    [InlineData("ES", "ESB12345678", true)]
    [InlineData("ES", null, false)]
    public void Identifiers_are_checked_syntactically(string country, string? taxId, bool wellFormed) =>
        TaxIdentifier.IsWellFormed(country, taxId).Should().Be(wellFormed);
}
