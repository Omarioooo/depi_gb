package com.depigb.procurement.application.port.out;

import com.depigb.procurement.domain.model.GoodsReceipt;
import java.util.Optional;
import java.util.UUID;

public interface GoodsReceiptRepositoryPort {
    GoodsReceipt save(GoodsReceipt receipt);

    Optional<GoodsReceipt> findById(UUID id);

    Optional<GoodsReceipt> findByIdempotencyKey(String idempotencyKey);
}
