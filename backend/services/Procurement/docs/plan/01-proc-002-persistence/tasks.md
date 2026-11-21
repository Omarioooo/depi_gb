# PROC-002 — Tasks

## Prepare

- [x] Confirm D2, D3, D4, D6, D7 in [`../decisions.md`](../decisions.md).
- [x] Create branch `feat/procurement/proc-002-persistence` (user-required branch format).
- [x] Start a local PostgreSQL 16 container.
- [x] Run the baseline `./gradlew clean test`.

## Configuration

- [x] Add `spring.datasource` settings backed by `PROCUREMENT_DB_URL`,
      `PROCUREMENT_DB_USERNAME`, and `PROCUREMENT_DB_PASSWORD`.
- [x] Add JPA settings: `ddl-auto: validate`, open-in-view disabled, UTC timezone.
- [x] Add Flyway settings with `locations: classpath:db/migration`.
- [x] Remove `DataSourceAutoConfiguration` from the exclusion list.
- [x] Add a `local` profile or default values that work with the runbook container.

## Schema

- [x] Write `V1__init.sql` with `purchase_orders`.
- [x] Add `purchase_order_lines` with FK, quantity checks, and a unique
      `(purchase_order_id, product_id)` constraint.
- [x] Add `goods_receipts` with unique `idempotency_key`, status check, retry
      columns, and Inventory operation ID.
- [x] Add `event_publications` with unique `event_id`, status check, and retry
      columns.
- [x] Add indexes for supplier/status/date queries, receipt lookups, and pending
      publication scans.
- [x] Verify the migration on an empty database.

## Domain

- [x] Add `PurchaseOrderStatus` with `DRAFT`, `SUBMITTED`, `APPROVED`,
      `PARTIALLY_RECEIVED`, `COMPLETED`, `CANCELLED`.
- [x] Add `ReceiptStatus` with `PENDING`, `APPLIED`, `FAILED`.
- [x] Add `PurchaseOrder`, `PurchaseOrderLine`, and `GoodsReceipt` domain models
      with no Spring or JPA imports.
- [x] Add basic invariants to the domain models (non-null IDs, positive quantities,
      received quantity not above ordered quantity).

## Persistence adapters

- [x] Add `PurchaseOrderJpaEntity`, `PurchaseOrderLineJpaEntity`,
      `GoodsReceiptJpaEntity`, and `EventPublicationJpaEntity`.
- [x] Add Spring Data repositories.
- [x] Add mappers between domain models and JPA entities.
- [x] Add `PurchaseOrderRepositoryPort` and its adapter.
- [x] Add `GoodsReceiptRepositoryPort` and its adapter.
- [x] Add adapter tests for round-trip mapping and optimistic locking.

## Tests

- [x] Flyway migration test against a clean PostgreSQL container.
- [x] Repository test: save and load an order with lines.
- [x] Repository test: duplicate idempotency key is rejected.
- [x] Repository test: zero/negative quantities are rejected.
- [x] Repository test: received quantity above ordered quantity is rejected.
- [x] Optimistic lock test for concurrent order updates.

## Documentation

- [x] Add the new environment variables to the service `README.md`.
- [x] Note the schema ownership and migration command in the README.
- [x] Record evidence in [`evidence.md`](./evidence.md).

## Review

- [x] `./gradlew clean build` passes.
- [x] Schema matches [`schema.md`](./schema.md).
- [x] No API or use case code leaked into this sprint.
- [ ] Diff reviewed and accepted.
