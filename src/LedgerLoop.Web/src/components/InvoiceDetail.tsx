import { useCallback, useEffect, useState } from 'react';
import { api } from '../api/client';
import type { InvoiceDetail as Detail } from '../api/types';
import { amount, money } from '../format';

interface Props {
  invoiceId: string;
  onBack: () => void;
  onChanged: () => Promise<void> | void;
}

export function InvoiceDetail({ invoiceId, onBack, onChanged }: Props) {
  const [invoice, setInvoice] = useState<Detail | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const load = useCallback(async () => {
    try {
      setInvoice(await api.getInvoice(invoiceId));
      setError(null);
    } catch (problem) {
      setError((problem as Error).message);
    }
  }, [invoiceId]);

  useEffect(() => {
    void load();
  }, [load]);

  async function run(action: () => Promise<unknown>) {
    setBusy(true);
    try {
      await action();
      await load();
      await onChanged();
      setError(null);
    } catch (problem) {
      setError((problem as Error).message);
    } finally {
      setBusy(false);
    }
  }

  if (!invoice) {
    return (
      <div>
        <button type="button" onClick={onBack}>
          Back
        </button>
        {error ? <p className="error">{error}</p> : <p className="muted">Loading…</p>}
      </div>
    );
  }

  return (
    <div>
      <button type="button" onClick={onBack}>
        Back
      </button>
      <div className="panel">
        <h2>
          {invoice.invoiceNumber} &middot; {invoice.customerName}
        </h2>
        <p className="muted">
          {invoice.documentKind} · {invoice.status} · {invoice.invoiceDate} · {invoice.priceMode}{' '}
          pricing · {invoice.totals.taxTreatment}
          {invoice.totals.manualOverrideApplied ? ' · manual tax amount on file' : ''}
        </p>
        {invoice.notes && <p>{invoice.notes}</p>}
        {error && <p className="error">{error}</p>}
        <div className="totals">
          <div>
            <span>Net</span>
            {money(invoice.totals.netAmount, invoice.currency)}
          </div>
          <div>
            <span>Tax</span>
            {money(invoice.totals.taxAmount, invoice.currency)}
          </div>
          <div>
            <span>Posting adjustment</span>
            {money(invoice.totals.postingAdjustment, invoice.currency)}
          </div>
          <div>
            <span>Gross</span>
            {money(invoice.totals.grossAmount, invoice.currency)}
          </div>
        </div>
      </div>
      <table>
        <thead>
          <tr>
            <th>#</th>
            <th>Description</th>
            <th>Category</th>
            <th className="numeric">Qty</th>
            <th className="numeric">Unit price</th>
            <th className="numeric">Discount %</th>
            <th className="numeric">Rate %</th>
            <th className="numeric">Net</th>
            <th className="numeric">Tax</th>
          </tr>
        </thead>
        <tbody>
          {invoice.totals.lines.map((line) => (
            <tr key={line.lineNumber}>
              <td>{line.lineNumber}</td>
              <td>{line.description}</td>
              <td>{line.category}</td>
              <td className="numeric">{line.quantity}</td>
              <td className="numeric">{amount(line.unitPrice)}</td>
              <td className="numeric">
                {amount(
                  invoice.lines.find((l) => l.lineNumber === line.lineNumber)?.lineDiscountPercent ?? 0
                )}
              </td>
              <td className="numeric">{amount(line.ratePercent)}</td>
              <td className="numeric">{amount(line.netAmount)}</td>
              <td className="numeric">{amount(line.taxAmount)}</td>
            </tr>
          ))}
        </tbody>
      </table>
      <p style={{ marginTop: 16 }}>
        {invoice.status === 'Draft' && (
          <button
            className="primary"
            type="button"
            disabled={busy}
            onClick={() => run(() => api.postInvoice(invoice.id))}
          >
            Post document
          </button>
        )}{' '}
        {invoice.status === 'Posted' && invoice.documentKind === 'Invoice' && (
          <button
            type="button"
            disabled={busy}
            onClick={() => run(() => api.createCreditNote(invoice.id, 'Credited from the invoice view'))}
          >
            Create credit note
          </button>
        )}
      </p>
    </div>
  );
}
