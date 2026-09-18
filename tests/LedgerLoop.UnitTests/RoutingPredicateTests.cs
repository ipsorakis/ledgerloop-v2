using FluentAssertions;
using LedgerLoop.Api.Domain;
using LedgerLoop.Api.Tax;
using LedgerLoop.UnitTests.Fixtures;

namespace LedgerLoop.UnitTests;

public class RoutingPredicateTests
{
    private static RoutingInvoiceTaxCalculator Router(string sellerCountry = "FR")
    {
        var settings = new TaxSettings { SellerCountry = sellerCountry };

        return new RoutingInvoiceTaxCalculator(new InvoiceTaxCalculator(RateResolver.Default, settings), settings);
    }

    private static Invoice Invoice(string country, string? taxId = null, string? taxOverride = null) => new()
    {
        InvoiceNumber = "ROUTE-0001",
        InvoiceDate = new DateOnly(2024, 1, 1),
        Customer = new Customer
        {
            Name = "Test",
            CountryCode = country,
            TaxId = taxId,
            TaxOverride = taxOverride
        }
    };

    [Fact]
    public void French_domestic_documents_match() =>
        Router().IsFrenchDomestic(Invoice("FR", "FR12345678901")).Should().BeTrue();

    [Fact]
    public void Reverse_charged_documents_do_not_match() =>
        Router().IsFrenchDomestic(Invoice("DE", "DE123456789")).Should().BeFalse();

    [Fact]
    public void Exports_do_not_match() =>
        Router().IsFrenchDomestic(Invoice("GB", "GB123456789")).Should().BeFalse();

    [Fact]
    public void Exempt_documents_do_not_match() =>
        Router().IsFrenchDomestic(Invoice("FR", "FR12345678901", "EXEMPT")).Should().BeFalse();

    [Theory]
    [InlineData("DE")]
    [InlineData("ES")]
    [InlineData("GB")]
    public void Other_seller_countries_never_match(string sellerCountry) =>
        Router(sellerCountry).IsFrenchDomestic(Invoice(sellerCountry, "FR12345678901")).Should().BeFalse();

    /// <summary>
    /// The predicate follows the seller country and the resolved treatment only, so a
    /// union customer whose identifier is not usable is in scope even though the
    /// customer itself is not French.
    /// </summary>
    [Fact]
    public void Union_customers_without_a_usable_identifier_are_in_scope()
    {
        var fixture = GoldenInvoiceLibrary.ById("06-eu-customer-without-usable-tax-id");

        fixture.Customer.CountryCode.Should().Be("DE");
        Router().IsFrenchDomestic(fixture.ToInvoice()).Should().BeTrue();
    }

    [Fact]
    public void Documents_on_both_branches_keep_the_legacy_result()
    {
        var router = Router();

        foreach (var fixture in GoldenInvoiceLibrary.All)
        {
            router.Calculate(fixture.ToInvoice()).Should()
                .BeEquivalentTo(InvoiceTaxCalculator.Default.Calculate(fixture.ToInvoice()), fixture.Id);
        }
    }
}
