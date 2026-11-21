package com.depigb.procurement.adapter.out.persistence;

import com.depigb.procurement.domain.model.PurchaseOrder;
import com.depigb.procurement.domain.model.PurchaseOrderLine;
import org.springframework.stereotype.Component;

@Component
public class PurchaseOrderPersistenceMapper {
    public PurchaseOrderJpaEntity toEntity(PurchaseOrder order) {
        PurchaseOrderJpaEntity entity = new PurchaseOrderJpaEntity(
                order.id(),
                order.supplierId(),
                order.status(),
                order.createdBy(),
                order.createdAt(),
                order.updatedAt(),
                order.submittedAt(),
                order.approvedAt(),
                order.cancelledAt(),
                order.completedAt(),
                order.version());
        order.lines().stream().map(this::toLineEntity).forEach(entity::addLine);
        return entity;
    }

    public PurchaseOrder toDomain(PurchaseOrderJpaEntity entity) {
        return new PurchaseOrder(
                entity.getId(),
                entity.getSupplierId(),
                entity.getStatus(),
                entity.getCreatedBy(),
                entity.getCreatedAt(),
                entity.getUpdatedAt(),
                entity.getSubmittedAt(),
                entity.getApprovedAt(),
                entity.getCancelledAt(),
                entity.getCompletedAt(),
                entity.getVersion(),
                entity.getLines().stream().map(this::toLineDomain).toList());
    }

    private PurchaseOrderLineJpaEntity toLineEntity(PurchaseOrderLine line) {
        return new PurchaseOrderLineJpaEntity(
                line.id(),
                line.productId(),
                line.orderedQuantity(),
                line.receivedQuantity(),
                line.createdAt(),
                line.updatedAt(),
                line.version());
    }

    private PurchaseOrderLine toLineDomain(PurchaseOrderLineJpaEntity entity) {
        return new PurchaseOrderLine(
                entity.getId(),
                entity.getPurchaseOrder().getId(),
                entity.getProductId(),
                entity.getOrderedQuantity(),
                entity.getReceivedQuantity(),
                entity.getCreatedAt(),
                entity.getUpdatedAt(),
                entity.getVersion());
    }
}
