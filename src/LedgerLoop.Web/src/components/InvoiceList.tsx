import type { InvoiceSummary } from '../api/types';
import { amount } from '../format';

interface Props {
  invoices: InvoiceSummary[];
  onSelect: (id: string) => void;
}

export function InvoiceList({ invoices, onSelect }: Props) {
  if (invoices.length === 0) {
    return <p className="muted">No documents yet.</p>;
  }

  return (
    <table>
      <thead>
        <tr>
          <th>Document</th>
          <th>Customer</th>
          <th>Date</th>
          <th>Status</th>
          <th>Treatment</th>
          <th className="numeric">Net</th>
          <th className="numeric">Tax</th>
          <th className="numeric">Gross</th>
          <th />
        </tr>
      </thead>
      <tbody>
        {invoices.map((invoice) => (
          <tr key={invoice.id}>
            <td>{invoice.invoiceNumber}</td>
            <td>{invoice.customerName}</td>
            <td>{invoice.invoiceDate}</td>
            <td>{invoice.status}</td>
            <td>{invoice.taxTreatment}</td>
            <td className="numeric">{amount(invoice.netAmount)}</td>
            <td className="numeric">{amount(invoice.taxAmount)}</td>
            <td className="numeric">
              {amount(invoice.grossAmount)} {invoice.currency}
            </td>
            <td>
              <button type="button" onClick={() => onSelect(invoice.id)}>
                Open
              </button>
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
