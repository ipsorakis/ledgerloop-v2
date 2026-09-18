-- Applied by the PostgreSQL entrypoint on a fresh volume. The ledger stores
-- posting timestamps in UTC and the reporting extracts assume the same.
ALTER DATABASE ledgerloop SET timezone TO 'UTC';
