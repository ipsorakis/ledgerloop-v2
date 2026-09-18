import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { InvoiceList } from './InvoiceList';
import type { InvoiceSummary } from '../api/types';

const invoices: InvoiceSummary[] = [
  {
    id: '11111111-1111-1111-1111-111111111111',
    invoiceNumber: 'INV-2024-01001',
    customerName: 'Cabinet Marchand',
    documentKind: 'Invoice',
    status: 'Posted',
    invoiceDate: '2024-02-01',
    currency: 'EUR',
    netAmount: 299.97,
    taxAmount: 59.99,
    grossAmount: 359.96,
    taxTreatment: 'DOMESTIC'
  }
];

describe('InvoiceList', () => {
  it('lists the ledger figures of every document', () => {
    render(<InvoiceList invoices={invoices} onSelect={() => undefined} />);

    expect(screen.getByText('INV-2024-01001')).toBeInTheDocument();
    expect(screen.getByText('59.99')).toBeInTheDocument();
    expect(screen.getByText('DOMESTIC')).toBeInTheDocument();
  });

  it('opens the selected document', async () => {
    const onSelect = vi.fn();
    render(<InvoiceList invoices={invoices} onSelect={onSelect} />);

    await userEvent.click(screen.getByRole('button', { name: /open/i }));

    expect(onSelect).toHaveBeenCalledWith('11111111-1111-1111-1111-111111111111');
  });

  it('explains an empty ledger', () => {
    render(<InvoiceList invoices={[]} onSelect={() => undefined} />);

    expect(screen.getByText('No documents yet.')).toBeInTheDocument();
  });
});
