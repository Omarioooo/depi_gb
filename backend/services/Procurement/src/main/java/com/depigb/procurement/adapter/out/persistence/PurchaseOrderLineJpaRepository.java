package com.depigb.procurement.adapter.out.persistence;

import java.util.UUID;
import org.springframework.data.jpa.repository.JpaRepository;

interface PurchaseOrderLineJpaRepository extends JpaRepository<PurchaseOrderLineJpaEntity, UUID> {}
