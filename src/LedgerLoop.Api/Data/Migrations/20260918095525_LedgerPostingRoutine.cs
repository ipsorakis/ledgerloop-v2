using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LedgerLoop.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class LedgerPostingRoutine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE SEQUENCE IF NOT EXISTS document_number_seq START WITH 1000 INCREMENT BY 1;");

            migrationBuilder.Sql(@"
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

    -- Ledger periods closed before the 2022 migration carry the transitional
    -- rounding correction agreed with the auditors.
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
$func$;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS ledgerloop_post_invoice(uuid);");
            migrationBuilder.Sql("DROP SEQUENCE IF EXISTS document_number_seq;");
        }
    }
}
