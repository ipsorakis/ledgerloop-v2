import { useEffect, useMemo, useState } from 'react';
import { api } from '../api/client';
import type { Customer, DraftLineInput, LineCategory, PriceMode, TaxResult } from '../api/types';
import { estimateDocument } from '../tax/estimate';
import { categoryLabels } from '../tax/rates';
import { amount } from '../format';

interface Props {
  customers: Customer[];
  onCreated: (invoiceId: string) => Promise<void> | void;
}

const categories = Object.keys(categoryLabels) as LineCategory[];

const emptyLine = (lineNumber: number): DraftLineInput => ({
  lineNumber,
  description: '',
  category: 'ProfessionalServices',
  quantity: 1,
  unitPrice: 0,
  lineDiscountPercent: 0
});

export function DraftEditor({ customers, onCreated }: Props) {
  const [customerId, setCustomerId] = useState('');
  const [invoiceDate, setInvoiceDate] = useState(new Date().toISOString().slice(0, 10));
  const [priceMode, setPriceMode] = useState<PriceMode>('Exclusive');
  const [invoiceDiscountPercent, setInvoiceDiscountPercent] = useState(0);
  const [manualTaxAmount, setManualTaxAmount] = useState<string>('');
  const [notes, setNotes] = useState('');
  const [lines, setLines] = useState<DraftLineInput[]>([
    { ...emptyLine(1), description: 'Advisory day', unitPrice: 750 }
  ]);
  const [serverTotals, setServerTotals] = useState<TaxResult | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    if (!customerId && customers.length > 0) {
      setCustomerId(customers[0].id);
    }
  }, [customers, customerId]);

  const customer = customers.find((c) => c.id === customerId);

  const draft = useMemo(
    () => ({
      customerId,
      invoiceDate,
      priceMode,
      invoiceDiscountPercent,
      manualTaxAmount: manualTaxAmount === '' ? null : Number(manualTaxAmount),
      notes: notes === '' ? null : notes,
      lines
    }),
    [customerId, invoiceDate, priceMode, invoiceDiscountPercent, manualTaxAmount, notes, lines]
  );

  const estimate = useMemo(
    () =>
      estimateDocument(draft, {
        countryCode: customer?.countryCode ?? 'FR',
        taxId: customer?.taxId,
        taxOverride: customer?.taxOverride
      }),
    [draft, customer]
  );

  useEffect(() => {
    setServerTotals(null);
  }, [draft]);

  function updateLine(index: number, patch: Partial<DraftLineInput>) {
    setLines((current) => current.map((line, i) => (i === index ? { ...line, ...patch } : line)));
  }

  async function confirmWithLedger() {
    if (!customerId) {
      return;
    }

    setBusy(true);
    try {
      setServerTotals(await api.previewDraft(draft));
      setError(null);
    } catch (problem) {
      setError((problem as Error).message);
    } finally {
      setBusy(false);
    }
  }

  async function save() {
    setBusy(true);
    try {
      const created = await api.createDraft(draft);
      await onCreated(created.id);
      setError(null);
    } catch (problem) {
      setError((problem as Error).message);
    } finally {
      setBusy(false);
    }
  }

  return (
    <div>
      <div className="panel">
        <h2>New draft</h2>
        <div style={{ display: 'flex', gap: 16, flexWrap: 'wrap' }}>
          <label>
            Customer
            <select
              aria-label="Customer"
              value={customerId}
              onChange={(event) => setCustomerId(event.target.value)}
            >
              {customers.map((option) => (
                <option key={option.id} value={option.id}>
                  {option.name} ({option.countryCode})
                </option>
              ))}
            </select>
          </label>
          <label>
            Document date
            <input
              aria-label="Document date"
              type="date"
              value={invoiceDate}
              onChange={(event) => setInvoiceDate(event.target.value)}
            />
          </label>
          <label>
            Pricing
            <select
              aria-label="Pricing"
              value={priceMode}
              onChange={(event) => setPriceMode(event.target.value as PriceMode)}
            >
              <option value="Exclusive">Exclusive</option>
              <option value="Inclusive">Inclusive</option>
            </select>
          </label>
          <label>
            Invoice discount %
            <input
              aria-label="Invoice discount %"
              type="number"
              step="0.01"
              value={invoiceDiscountPercent}
              onChange={(event) => setInvoiceDiscountPercent(Number(event.target.value))}
            />
          </label>
          <label>
            Manual tax amount
            <input
              aria-label="Manual tax amount"
              type="number"
              step="0.01"
              value={manualTaxAmount}
              onChange={(event) => setManualTaxAmount(event.target.value)}
            />
          </label>
          <label>
            Notes
            <input aria-label="Notes" value={notes} onChange={(event) => setNotes(event.target.value)} />
          </label>
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
          {lines.map((line, index) => {
            const estimated = estimate.lines[index];
            return (
              <tr key={line.lineNumber}>
                <td>{line.lineNumber}</td>
                <td>
                  <input
                    aria-label={`Description ${line.lineNumber}`}
                    value={line.description}
                    onChange={(event) => updateLine(index, { description: event.target.value })}
                  />
                </td>
                <td>
                  <select
                    aria-label={`Category ${line.lineNumber}`}
                    value={line.category}
                    onChange={(event) =>
                      updateLine(index, { category: event.target.value as LineCategory })
                    }
                  >
                    {categories.map((category) => (
                      <option key={category} value={category}>
                        {categoryLabels[category]}
                      </option>
                    ))}
                  </select>
                </td>
                <td className="numeric">
                  <input
                    aria-label={`Quantity ${line.lineNumber}`}
                    type="number"
                    step="0.01"
                    value={line.quantity}
                    onChange={(event) => updateLine(index, { quantity: Number(event.target.value) })}
                  />
                </td>
                <td className="numeric">
                  <input
                    aria-label={`Unit price ${line.lineNumber}`}
                    type="number"
                    step="0.01"
                    value={line.unitPrice}
                    onChange={(event) => updateLine(index, { unitPrice: Number(event.target.value) })}
                  />
                </td>
                <td className="numeric">
                  <input
                    aria-label={`Line discount ${line.lineNumber}`}
                    type="number"
                    step="0.01"
                    value={line.lineDiscountPercent}
                    onChange={(event) =>
                      updateLine(index, { lineDiscountPercent: Number(event.target.value) })
                    }
                  />
                </td>
                <td className="numeric">{amount(estimated?.ratePercent ?? 0)}</td>
                <td className="numeric">{amount(estimated?.netAmount ?? 0)}</td>
                <td className="numeric">{amount(estimated?.taxAmount ?? 0)}</td>
              </tr>
            );
          })}
        </tbody>
      </table>

      <p>
        <button
          type="button"
          onClick={() => setLines((current) => [...current, emptyLine(current.length + 1)])}
        >
          Add line
        </button>
      </p>

      <div className="panel">
        <h3>Estimated totals</h3>
        <p className="muted">Treatment {estimate.treatment}</p>
        <div className="totals">
          <div>
            <span>Net</span>
            <span data-testid="estimate-net">{amount(estimate.netAmount)}</span>
          </div>
          <div>
            <span>Tax</span>
            <span data-testid="estimate-tax">{amount(estimate.taxAmount)}</span>
          </div>
          <div>
            <span>Gross</span>
            <span data-testid="estimate-gross">{amount(estimate.grossAmount)}</span>
          </div>
        </div>
        {serverTotals && (
          <>
            <h3>Ledger totals</h3>
            <div className="totals">
              <div>
                <span>Net</span>
                <span data-testid="ledger-net">{amount(serverTotals.netAmount)}</span>
              </div>
              <div>
                <span>Tax</span>
                <span data-testid="ledger-tax">{amount(serverTotals.taxAmount)}</span>
              </div>
              <div>
                <span>Gross</span>
                <span data-testid="ledger-gross">{amount(serverTotals.grossAmount)}</span>
              </div>
            </div>
          </>
        )}
        {error && <p className="error">{error}</p>}
        <p>
          <button type="button" disabled={busy} onClick={confirmWithLedger}>
            Check with ledger
          </button>{' '}
          <button className="primary" type="button" disabled={busy || !customerId} onClick={save}>
            Save draft
          </button>
        </p>
      </div>
    </div>
  );
}
