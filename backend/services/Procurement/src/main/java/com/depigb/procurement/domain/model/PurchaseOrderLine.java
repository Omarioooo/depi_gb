package com.depigb.procurement.domain.model;

import java.math.BigDecimal;
import java.time.Instant;
import java.util.Objects;
import java.util.UUID;

public record PurchaseOrderLine(
        UUID id,
        UUID purchaseOrderId,
        UUID productId,
        BigDecimal orderedQuantity,
        BigDecimal receivedQuantity,
        Instant createdAt,
        Instant updatedAt,
        long version) {

    public PurchaseOrderLine {
        Objects.requireNonNull(id, "id is required");
        Objects.requireNonNull(purchaseOrderId, "purchaseOrderId is required");
        Objects.requireNonNull(productId, "productId is required");
        Objects.requireNonNull(orderedQuantity, "orderedQuantity is required");
        Objects.requireNonNull(receivedQuantity, "receivedQuantity is required");
        Objects.requireNonNull(createdAt, "createdAt is required");
        Objects.requireNonNull(updatedAt, "updatedAt is required");
        if (orderedQuantity.signum() <= 0) {
            throw new IllegalArgumentException("orderedQuantity must be positive");
        }
        if (receivedQuantity.signum() < 0) {
            throw new IllegalArgumentException("receivedQuantity must not be negative");
        }
        if (receivedQuantity.compareTo(orderedQuantity) > 0) {
            throw new IllegalArgumentException("receivedQuantity must not exceed orderedQuantity");
        }
        if (version < 0) {
            throw new IllegalArgumentException("version must not be negative");
        }
    }
}
