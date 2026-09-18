-- Month-end reconciliation used by the finance team. Compares the stored
-- document totals with the sum of the stored line figures.
SELECT i.invoice_number,
       i.invoice_date,
       i.tax_treatment,
       i.net_amount,
       i.tax_amount,
       i.posting_adjustment,
       i.gross_amount,
       SUM(l.net_amount) AS line_net,
       SUM(l.tax_amount) AS line_tax
  FROM invoices i
  JOIN invoice_lines l ON l.invoice_id = i.id
 WHERE i.status = 1
   AND i.invoice_date BETWEEN :from_date AND :to_date
 GROUP BY i.id, i.invoice_number, i.invoice_date, i.tax_treatment,
          i.net_amount, i.tax_amount, i.posting_adjustment, i.gross_amount
HAVING SUM(l.tax_amount) <> i.tax_amount
    OR SUM(l.net_amount) <> i.net_amount
 ORDER BY i.invoice_date;
