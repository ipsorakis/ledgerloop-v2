-- Operations copy of the posting routine. The authoritative definition ships
-- with the EF Core migrations; this file is kept for DBA review and for
-- restoring the routine on a database that was cloned without it.
CREATE OR REPLACE FUNCTION ledgerloop_post_invoice(p_invoice_id uuid)
RETURNS numeric
LANGUAGE plpgsql
AS $func$
DECLARE
    v_invoice_date date;
    v_tax numeric(18,2);
    v_adjustment numeric(18,2) := 0;
BEGIN
    SELECT invoice_date, tax_amount
      INTO v_invoice_date, v_tax
      FROM invoices
     WHERE id = p_invoice_id;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Invoice % does not exist', p_invoice_id;
    END IF;

    IF v_invoice_date < DATE '2022-07-01' THEN
        v_adjustment := round(v_tax * 0.005, 2);
    END IF;

    UPDATE invoices
       SET posting_adjustment = v_adjustment,
           gross_amount = net_amount + tax_amount + v_adjustment,
           updated_at = now()
     WHERE id = p_invoice_id;

    INSERT INTO audit_events (invoice_id, event_type, detail, actor_role, created_at)
    VALUES (p_invoice_id, 'ledger.posted', 'adjustment ' || v_adjustment::text, 'ledger', now());

    RETURN v_adjustment;
END;
$func$;
