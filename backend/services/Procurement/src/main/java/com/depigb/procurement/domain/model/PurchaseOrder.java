package com.depigb.procurement.domain.model;

import java.time.Instant;
import java.util.List;
import java.util.Objects;
import java.util.UUID;

public record PurchaseOrder(
        UUID id,
        UUID supplierId,
        PurchaseOrderStatus status,
        String createdBy,
        Instant createdAt,
        Instant updatedAt,
        Instant submittedAt,
        Instant approvedAt,
        Instant cancelledAt,
        Instant completedAt,
        long version,
        List<PurchaseOrderLine> lines) {

    public PurchaseOrder {
        Objects.requireNonNull(id, "id is required");
        Objects.requireNonNull(supplierId, "supplierId is required");
        Objects.requireNonNull(status, "status is required");
        Objects.requireNonNull(createdBy, "createdBy is required");
        Objects.requireNonNull(createdAt, "createdAt is required");
        Objects.requireNonNull(updatedAt, "updatedAt is required");
        lines = List.copyOf(Objects.requireNonNull(lines, "lines are required"));
        if (createdBy.isBlank()) {
            throw new IllegalArgumentException("createdBy must not be blank");
        }
        if (lines.isEmpty()) {
            throw new IllegalArgumentException("at least one line is required");
        }
        if (lines.stream().anyMatch(line -> !id.equals(line.purchaseOrderId()))) {
            throw new IllegalArgumentException("every line must belong to the order");
        }
        if (version < 0) {
            throw new IllegalArgumentException("version must not be negative");
        }
    }
}
