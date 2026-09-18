import fs from 'node:fs';
import path from 'node:path';
import type { LineCategory, PriceMode } from '../api/types';

export interface GoldenInvoice {
  id: string;
  description: string;
  customer: {
    name: string;
    countryCode: string;
    taxId: string | null;
    taxOverride: string | null;
    currency: string;
  };
  invoice: {
    documentNumber: string;
    documentKind: 'Invoice' | 'CreditNote';
    invoiceDate: string;
    priceMode: PriceMode;
    currency: string;
    invoiceDiscountPercent: number;
    manualTaxAmount: number | null;
    lines: {
      lineNumber: number;
      description: string;
      category: LineCategory;
      quantity: number;
      unitPrice: number;
      lineDiscountPercent: number;
    }[];
  };
  expected: {
    taxTreatment: string;
    netAmount: number;
    taxAmount: number;
    grossAmount: number;
    postingAdjustment: number;
    postedGrossAmount: number;
    editorEstimate: {
      treatment: string;
      netAmount: number;
      taxAmount: number;
      grossAmount: number;
    };
  };
}

const directory = path.resolve(__dirname, '../../../../fixtures/golden-invoices');

export const goldenInvoices: GoldenInvoice[] = fs
  .readdirSync(directory)
  .filter((file) => file.endsWith('.json'))
  .sort()
  .map((file) => JSON.parse(fs.readFileSync(path.join(directory, file), 'utf8')) as GoldenInvoice);
