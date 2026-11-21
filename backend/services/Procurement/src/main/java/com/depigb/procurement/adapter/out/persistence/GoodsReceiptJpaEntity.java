package com.depigb.procurement.adapter.out.persistence;

import com.depigb.procurement.domain.model.ReceiptStatus;
import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.EnumType;
import jakarta.persistence.Enumerated;
import jakarta.persistence.Id;
import jakarta.persistence.Table;
import jakarta.persistence.Version;
import java.math.BigDecimal;
import java.time.Instant;
import java.util.UUID;

@Entity
@Table(name = "goods_receipts")
public class GoodsReceiptJpaEntity {
    @Id
    private UUID id;

    @Column(name = "purchase_order_id", nullable = false)
    private UUID purchaseOrderId;

    @Column(name = "purchase_order_line_id", nullable = false)
    private UUID purchaseOrderLineId;

    @Column(nullable = false, precision = 19, scale = 3)
    private BigDecimal quantity;

    @Column(name = "idempotency_key", nullable = false, unique = true)
    private String idempotencyKey;

    @Enumerated(EnumType.STRING)
    @Column(nullable = false, length = 32)
    private ReceiptStatus status;

    @Column(name = "inventory_operation_id")
    private String inventoryOperationId;

    @Column(name = "attempt_count", nullable = false)
    private int attemptCount;

    @Column(name = "last_error")
    private String lastError;

    @Column(name = "created_by", nullable = false)
    private String createdBy;

    @Column(name = "created_at", nullable = false)
    private Instant createdAt;

    @Column(name = "updated_at", nullable = false)
    private Instant updatedAt;

    @Column(name = "applied_at")
    private Instant appliedAt;

    @Version
    @Column(nullable = false)
    private long version;

    protected GoodsReceiptJpaEntity() {}

    public GoodsReceiptJpaEntity(
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
        this.id = id;
        this.purchaseOrderId = purchaseOrderId;
        this.purchaseOrderLineId = purchaseOrderLineId;
        this.quantity = quantity;
        this.idempotencyKey = idempotencyKey;
        this.status = status;
        this.inventoryOperationId = inventoryOperationId;
        this.attemptCount = attemptCount;
        this.lastError = lastError;
        this.createdBy = createdBy;
        this.createdAt = createdAt;
        this.updatedAt = updatedAt;
        this.appliedAt = appliedAt;
        this.version = version;
    }

    public UUID getId() { return id; }
    public UUID getPurchaseOrderId() { return purchaseOrderId; }
    public UUID getPurchaseOrderLineId() { return purchaseOrderLineId; }
    public BigDecimal getQuantity() { return quantity; }
    public String getIdempotencyKey() { return idempotencyKey; }
    public ReceiptStatus getStatus() { return status; }
    public String getInventoryOperationId() { return inventoryOperationId; }
    public int getAttemptCount() { return attemptCount; }
    public String getLastError() { return lastError; }
    public String getCreatedBy() { return createdBy; }
    public Instant getCreatedAt() { return createdAt; }
    public Instant getUpdatedAt() { return updatedAt; }
    public Instant getAppliedAt() { return appliedAt; }
    public long getVersion() { return version; }
}
