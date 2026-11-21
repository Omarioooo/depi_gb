package com.depigb.procurement.application.port.out;

import com.depigb.procurement.domain.model.PurchaseOrder;
import java.util.Optional;
import java.util.UUID;

public interface PurchaseOrderRepositoryPort {
    PurchaseOrder save(PurchaseOrder order);

    Optional<PurchaseOrder> findById(UUID id);
}
