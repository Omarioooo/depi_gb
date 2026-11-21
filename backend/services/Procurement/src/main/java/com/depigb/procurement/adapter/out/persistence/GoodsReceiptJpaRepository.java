package com.depigb.procurement.adapter.out.persistence;

import java.util.Optional;
import java.util.UUID;
import org.springframework.data.jpa.repository.JpaRepository;

interface GoodsReceiptJpaRepository extends JpaRepository<GoodsReceiptJpaEntity, UUID> {
    Optional<GoodsReceiptJpaEntity> findByIdempotencyKey(String idempotencyKey);
}
