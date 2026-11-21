package com.depigb.procurement.adapter.out.persistence;

import com.depigb.procurement.application.port.out.PurchaseOrderRepositoryPort;
import com.depigb.procurement.domain.model.PurchaseOrder;
import java.util.Optional;
import java.util.UUID;
import org.springframework.stereotype.Repository;
import org.springframework.transaction.annotation.Transactional;

@Repository
public class PurchaseOrderPersistenceAdapter implements PurchaseOrderRepositoryPort {
    private final PurchaseOrderJpaRepository repository;
    private final PurchaseOrderPersistenceMapper mapper;

    public PurchaseOrderPersistenceAdapter(
            PurchaseOrderJpaRepository repository, PurchaseOrderPersistenceMapper mapper) {
        this.repository = repository;
        this.mapper = mapper;
    }

    @Override
    @Transactional
    public PurchaseOrder save(PurchaseOrder order) {
        return mapper.toDomain(repository.saveAndFlush(mapper.toEntity(order)));
    }

    @Override
    @Transactional(readOnly = true)
    public Optional<PurchaseOrder> findById(UUID id) {
        return repository.findById(id).map(mapper::toDomain);
    }
}
