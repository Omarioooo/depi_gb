package com.depigb.procurement.domain.model;

import static org.assertj.core.api.Assertions.assertThatThrownBy;

import java.math.BigDecimal;
import java.time.Instant;
import java.util.UUID;
import org.junit.jupiter.api.Test;

class DomainModelTest {
    private static final Instant NOW = Instant.parse("2026-11-21T12:00:00Z");

    @Test
    void rejectsNonPositiveOrderedQuantity() {
        assertThatThrownBy(() -> line(BigDecimal.ZERO, BigDecimal.ZERO))
                .isInstanceOf(IllegalArgumentException.class)
                .hasMessage("orderedQuantity must be positive");
    }

    @Test
    void rejectsReceivedQuantityAboveOrderedQuantity() {
        assertThatThrownBy(() -> line(new BigDecimal("2.000"), new BigDecimal("2.001")))
                .isInstanceOf(IllegalArgumentException.class)
                .hasMessage("receivedQuantity must not exceed orderedQuantity");
    }

    @Test
    void rejectsNonPositiveReceiptQuantity() {
        assertThatThrownBy(() -> new GoodsReceipt(
                        UUID.randomUUID(), UUID.randomUUID(), UUID.randomUUID(), BigDecimal.ZERO,
                        "receipt-1", ReceiptStatus.PENDING, null, 0, null, "user-1", NOW, NOW, null, 0))
                .isInstanceOf(IllegalArgumentException.class)
                .hasMessage("quantity must be positive");
    }

    private PurchaseOrderLine line(BigDecimal ordered, BigDecimal received) {
        return new PurchaseOrderLine(
                UUID.randomUUID(), UUID.randomUUID(), UUID.randomUUID(), ordered, received, NOW, NOW, 0);
    }
}
