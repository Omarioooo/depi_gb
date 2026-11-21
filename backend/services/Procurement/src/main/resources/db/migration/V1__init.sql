CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE purchase_orders (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    supplier_id UUID NOT NULL,
    status VARCHAR(32) NOT NULL,
    created_by VARCHAR(255) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    submitted_at TIMESTAMPTZ,
    approved_at TIMESTAMPTZ,
    cancelled_at TIMESTAMPTZ,
    completed_at TIMESTAMPTZ,
    version BIGINT NOT NULL DEFAULT 0,
    CONSTRAINT ck_purchase_orders_status CHECK (
        status IN ('DRAFT', 'SUBMITTED', 'APPROVED', 'PARTIALLY_RECEIVED', 'COMPLETED', 'CANCELLED')
    )
);

CREATE INDEX idx_purchase_orders_supplier_created_at
    ON purchase_orders (supplier_id, created_at DESC);
CREATE INDEX idx_purchase_orders_status_created_at
    ON purchase_orders (status, created_at DESC);
CREATE INDEX idx_purchase_orders_created_at
    ON purchase_orders (created_at DESC);

CREATE TABLE purchase_order_lines (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    purchase_order_id UUID NOT NULL REFERENCES purchase_orders(id) ON DELETE CASCADE,
    product_id UUID NOT NULL,
    ordered_quantity NUMERIC(19,3) NOT NULL,
    received_quantity NUMERIC(19,3) NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    version BIGINT NOT NULL DEFAULT 0,
    CONSTRAINT uk_purchase_order_lines_order_product UNIQUE (purchase_order_id, product_id),
    CONSTRAINT ck_purchase_order_lines_ordered_quantity CHECK (ordered_quantity > 0),
    CONSTRAINT ck_purchase_order_lines_received_quantity CHECK (
        received_quantity >= 0 AND received_quantity <= ordered_quantity
    )
);

CREATE INDEX idx_purchase_order_lines_order_id
    ON purchase_order_lines (purchase_order_id);

CREATE TABLE goods_receipts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    purchase_order_id UUID NOT NULL REFERENCES purchase_orders(id),
    purchase_order_line_id UUID NOT NULL REFERENCES purchase_order_lines(id),
    quantity NUMERIC(19,3) NOT NULL,
    idempotency_key VARCHAR(255) NOT NULL,
    status VARCHAR(32) NOT NULL,
    inventory_operation_id VARCHAR(255),
    attempt_count INTEGER NOT NULL DEFAULT 0,
    last_error TEXT,
    created_by VARCHAR(255) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    applied_at TIMESTAMPTZ,
    version BIGINT NOT NULL DEFAULT 0,
    CONSTRAINT uk_goods_receipts_idempotency_key UNIQUE (idempotency_key),
    CONSTRAINT ck_goods_receipts_quantity CHECK (quantity > 0),
    CONSTRAINT ck_goods_receipts_status CHECK (status IN ('PENDING', 'APPLIED', 'FAILED')),
    CONSTRAINT ck_goods_receipts_attempt_count CHECK (attempt_count >= 0)
);

CREATE INDEX idx_goods_receipts_order_created_at
    ON goods_receipts (purchase_order_id, created_at DESC);
CREATE INDEX idx_goods_receipts_line_id
    ON goods_receipts (purchase_order_line_id);
CREATE INDEX idx_goods_receipts_status_created_at
    ON goods_receipts (status, created_at);

CREATE TABLE event_publications (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    event_id UUID NOT NULL,
    event_type VARCHAR(100) NOT NULL,
    aggregate_type VARCHAR(50) NOT NULL,
    aggregate_id UUID NOT NULL,
    payload JSONB NOT NULL,
    status VARCHAR(32) NOT NULL,
    attempt_count INTEGER NOT NULL DEFAULT 0,
    last_error TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    published_at TIMESTAMPTZ,
    version BIGINT NOT NULL DEFAULT 0,
    CONSTRAINT uk_event_publications_event_id UNIQUE (event_id),
    CONSTRAINT ck_event_publications_status CHECK (status IN ('PENDING', 'PUBLISHED', 'FAILED')),
    CONSTRAINT ck_event_publications_attempt_count CHECK (attempt_count >= 0)
);

CREATE INDEX idx_event_publications_status_created_at
    ON event_publications (status, created_at);
CREATE INDEX idx_event_publications_aggregate
    ON event_publications (aggregate_type, aggregate_id);
