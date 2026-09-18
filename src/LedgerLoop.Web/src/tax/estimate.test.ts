import { describe, expect, it } from 'vitest';
import { estimateDocument, treatmentFor } from './estimate';
import { goldenInvoices } from '../test/goldenInvoices';

describe('editor treatment', () => {
  it('treats domestic customers as taxable', () => {
    expect(treatmentFor({ countryCode: 'FR', taxId: 'FR12345678901' })).toBe('DOMESTIC');
  });

  it('reverse charges union customers with a plausible identifier', () => {
    expect(treatmentFor({ countryCode: 'DE', taxId: 'DE123456789' })).toBe('REVERSE_CHARGE');
  });

  it('keeps union customers without an identifier on domestic rates', () => {
    expect(treatmentFor({ countryCode: 'DE', taxId: null })).toBe('DOMESTIC');
  });

  it('treats customers outside the union as exports', () => {
    expect(treatmentFor({ countryCode: 'GB', taxId: 'GB123456789' })).toBe('EXPORT');
  });

  it('honours the customer exemption flag', () => {
    expect(treatmentFor({ countryCode: 'FR', taxId: 'FR12345678901', taxOverride: 'EXEMPT' })).toBe('EXEMPT');
  });
});

describe('editor estimate', () => {
  it('shows tax immediately for a single standard-rate line', () => {
    const estimate = estimateDocument(
      {
        priceMode: 'Exclusive',
        lines: [{ category: 'ProfessionalServices', quantity: 3, unitPrice: 99.99 }]
      },
      { countryCode: 'FR', taxId: 'FR12345678901' }
    );

    expect(estimate.netAmount).toBe(299.97);
    expect(estimate.taxAmount).toBe(59.99);
    expect(estimate.grossAmount).toBe(359.96);
  });

  it('derives the net amount of inclusive prices from the gross amount', () => {
    const estimate = estimateDocument(
      {
        priceMode: 'Inclusive',
        lines: [{ category: 'ProfessionalServices', quantity: 1, unitPrice: 120 }]
      },
      { countryCode: 'FR', taxId: 'FR12345678901' }
    );

    expect(estimate.netAmount).toBe(100);
    expect(estimate.taxAmount).toBe(20);
  });

  it('reverses the amounts of a credit note', () => {
    const estimate = estimateDocument(
      {
        documentKind: 'CreditNote',
        priceMode: 'Exclusive',
        lines: [{ category: 'ProfessionalServices', quantity: 1, unitPrice: 320 }]
      },
      { countryCode: 'FR', taxId: 'FR12345678901' }
    );

    expect(estimate.netAmount).toBe(-320);
    expect(estimate.taxAmount).toBe(-64);
  });

  it('applies line and document discounts', () => {
    const estimate = estimateDocument(
      {
        priceMode: 'Exclusive',
        invoiceDiscountPercent: 10,
        lines: [{ category: 'ProfessionalServices', quantity: 1, unitPrice: 1000, lineDiscountPercent: 20 }]
      },
      { countryCode: 'FR', taxId: 'FR12345678901' }
    );

    expect(estimate.netAmount).toBe(720);
    expect(estimate.taxAmount).toBe(144);
  });

  it('shows a manually entered tax amount unchanged', () => {
    const estimate = estimateDocument(
      {
        priceMode: 'Exclusive',
        manualTaxAmount: 1150,
        lines: [{ category: 'ProfessionalServices', quantity: 1, unitPrice: 6240 }]
      },
      { countryCode: 'FR', taxId: 'FR12345678901' }
    );

    expect(estimate.taxAmount).toBe(1150);
    expect(estimate.grossAmount).toBe(7390);
  });

  it('shows no tax for an exempt customer', () => {
    const estimate = estimateDocument(
      {
        priceMode: 'Exclusive',
        lines: [{ category: 'ProfessionalServices', quantity: 2, unitPrice: 180 }]
      },
      { countryCode: 'FR', taxOverride: '1' }
    );

    expect(estimate.taxAmount).toBe(0);
    expect(estimate.grossAmount).toBe(360);
  });
});

describe('editor estimate against the golden documents', () => {
  it.each(goldenInvoices.map((fixture) => [fixture.id, fixture] as const))(
    '%s matches the estimate recorded with the document',
    (_id, fixture) => {
      const estimate = estimateDocument(
        {
          documentKind: fixture.invoice.documentKind,
          priceMode: fixture.invoice.priceMode,
          invoiceDiscountPercent: fixture.invoice.invoiceDiscountPercent,
          manualTaxAmount: fixture.invoice.manualTaxAmount,
          lines: fixture.invoice.lines
        },
        fixture.customer
      );

      expect(estimate.treatment).toBe(fixture.expected.editorEstimate.treatment);
      expect(estimate.netAmount).toBe(fixture.expected.editorEstimate.netAmount);
      expect(estimate.taxAmount).toBe(fixture.expected.editorEstimate.taxAmount);
      expect(estimate.grossAmount).toBe(fixture.expected.editorEstimate.grossAmount);
    }
  );

  it('accepts the one cent the editor differs from the ledger on food staples', () => {
    const fixture = goldenInvoices.find((f) => f.id === '15-one-cent-sensitive-total');
    expect(fixture).toBeDefined();

    const estimate = estimateDocument(
      {
        priceMode: fixture!.invoice.priceMode,
        lines: fixture!.invoice.lines
      },
      fixture!.customer
    );

    // The editor sums unrounded line tax and rounds once; the ledger rounds
    // every line. The ledger figure is the one that is stored.
    expect(fixture!.expected.taxAmount - estimate.taxAmount).toBeCloseTo(0.01, 10);
  });
});
