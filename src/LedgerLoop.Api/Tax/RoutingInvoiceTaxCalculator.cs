using LedgerLoop.Api.Contracts;
using LedgerLoop.Api.Domain;

namespace LedgerLoop.Api.Tax;

/// <summary>
/// Seam in front of the document calculators. Currently every document is
/// handled by the legacy <see cref="InvoiceTaxCalculator"/>.
/// </summary>
public sealed class RoutingInvoiceTaxCalculator : IInvoiceTaxCalculator
{
    private readonly InvoiceTaxCalculator _legacy;

    public RoutingInvoiceTaxCalculator(InvoiceTaxCalculator legacy)
    {
        _legacy = legacy;
    }

    public TaxResult Calculate(Invoice invoice) => _legacy.Calculate(invoice);
}
