import type { LineCategory, PriceMode } from '../api/types';
import { ratePercentFor, sellerCountry } from './rates';

export interface EstimateCustomer {
  countryCode: string;
  taxId?: string | null;
  taxOverride?: string | null;
}

export interface EstimateLine {
  lineNumber?: number;
  description?: string;
  category: LineCategory;
  quantity: number;
  unitPrice: number;
  lineDiscountPercent?: number;
}

export interface EstimateInput {
  documentKind?: 'Invoice' | 'CreditNote';
  priceMode: PriceMode;
  invoiceDiscountPercent?: number;
  manualTaxAmount?: number | null;
  lines: EstimateLine[];
}

export interface EstimateLineResult {
  lineNumber: number;
  ratePercent: number;
  netAmount: number;
  taxAmount: number;
  grossAmount: number;
}

export interface Estimate {
  treatment: string;
  netAmount: number;
  taxAmount: number;
  grossAmount: number;
  lines: EstimateLineResult[];
}

const exemptFlags = ['1', 'EXEMPT', 'EXEMPT_V2'];
const unionCountries = ['FR', 'DE', 'ES', 'IE', 'NL', 'IT', 'BE', 'PT'];

function money(value: number): number {
  return Math.round(value * 100) / 100;
}

function looksLikeTaxId(country: string, taxId?: string | null): boolean {
  if (!taxId) {
    return false;
  }

  const cleaned = taxId.replace(/\s/g, '').toUpperCase();
  return cleaned.startsWith(country.toUpperCase()) && /^[A-Z]{2}[A-Z0-9]{9,}$/.test(cleaned);
}

export function treatmentFor(customer: EstimateCustomer): string {
  const flag = (customer.taxOverride ?? '').trim().toUpperCase();
  const country = customer.countryCode.toUpperCase();

  if (exemptFlags.includes(flag)) {
    return 'EXEMPT';
  }

  if (country === sellerCountry) {
    return 'DOMESTIC';
  }

  if (unionCountries.includes(country)) {
    return looksLikeTaxId(country, customer.taxId) ? 'REVERSE_CHARGE' : 'DOMESTIC';
  }

  return 'EXPORT';
}

/**
 * Editor estimate. Line figures are kept at full precision and the document
 * totals are rounded once, which keeps the on-screen figures stable while the
 * operator edits quantities.
 */
export function estimateDocument(input: EstimateInput, customer: EstimateCustomer): Estimate {
  const treatment = treatmentFor(customer);
  const taxable = treatment === 'DOMESTIC';
  const sign = input.documentKind === 'CreditNote' ? -1 : 1;
  const invoiceDiscount = input.invoiceDiscountPercent ?? 0;

  let net = 0;
  let tax = 0;
  const lines: EstimateLineResult[] = [];

  input.lines.forEach((line, index) => {
    const ratePercent = taxable ? ratePercentFor(sellerCountry, line.category) : 0;

    let amount = line.quantity * line.unitPrice;
    amount = amount - (amount * (line.lineDiscountPercent ?? 0)) / 100;
    amount = amount - (amount * invoiceDiscount) / 100;
    amount = amount * sign;

    let lineNet: number;
    let lineTax: number;

    if (input.priceMode === 'Inclusive') {
      lineNet = amount / (1 + ratePercent / 100);
      lineTax = amount - lineNet;
    } else {
      lineNet = amount;
      lineTax = (lineNet * ratePercent) / 100;
    }

    net += lineNet;
    tax += lineTax;

    lines.push({
      lineNumber: line.lineNumber ?? index + 1,
      ratePercent,
      netAmount: money(lineNet),
      taxAmount: money(lineTax),
      grossAmount: money(lineNet + lineTax)
    });
  });

  const netTotal = money(net);
  const taxTotal =
    input.manualTaxAmount === null || input.manualTaxAmount === undefined
      ? money(tax)
      : money(input.manualTaxAmount * sign);

  return {
    treatment,
    netAmount: netTotal,
    taxAmount: taxTotal,
    grossAmount: money(netTotal + taxTotal),
    lines
  };
}
