# PROC-002 — Acceptance Criteria

Source: [`docs/scope/scope.md`](../../../../../../docs/scope/scope.md), PROC-002.

| ID | Criterion | How to verify | Status |
|---|---|---|---|
| AC-002-1 | A clean database can be migrated from empty state. | Drop/recreate `procurement_db`, start the service, confirm Flyway applies V1 and the app starts. | [x] |
| AC-002-2 | Purchase orders require a supplier ID, creator, status, and timestamps. | Inspect `purchase_orders`; insert with each null field in a test and expect failure. | [x] |
| AC-002-3 | Order lines require a product ID and positive ordered quantity. | Insert zero/negative quantity and null product; expect constraint failure. | [x] |
| AC-002-4 | Receipts store one line, quantity, idempotency key, status, retry data, and Inventory operation ID. | Inspect `goods_receipts` columns and round-trip a receipt through the repository. | [x] |
| AC-002-5 | Duplicate idempotency keys are rejected by the database. | Insert two receipts with the same key; second insert fails. | [x] |
| AC-002-6 | The migration is applied and the live schema is inspected in PostgreSQL. | Run `\d+ purchase_orders`, `\d+ goods_receipts`, `\d+ event_publications` and capture output. | [x] |

## Additional checks from the plan

- [x] `NUMERIC(19,3)` is used for all quantities.
- [x] UUID primary keys are used.
- [x] Timestamps are `TIMESTAMPTZ`.
- [x] `version` columns exist for optimistic locking.
- [x] `event_publications` is created in V1.
- [x] JPA runs with `ddl-auto: validate`, not `update`.

## Evidence required

1. Migration console output.
2. `psql` schema inspection output.
3. Constraint/repository test output.
4. Full build output.
