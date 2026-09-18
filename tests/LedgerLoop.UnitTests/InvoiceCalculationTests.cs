using FluentAssertions;
using LedgerLoop.Api.Domain;
using LedgerLoop.Api.Tax;

namespace LedgerLoop.UnitTests;

public class InvoiceCalculationTests
{
    private static Invoice Invoice(
        DocumentKind kind = DocumentKind.Invoice,
        PriceMode priceMode = PriceMode.Exclusive,
        decimal invoiceDiscount = 0m,
        decimal? manualTax = null,
        Customer? customer = null,
        params InvoiceLine[] lines)
    {
        customer ??= new Customer { Name = "Cabinet Marchand", CountryCode = "FR", TaxId = "FR12345678901" };

        var invoice = new Invoice
        {
            InvoiceNumber = "INV-TEST",
            Customer = customer,
            CustomerId = customer.Id,
            DocumentKind = kind,
            PriceMode = priceMode,
            InvoiceDate = new DateOnly(2024, 5, 1),
            Currency = "EUR",
            InvoiceDiscountPercent = invoiceDiscount,
            ManualTaxAmount = manualTax
        };

        foreach (var line in lines)
        {
            invoice.Lines.Add(line);
        }

        return invoice;
    }

    private static InvoiceLine Line(int number, decimal quantity, decimal unitPrice, LineCategory category = LineCategory.ProfessionalServices, decimal discount = 0m) => new()
    {
        LineNumber = number,
        Description = $"Line {number}",
        Category = category,
        Quantity = quantity,
        UnitPrice = unitPrice,
        LineDiscountPercent = discount
    };

    [Fact]
    public void Exclusive_pricing_rounds_the_tax_of_every_line()
    {
        var result = InvoiceTaxCalculator.Default.Calculate(
            Invoice(lines: new[] { Line(1, 1, 55m, LineCategory.FoodStaples), Line(2, 1, 55m, LineCategory.FoodStaples) }));

        result.Lines.Should().OnlyContain(line => line.TaxAmount == 1.16m);
        result.TaxAmount.Should().Be(2.32m);
        result.GrossAmount.Should().Be(112.32m);
    }

    [Fact]
    public void Inclusive_pricing_derives_the_net_amount_from_the_gross_amount()
    {
        var result = InvoiceTaxCalculator.Default.Calculate(
            Invoice(priceMode: PriceMode.Inclusive, lines: new[] { Line(1, 1, 120m) }));

        result.Lines[0].GrossAmount.Should().Be(120m);
        result.Lines[0].NetAmount.Should().Be(100m);
        result.Lines[0].TaxAmount.Should().Be(20m);
    }

    [Fact]
    public void Credit_notes_carry_negative_amounts()
    {
        var result = InvoiceTaxCalculator.Default.Calculate(
            Invoice(kind: DocumentKind.CreditNote, lines: new[] { Line(1, 1, 320m) }));

        result.NetAmount.Should().Be(-320m);
        result.TaxAmount.Should().Be(-64m);
        result.GrossAmount.Should().Be(-384m);
    }

    [Fact]
    public void Line_and_document_discounts_are_both_applied()
    {
        var result = InvoiceTaxCalculator.Default.Calculate(
            Invoice(invoiceDiscount: 10m, lines: new[] { Line(1, 1, 1000m, discount: 20m) }));

        result.NetAmount.Should().Be(720m);
        result.TaxAmount.Should().Be(144m);
    }

    [Fact]
    public void A_manual_tax_amount_replaces_the_calculated_figure()
    {
        var result = InvoiceTaxCalculator.Default.Calculate(
            Invoice(manualTax: 1150m, lines: new[] { Line(1, 1, 6240m) }));

        result.TaxAmount.Should().Be(1150m);
        result.ManualOverrideApplied.Should().BeTrue();
        result.GrossAmount.Should().Be(7390m);
    }

    [Fact]
    public void Exempt_customers_are_billed_without_tax()
    {
        var customer = new Customer { Name = "Charity", CountryCode = "FR", TaxOverride = "1" };

        var result = InvoiceTaxCalculator.Default.Calculate(
            Invoice(customer: customer, lines: new[] { Line(1, 2, 180m) }));

        result.TaxTreatment.Should().Be(TreatmentCodes.Exempt);
        result.TaxAmount.Should().Be(0m);
        result.Lines[0].RatePercent.Should().Be(0m);
    }

    [Fact]
    public void Documents_dated_before_the_reduced_rate_revision_use_the_older_rate()
    {
        var invoice = Invoice(lines: new[] { Line(1, 40, 12.50m, LineCategory.PrintedBooks) });
        invoice.InvoiceDate = new DateOnly(2022, 5, 9);

        var result = InvoiceTaxCalculator.Default.Calculate(invoice);

        result.Lines[0].RatePercent.Should().Be(7.0m);
        result.TaxAmount.Should().Be(35.00m);
    }
}
