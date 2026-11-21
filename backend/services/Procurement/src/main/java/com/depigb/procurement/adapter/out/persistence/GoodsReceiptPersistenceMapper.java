package com.depigb.procurement.adapter.out.persistence;

import com.depigb.procurement.domain.model.GoodsReceipt;
import org.springframework.stereotype.Component;

@Component
public class GoodsReceiptPersistenceMapper {
    public GoodsReceiptJpaEntity toEntity(GoodsReceipt receipt) {
        return new GoodsReceiptJpaEntity(
                receipt.id(),
                receipt.purchaseOrderId(),
                receipt.purchaseOrderLineId(),
                receipt.quantity(),
                receipt.idempotencyKey(),
                receipt.status(),
                receipt.inventoryOperationId(),
                receipt.attemptCount(),
                receipt.lastError(),
                receipt.createdBy(),
                receipt.createdAt(),
                receipt.updatedAt(),
                receipt.appliedAt(),
                receipt.version());
    }

    public GoodsReceipt toDomain(GoodsReceiptJpaEntity entity) {
        return new GoodsReceipt(
                entity.getId(),
                entity.getPurchaseOrderId(),
                entity.getPurchaseOrderLineId(),
                entity.getQuantity(),
                entity.getIdempotencyKey(),
                entity.getStatus(),
                entity.getInventoryOperationId(),
                entity.getAttemptCount(),
                entity.getLastError(),
                entity.getCreatedBy(),
                entity.getCreatedAt(),
                entity.getUpdatedAt(),
                entity.getAppliedAt(),
                entity.getVersion());
    }
}
