# PROC-002 — Database Contract

`V1__init.sql` must create the objects below. This is the agreed contract for later
sprints; change it only through a new migration.

## Conventions

- Keys: `UUID PRIMARY KEY DEFAULT gen_random_uuid()`
- Quantities: `NUMERIC(19,3)`
- Timestamps: `TIMESTAMPTZ NOT NULL DEFAULT now()`
- Optimistic locking: `version BIGINT NOT NULL DEFAULT 0`
- Status columns: `VARCHAR(32)` with named check constraints
- No cross-service foreign keys; supplier/product IDs are opaque UUIDs

## purchase_orders

| Column | Type | Rules |
|---|---|---|
| `id` | UUID | PK, default generated |
| `supplier_id` | UUID | NOT NULL |
| `status` | VARCHAR(32) | NOT NULL, check in (`DRAFT`, `SUBMITTED`, `APPROVED`, `PARTIALLY_RECEIVED`, `COMPLETED`, `CANCELLED`) |
| `created_by` | VARCHAR(255) | NOT NULL |
| `created_at` | TIMESTAMPTZ | NOT NULL, default now() |
| `updated_at` | TIMESTAMPTZ | NOT NULL, default now() |
| `submitted_at` | TIMESTAMPTZ | NULL |
| `approved_at` | TIMESTAMPTZ | NULL |
| `cancelled_at` | TIMESTAMPTZ | NULL |
| `completed_at` | TIMESTAMPTZ | NULL |
| `version` | BIGINT | NOT NULL, default 0 |

Indexes:

- `(supplier_id, created_at DESC)`
- `(status, created_at DESC)`
- `(created_at DESC)`

## purchase_order_lines

| Column | Type | Rules |
|---|---|---|
| `id` | UUID | PK, default generated |
| `purchase_order_id` | UUID | NOT NULL, FK → `purchase_orders(id)` ON DELETE CASCADE |
| `product_id` | UUID | NOT NULL |
| `ordered_quantity` | NUMERIC(19,3) | NOT NULL, check `> 0` |
| `received_quantity` | NUMERIC(19,3) | NOT NULL, default 0, check `>= 0 AND <= ordered_quantity` |
| `created_at` | TIMESTAMPTZ | NOT NULL, default now() |
| `updated_at` | TIMESTAMPTZ | NOT NULL, default now() |
| `version` | BIGINT | NOT NULL, default 0 |

Constraints/indexes:

- Unique `(purchase_order_id, product_id)`
- Index on `purchase_order_id`

## goods_receipts

One row per receipt request. One request covers one line (D7).

| Column | Type | Rules |
|---|---|---|
| `id` | UUID | PK, default generated |
| `purchase_order_id` | UUID | NOT NULL, FK → `purchase_orders(id)` |
| `purchase_order_line_id` | UUID | NOT NULL, FK → `purchase_order_lines(id)` |
| `quantity` | NUMERIC(19,3) | NOT NULL, check `> 0` |
| `idempotency_key` | VARCHAR(255) | NOT NULL, UNIQUE |
| `status` | VARCHAR(32) | NOT NULL, check in (`PENDING`, `APPLIED`, `FAILED`) |
| `inventory_operation_id` | VARCHAR(255) | NULL |
| `attempt_count` | INTEGER | NOT NULL, default 0, check `>= 0` |
| `last_error` | TEXT | NULL |
| `created_by` | VARCHAR(255) | NOT NULL |
| `created_at` | TIMESTAMPTZ | NOT NULL, default now() |
| `updated_at` | TIMESTAMPTZ | NOT NULL, default now() |
| `applied_at` | TIMESTAMPTZ | NULL |
| `version` | BIGINT | NOT NULL, default 0 |

Indexes:

- `(purchase_order_id, created_at DESC)`
- `(purchase_order_line_id)`
- `(status, created_at)` for retry scans

## event_publications

Written in the same transaction as the business change; published after commit.

| Column | Type | Rules |
|---|---|---|
| `id` | UUID | PK, default generated |
| `event_id` | UUID | NOT NULL, UNIQUE |
| `event_type` | VARCHAR(100) | NOT NULL |
| `aggregate_type` | VARCHAR(50) | NOT NULL |
| `aggregate_id` | UUID | NOT NULL |
| `payload` | JSONB | NOT NULL |
| `status` | VARCHAR(32) | NOT NULL, check in (`PENDING`, `PUBLISHED`, `FAILED`) |
| `attempt_count` | INTEGER | NOT NULL, default 0, check `>= 0` |
| `last_error` | TEXT | NULL |
| `created_at` | TIMESTAMPTZ | NOT NULL, default now() |
| `published_at` | TIMESTAMPTZ | NULL |
| `version` | BIGINT | NOT NULL, default 0 |

Indexes:

- `(status, created_at)` for the retry scanner
- `(aggregate_type, aggregate_id)`

## Sample DDL sketch

The implementation may use a different order or naming, but the columns and
constraints must match.

```sql
CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE purchase_orders (
    id           UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    supplier_id  UUID         NOT NULL,
    status       VARCHAR(32)  NOT NULL,
    created_by   VARCHAR(255) NOT NULL,
    created_at   TIMESTAMPTZ  NOT NULL DEFAULT now(),
    updated_at   TIMESTAMPTZ  NOT NULL DEFAULT now(),
    submitted_at TIMESTAMPTZ,
    approved_at  TIMESTAMPTZ,
    cancelled_at TIMESTAMPTZ,
    completed_at TIMESTAMPTZ,
    version      BIGINT       NOT NULL DEFAULT 0,
    CONSTRAINT ck_purchase_orders_status CHECK (
        status IN ('DRAFT','SUBMITTED','APPROVED','PARTIALLY_RECEIVED','COMPLETED','CANCELLED')
    )
);
```

## Deliberate omissions

- No prices, currency, payments, or invoices.
- No supplier or product master data.
- No soft-delete column on orders; terminal orders remain queryable.
- No cross-database foreign keys.
