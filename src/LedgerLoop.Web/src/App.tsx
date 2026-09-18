import { useCallback, useEffect, useState } from 'react';
import { api } from './api/client';
import type { Customer, InvoiceSummary } from './api/types';
import { InvoiceList } from './components/InvoiceList';
import { InvoiceDetail } from './components/InvoiceDetail';
import { DraftEditor } from './components/DraftEditor';

type View = 'list' | 'new';

export function App() {
  const [view, setView] = useState<View>('list');
  const [invoices, setInvoices] = useState<InvoiceSummary[]>([]);
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [selected, setSelected] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const reload = useCallback(async () => {
    try {
      const [documents, people] = await Promise.all([api.listInvoices(), api.listCustomers()]);
      setInvoices(documents);
      setCustomers(people);
      setError(null);
    } catch (problem) {
      setError((problem as Error).message);
    }
  }, []);

  useEffect(() => {
    void reload();
  }, [reload]);

  return (
    <>
      <header>
        <h1>LedgerLoop</h1>
        <span>Invoice preparation and posting &middot; synthetic tax figures</span>
      </header>
      <nav>
        <button
          type="button"
          aria-pressed={view === 'list'}
          onClick={() => {
            setView('list');
            setSelected(null);
          }}
        >
          Invoices
        </button>
        <button type="button" aria-pressed={view === 'new'} onClick={() => setView('new')}>
          New draft
        </button>
      </nav>
      <main>
        {error && <p className="error">{error}</p>}
        {view === 'list' && !selected && (
          <InvoiceList invoices={invoices} onSelect={setSelected} />
        )}
        {view === 'list' && selected && (
          <InvoiceDetail
            invoiceId={selected}
            onBack={() => setSelected(null)}
            onChanged={reload}
          />
        )}
        {view === 'new' && (
          <DraftEditor
            customers={customers}
            onCreated={async (id) => {
              await reload();
              setView('list');
              setSelected(id);
            }}
          />
        )}
      </main>
    </>
  );
}
