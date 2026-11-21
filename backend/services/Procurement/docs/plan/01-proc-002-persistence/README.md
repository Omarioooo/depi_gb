# Sprint 01 — PROC-002: Persistence and Schema

**Status:** review
**Phase:** Foundation
**Depends on:** PROC-001
**Unblocks:** every later sprint

## Goal

Replace the in-memory-free foundation with a real PostgreSQL model: Flyway
migrations, JPA entities, repositories, and optimistic locking. This sprint creates
the database contract every later slice depends on.

## Why this order

Every business slice persists orders, lines, and receipts. Changing the schema after
PROC-004 is expensive, so define it once and review it before writing use cases.

## In scope

- Datasource, JPA, and Flyway configuration from environment variables.
- `V1__init.sql` covering purchase orders, lines, receipts, and event publications.
- Domain enums and pure domain models.
- JPA entities, mappers, repositories, and repository ports/adapters.
- Constraint and repository tests against PostgreSQL.
- Removal of the `DataSourceAutoConfiguration` exclusion.

## Out of scope

- REST endpoints.
- Authorization.
- Use case services.
- Receiving flow and Inventory calls.
- Event publishing.

## Deliverables

```text
src/main/resources/application.yml
src/main/resources/db/migration/V1__init.sql
domain/model/PurchaseOrder.java
domain/model/PurchaseOrderLine.java
domain/model/GoodsReceipt.java
domain/model/PurchaseOrderStatus.java
domain/model/ReceiptStatus.java
application/port/out/PurchaseOrderRepositoryPort.java
application/port/out/GoodsReceiptRepositoryPort.java
adapter/out/persistence/PurchaseOrderJpaEntity.java
adapter/out/persistence/PurchaseOrderLineJpaEntity.java
adapter/out/persistence/GoodsReceiptJpaEntity.java
adapter/out/persistence/EventPublicationJpaEntity.java
adapter/out/persistence/*Repository.java
adapter/out/persistence/*Mapper.java
src/test/java/.../persistence/*Test.java
```

## Key design notes

- Domain models stay free of JPA annotations; persistence entities live in the
  adapter and map explicitly.
- Quantities are `NUMERIC(19,3)` in the database and `BigDecimal` in Java.
- `purchase_orders.version`, `purchase_order_lines.version`,
  `goods_receipts.version`, and `event_publications.version` support optimistic
  locking.
- `goods_receipts.idempotency_key` is unique at the database level.
- Create `event_publications` now so PROC-009 does not need a second migration.
- Status values are protected by check constraints as well as enums.

## Definition of done

- A clean PostgreSQL database migrates with Flyway.
- Orders, lines, receipts, and publication records save and load through the
  repository ports.
- Duplicate idempotency keys and invalid quantities are rejected by the database.
- `./gradlew clean test` passes.
- Evidence is recorded in `evidence.md`.

## Files in this sprint

- [`tasks.md`](./tasks.md)
- [`acceptance.md`](./acceptance.md)
- [`runbook.md`](./runbook.md)
- [`schema.md`](./schema.md)
- [`evidence.md`](./evidence.md)

## Next sprint

Sprint 02 — PROC-003 Authorization boundary. It can run in parallel after the
schema direction is agreed.
