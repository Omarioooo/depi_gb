package com.depigb.procurement.adapter.out.persistence;

import com.depigb.procurement.application.port.out.GoodsReceiptRepositoryPort;
import com.depigb.procurement.domain.model.GoodsReceipt;
import java.util.Optional;
import java.util.UUID;
import org.springframework.stereotype.Repository;
import org.springframework.transaction.annotation.Transactional;

@Repository
public class GoodsReceiptPersistenceAdapter implements GoodsReceiptRepositoryPort {
    private final GoodsReceiptJpaRepository repository;
    private final GoodsReceiptPersistenceMapper mapper;

    public GoodsReceiptPersistenceAdapter(
            GoodsReceiptJpaRepository repository, GoodsReceiptPersistenceMapper mapper) {
        this.repository = repository;
        this.mapper = mapper;
    }

    @Override
    @Transactional
    public GoodsReceipt save(GoodsReceipt receipt) {
        return mapper.toDomain(repository.saveAndFlush(mapper.toEntity(receipt)));
    }

    @Override
    @Transactional(readOnly = true)
    public Optional<GoodsReceipt> findById(UUID id) {
        return repository.findById(id).map(mapper::toDomain);
    }

    @Override
    @Transactional(readOnly = true)
    public Optional<GoodsReceipt> findByIdempotencyKey(String idempotencyKey) {
        return repository.findByIdempotencyKey(idempotencyKey).map(mapper::toDomain);
    }
}
