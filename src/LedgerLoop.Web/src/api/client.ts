import type {
  Customer,
  DraftInvoiceInput,
  InvoiceDetail,
  InvoiceSummary,
  TaxResult
} from './types';

const base = import.meta.env.VITE_API_BASE ?? '';

export const operatorRole = 'accounting';

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${base}${path}`, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      'X-LedgerLoop-Role': operatorRole,
      'X-LedgerLoop-User': 'web-ui',
      ...(init?.headers ?? {})
    }
  });

  if (!response.ok) {
    const problem = await response.json().catch(() => null);
    throw new Error(problem?.detail ?? problem?.title ?? `Request failed (${response.status})`);
  }

  return (await response.json()) as T;
}

export const api = {
  listCustomers: () => request<Customer[]>('/api/customers'),
  listInvoices: () => request<InvoiceSummary[]>('/api/invoices'),
  getInvoice: (id: string) => request<InvoiceDetail>(`/api/invoices/${id}`),
  previewDraft: (input: DraftInvoiceInput) =>
    request<TaxResult>('/api/invoices/preview', { method: 'POST', body: JSON.stringify(input) }),
  createDraft: (input: DraftInvoiceInput) =>
    request<InvoiceDetail>('/api/invoices', { method: 'POST', body: JSON.stringify(input) }),
  postInvoice: (id: string) => request<TaxResult>(`/api/invoices/${id}/post`, { method: 'POST' }),
  createCreditNote: (invoiceId: string, reason: string) =>
    request<InvoiceDetail>('/api/credit-notes', {
      method: 'POST',
      body: JSON.stringify({ invoiceId, reason, lineNumbers: [] })
    })
};
