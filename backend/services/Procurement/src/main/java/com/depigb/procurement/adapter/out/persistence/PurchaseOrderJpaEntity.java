package com.depigb.procurement.adapter.out.persistence;

import com.depigb.procurement.domain.model.PurchaseOrderStatus;
import jakarta.persistence.CascadeType;
import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.EnumType;
import jakarta.persistence.Enumerated;
import jakarta.persistence.FetchType;
import jakarta.persistence.Id;
import jakarta.persistence.OneToMany;
import jakarta.persistence.Table;
import jakarta.persistence.Version;
import java.time.Instant;
import java.util.ArrayList;
import java.util.List;
import java.util.UUID;

@Entity
@Table(name = "purchase_orders")
public class PurchaseOrderJpaEntity {
    @Id
    private UUID id;

    @Column(name = "supplier_id", nullable = false)
    private UUID supplierId;

    @Enumerated(EnumType.STRING)
    @Column(nullable = false, length = 32)
    private PurchaseOrderStatus status;

    @Column(name = "created_by", nullable = false)
    private String createdBy;

    @Column(name = "created_at", nullable = false)
    private Instant createdAt;

    @Column(name = "updated_at", nullable = false)
    private Instant updatedAt;

    @Column(name = "submitted_at")
    private Instant submittedAt;

    @Column(name = "approved_at")
    private Instant approvedAt;

    @Column(name = "cancelled_at")
    private Instant cancelledAt;

    @Column(name = "completed_at")
    private Instant completedAt;

    @Version
    @Column(nullable = false)
    private long version;

    @OneToMany(mappedBy = "purchaseOrder", cascade = CascadeType.ALL, orphanRemoval = true, fetch = FetchType.LAZY)
    private List<PurchaseOrderLineJpaEntity> lines = new ArrayList<>();

    protected PurchaseOrderJpaEntity() {}

    public PurchaseOrderJpaEntity(
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
            long version) {
        this.id = id;
        this.supplierId = supplierId;
        this.status = status;
        this.createdBy = createdBy;
        this.createdAt = createdAt;
        this.updatedAt = updatedAt;
        this.submittedAt = submittedAt;
        this.approvedAt = approvedAt;
        this.cancelledAt = cancelledAt;
        this.completedAt = completedAt;
        this.version = version;
    }

    public void addLine(PurchaseOrderLineJpaEntity line) {
        lines.add(line);
        line.setPurchaseOrder(this);
    }

    public UUID getId() { return id; }
    public UUID getSupplierId() { return supplierId; }
    public PurchaseOrderStatus getStatus() { return status; }
    public String getCreatedBy() { return createdBy; }
    public Instant getCreatedAt() { return createdAt; }
    public Instant getUpdatedAt() { return updatedAt; }
    public Instant getSubmittedAt() { return submittedAt; }
    public Instant getApprovedAt() { return approvedAt; }
    public Instant getCancelledAt() { return cancelledAt; }
    public Instant getCompletedAt() { return completedAt; }
    public long getVersion() { return version; }
    public List<PurchaseOrderLineJpaEntity> getLines() { return lines; }
}
