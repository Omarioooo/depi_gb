package com.depigb.procurement.adapter.out.persistence;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.FetchType;
import jakarta.persistence.Id;
import jakarta.persistence.JoinColumn;
import jakarta.persistence.ManyToOne;
import jakarta.persistence.Table;
import jakarta.persistence.Version;
import java.math.BigDecimal;
import java.time.Instant;
import java.util.UUID;

@Entity
@Table(name = "purchase_order_lines")
public class PurchaseOrderLineJpaEntity {
    @Id
    private UUID id;

    @ManyToOne(fetch = FetchType.LAZY, optional = false)
    @JoinColumn(name = "purchase_order_id", nullable = false)
    private PurchaseOrderJpaEntity purchaseOrder;

    @Column(name = "product_id", nullable = false)
    private UUID productId;

    @Column(name = "ordered_quantity", nullable = false, precision = 19, scale = 3)
    private BigDecimal orderedQuantity;

    @Column(name = "received_quantity", nullable = false, precision = 19, scale = 3)
    private BigDecimal receivedQuantity;

    @Column(name = "created_at", nullable = false)
    private Instant createdAt;

    @Column(name = "updated_at", nullable = false)
    private Instant updatedAt;

    @Version
    @Column(nullable = false)
    private long version;

    protected PurchaseOrderLineJpaEntity() {}

    public PurchaseOrderLineJpaEntity(
            UUID id,
            UUID productId,
            BigDecimal orderedQuantity,
            BigDecimal receivedQuantity,
            Instant createdAt,
            Instant updatedAt,
            long version) {
        this.id = id;
        this.productId = productId;
        this.orderedQuantity = orderedQuantity;
        this.receivedQuantity = receivedQuantity;
        this.createdAt = createdAt;
        this.updatedAt = updatedAt;
        this.version = version;
    }

    void setPurchaseOrder(PurchaseOrderJpaEntity purchaseOrder) { this.purchaseOrder = purchaseOrder; }

    public UUID getId() { return id; }
    public PurchaseOrderJpaEntity getPurchaseOrder() { return purchaseOrder; }
    public UUID getProductId() { return productId; }
    public BigDecimal getOrderedQuantity() { return orderedQuantity; }
    public BigDecimal getReceivedQuantity() { return receivedQuantity; }
    public Instant getCreatedAt() { return createdAt; }
    public Instant getUpdatedAt() { return updatedAt; }
    public long getVersion() { return version; }
}
