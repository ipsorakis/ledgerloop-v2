using LedgerLoop.Api.Contracts;
using LedgerLoop.Api.Domain;

namespace LedgerLoop.Api.Tax;

public interface IInvoiceTaxCalculator
{
    TaxResult Calculate(Invoice invoice);
}

/// <summary>
/// Authoritative document calculator used by the preview and posting paths.
/// </summary>
public sealed class InvoiceTaxCalculator : IInvoiceTaxCalculator
{
    private readonly IRateResolver _rates;
    private readonly TaxSettings _settings;

    public InvoiceTaxCalculator(IRateResolver rates, TaxSettings settings)
    {
        _rates = rates;
        _settings = settings;
    }

    public static InvoiceTaxCalculator Default { get; } = new(RateResolver.Default, new TaxSettings());

    public TaxResult Calculate(Invoice invoice)
    {
        var customer = invoice.Customer
            ?? throw new InvalidOperationException($"Invoice {invoice.InvoiceNumber} has no customer loaded.");

        var treatment = CustomerTaxProfile.Resolve(customer, _settings.SellerCountry);
        var taxable = treatment == TreatmentCodes.Domestic;
        var sign = invoice.DocumentKind == DocumentKind.CreditNote ? -1m : 1m;

        var result = new TaxResult
        {
            DocumentNumber = invoice.InvoiceNumber,
            DocumentKind = invoice.DocumentKind.ToString(),
            Currency = string.IsNullOrWhiteSpace(invoice.Currency) ? _settings.DefaultCurrency : invoice.Currency,
            InvoiceDate = invoice.InvoiceDate,
            TaxTreatment = treatment
        };

        foreach (var line in invoice.Lines.OrderBy(l => l.LineNumber))
        {
            var ratePercent = taxable
                ? _rates.Resolve(_settings.SellerCountry, line.Category, invoice.InvoiceDate)
                : 0m;

            var gross = line.Quantity * line.UnitPrice;
            gross -= Money.Percent(gross, line.LineDiscountPercent);
            gross -= Money.Percent(gross, invoice.InvoiceDiscountPercent);
            gross *= sign;

            decimal net;
            decimal tax;

            if (invoice.PriceMode == PriceMode.Inclusive)
            {
                var grossRounded = Money.Round(gross);
                net = Money.Round(grossRounded / (1m + ratePercent / 100m));
                tax = grossRounded - net;
                gross = grossRounded;
            }
            else
            {
                net = Money.Round(gross);
                tax = Money.Round(Money.Percent(net, ratePercent));
                gross = net + tax;
            }

            result.Lines.Add(new TaxResultLine
            {
                LineNumber = line.LineNumber,
                Description = line.Description,
                Category = line.Category.ToString(),
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                RatePercent = ratePercent,
                NetAmount = net,
                TaxAmount = tax,
                GrossAmount = gross,
                TreatmentCode = treatment
            });
        }

        result.NetAmount = result.Lines.Sum(l => l.NetAmount);
        result.TaxAmount = result.Lines.Sum(l => l.TaxAmount);

        if (invoice.ManualTaxAmount.HasValue)
        {
            result.TaxAmount = Money.Round(invoice.ManualTaxAmount.Value * sign);
            result.ManualOverrideApplied = true;
        }

        result.GrossAmount = result.NetAmount + result.TaxAmount;

        return result;
    }
}
