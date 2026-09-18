export type PriceMode = 'Exclusive' | 'Inclusive';

export type LineCategory =
  | 'StandardGoods'
  | 'ProfessionalServices'
  | 'DigitalServices'
  | 'Shipping'
  | 'PrintedBooks'
  | 'MedicalSupplies'
  | 'FoodStaples';

export interface Customer {
  id: string;
  name: string;
  countryCode: string;
  taxId?: string | null;
  taxOverride?: string | null;
  currency: string;
  contactEmail?: string | null;
  taxTreatment: string;
}

export interface TaxResultLine {
  lineNumber: number;
  description: string;
  category: string;
  quantity: number;
  unitPrice: number;
  ratePercent: number;
  netAmount: number;
  taxAmount: number;
  grossAmount: number;
  treatmentCode: string;
}

export interface TaxResult {
  documentNumber: string;
  documentKind: string;
  currency: string;
  invoiceDate: string;
  taxTreatment: string;
  netAmount: number;
  taxAmount: number;
  grossAmount: number;
  postingAdjustment: number;
  manualOverrideApplied: boolean;
  calculatorVersion: string;
  lines: TaxResultLine[];
}

export interface InvoiceSummary {
  id: string;
  invoiceNumber: string;
  customerName: string;
  documentKind: string;
  status: string;
  invoiceDate: string;
  currency: string;
  netAmount: number;
  taxAmount: number;
  grossAmount: number;
  taxTreatment: string;
}

export interface InvoiceLine {
  lineNumber: number;
  description: string;
  category: string;
  quantity: number;
  unitPrice: number;
  lineDiscountPercent: number;
}

export interface InvoiceDetail {
  id: string;
  invoiceNumber: string;
  customerId: string;
  customerName: string;
  customerCountry: string;
  documentKind: string;
  status: string;
  priceMode: string;
  invoiceDate: string;
  currency: string;
  invoiceDiscountPercent: number;
  manualTaxAmount?: number | null;
  notes?: string | null;
  postingAdjustment: number;
  postedAt?: string | null;
  originalInvoiceId?: string | null;
  totals: TaxResult;
  lines: InvoiceLine[];
}

export interface DraftLineInput {
  lineNumber?: number;
  description: string;
  category: LineCategory;
  quantity: number;
  unitPrice: number;
  lineDiscountPercent: number;
}

export interface DraftInvoiceInput {
  customerId: string;
  invoiceDate: string;
  priceMode: PriceMode;
  currency?: string;
  invoiceDiscountPercent: number;
  manualTaxAmount?: number | null;
  notes?: string | null;
  lines: DraftLineInput[];
}
