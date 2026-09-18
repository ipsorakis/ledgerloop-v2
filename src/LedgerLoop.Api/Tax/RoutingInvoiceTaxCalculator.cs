using LedgerLoop.Api.Contracts;
using LedgerLoop.Api.Domain;

namespace LedgerLoop.Api.Tax;

/// <summary>
/// Seam in front of the document calculators. French domestic documents are
/// identified by <see cref="IsFrenchDomestic"/>; both branches are still
/// handled by the legacy <see cref="InvoiceTaxCalculator"/>.
/// </summary>
public sealed class RoutingInvoiceTaxCalculator : IInvoiceTaxCalculator
{
    private readonly InvoiceTaxCalculator _legacy;
    private readonly TaxSettings _settings;

    public RoutingInvoiceTaxCalculator(InvoiceTaxCalculator legacy, TaxSettings settings)
    {
        _legacy = legacy;
        _settings = settings;
    }

    public TaxResult Calculate(Invoice invoice) =>
        IsFrenchDomestic(invoice) ? _legacy.Calculate(invoice) : _legacy.Calculate(invoice);

    public bool IsFrenchDomestic(Invoice invoice)
    {
        var customer = invoice.Customer;

        return customer is not null
               && string.Equals(_settings.SellerCountry, "FR", StringComparison.OrdinalIgnoreCase)
               && CustomerTaxProfile.Resolve(customer, _settings.SellerCountry) == TreatmentCodes.Domestic;
    }
}
