package com.depigb.procurement.domain.model;

import java.math.BigDecimal;
import java.time.Instant;
import java.util.Objects;
import java.util.UUID;

public record GoodsReceipt(
        UUID id,
        UUID purchaseOrderId,
        UUID purchaseOrderLineId,
        BigDecimal quantity,
        String idempotencyKey,
        ReceiptStatus status,
        String inventoryOperationId,
        int attemptCount,
        String lastError,
        String createdBy,
        Instant createdAt,
        Instant updatedAt,
        Instant appliedAt,
        long version) {

    public GoodsReceipt {
        Objects.requireNonNull(id, "id is required");
        Objects.requireNonNull(purchaseOrderId, "purchaseOrderId is required");
        Objects.requireNonNull(purchaseOrderLineId, "purchaseOrderLineId is required");
        Objects.requireNonNull(quantity, "quantity is required");
        Objects.requireNonNull(idempotencyKey, "idempotencyKey is required");
        Objects.requireNonNull(status, "status is required");
        Objects.requireNonNull(createdBy, "createdBy is required");
        Objects.requireNonNull(createdAt, "createdAt is required");
        Objects.requireNonNull(updatedAt, "updatedAt is required");
        if (quantity.signum() <= 0) {
            throw new IllegalArgumentException("quantity must be positive");
        }
        if (idempotencyKey.isBlank()) {
            throw new IllegalArgumentException("idempotencyKey must not be blank");
        }
        if (createdBy.isBlank()) {
            throw new IllegalArgumentException("createdBy must not be blank");
        }
        if (attemptCount < 0) {
            throw new IllegalArgumentException("attemptCount must not be negative");
        }
        if (version < 0) {
            throw new IllegalArgumentException("version must not be negative");
        }
    }
}
