package com.depigb.procurement.adapter.out.persistence;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;

import com.depigb.procurement.application.port.out.GoodsReceiptRepositoryPort;
import com.depigb.procurement.application.port.out.PurchaseOrderRepositoryPort;
import com.depigb.procurement.domain.model.GoodsReceipt;
import com.depigb.procurement.domain.model.PurchaseOrder;
import com.depigb.procurement.domain.model.PurchaseOrderLine;
import com.depigb.procurement.domain.model.PurchaseOrderStatus;
import com.depigb.procurement.domain.model.ReceiptStatus;
import java.math.BigDecimal;
import java.sql.Timestamp;
import java.time.Instant;
import java.util.List;
import java.util.UUID;
import org.flywaydb.core.Flyway;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.dao.DataIntegrityViolationException;
import org.springframework.orm.ObjectOptimisticLockingFailureException;
import org.springframework.jdbc.core.JdbcTemplate;

@SpringBootTest
class PersistenceIntegrationTest {
    private static final Instant NOW = Instant.parse("2026-11-21T12:00:00Z");

    @Autowired private Flyway flyway;
    @Autowired private JdbcTemplate jdbc;
    @Autowired private PurchaseOrderRepositoryPort orders;
    @Autowired private GoodsReceiptRepositoryPort receipts;

    @BeforeEach
    void cleanTables() {
        jdbc.update("DELETE FROM event_publications");
        jdbc.update("DELETE FROM goods_receipts");
        jdbc.update("DELETE FROM purchase_order_lines");
        jdbc.update("DELETE FROM purchase_orders");
    }

    @Test
    void appliesV1MigrationToCleanPostgres() {
        assertThat(flyway.info().current().getVersion().getVersion()).isEqualTo("1");
        assertThat(jdbc.queryForObject(
                        "SELECT count(*) FROM information_schema.tables "
                                + "WHERE table_schema='public' AND table_name IN "
                                + "('purchase_orders','purchase_order_lines','goods_receipts','event_publications')",
                        Integer.class))
                .isEqualTo(4);
    }

    @Test
    void savesAndLoadsOrderWithLines() {
        PurchaseOrder order = order(PurchaseOrderStatus.DRAFT);

        PurchaseOrder loaded = orders.findById(orders.save(order).id()).orElseThrow();

        assertThat(loaded.supplierId()).isEqualTo(order.supplierId());
        assertThat(loaded.lines()).singleElement().satisfies(line -> {
            assertThat(line.productId()).isEqualTo(order.lines().getFirst().productId());
            assertThat(line.orderedQuantity()).isEqualByComparingTo("10.000");
            assertThat(line.receivedQuantity()).isEqualByComparingTo("0.000");
        });
    }

    @Test
    void roundTripsReceiptAndRejectsDuplicateIdempotencyKey() {
        PurchaseOrder order = orders.save(order(PurchaseOrderStatus.APPROVED));
        GoodsReceipt first = receipt(order, UUID.randomUUID(), "receipt-key-1");
        GoodsReceipt duplicate = receipt(order, UUID.randomUUID(), "receipt-key-1");

        GoodsReceipt loaded = receipts.findById(receipts.save(first).id()).orElseThrow();

        assertThat(loaded.purchaseOrderLineId()).isEqualTo(order.lines().getFirst().id());
        assertThat(loaded.quantity()).isEqualByComparingTo("2.500");
        assertThat(loaded.inventoryOperationId()).isEqualTo("inventory-op-1");
        assertThat(loaded.attemptCount()).isZero();
        assertThatThrownBy(() -> receipts.save(duplicate))
                .isInstanceOf(DataIntegrityViolationException.class);
    }

    @Test
    void databaseRejectsInvalidLineQuantitiesAndNullProduct() {
        UUID orderId = insertOrder();

        assertThatThrownBy(() -> insertLine(orderId, UUID.randomUUID(), BigDecimal.ZERO, BigDecimal.ZERO))
                .isInstanceOf(DataIntegrityViolationException.class);
        assertThatThrownBy(() -> insertLine(
                        orderId, UUID.randomUUID(), new BigDecimal("-1.000"), BigDecimal.ZERO))
                .isInstanceOf(DataIntegrityViolationException.class);
        assertThatThrownBy(() -> insertLine(
                        orderId, UUID.randomUUID(), new BigDecimal("1.000"), new BigDecimal("1.001")))
                .isInstanceOf(DataIntegrityViolationException.class);
        assertThatThrownBy(() -> insertLine(
                        orderId, null, new BigDecimal("1.000"), BigDecimal.ZERO))
                .isInstanceOf(DataIntegrityViolationException.class);
    }

    @Test
    void databaseRejectsMissingRequiredOrderValues() {
        assertThatThrownBy(() -> jdbc.update(
                        "INSERT INTO purchase_orders (id,supplier_id,status,created_by,created_at,updated_at) "
                                + "VALUES (?,NULL,?,?,?,?)",
                        UUID.randomUUID(), "DRAFT", "user-1", Timestamp.from(NOW), Timestamp.from(NOW)))
                .isInstanceOf(DataIntegrityViolationException.class);
        assertThatThrownBy(() -> jdbc.update(
                        "INSERT INTO purchase_orders (id,supplier_id,status,created_by,created_at,updated_at) "
                                + "VALUES (?,?,NULL,?,?,?)",
                        UUID.randomUUID(), UUID.randomUUID(), "user-1", Timestamp.from(NOW), Timestamp.from(NOW)))
                .isInstanceOf(DataIntegrityViolationException.class);
        assertThatThrownBy(() -> jdbc.update(
                        "INSERT INTO purchase_orders (id,supplier_id,status,created_by,created_at,updated_at) "
                                + "VALUES (?,?,?,NULL,?,?)",
                        UUID.randomUUID(), UUID.randomUUID(), "DRAFT", Timestamp.from(NOW), Timestamp.from(NOW)))
                .isInstanceOf(DataIntegrityViolationException.class);
        assertThatThrownBy(() -> jdbc.update(
                        "INSERT INTO purchase_orders (id,supplier_id,status,created_by,created_at,updated_at) "
                                + "VALUES (?,?,?,?,NULL,?)",
                        UUID.randomUUID(), UUID.randomUUID(), "DRAFT", "user-1", Timestamp.from(NOW)))
                .isInstanceOf(DataIntegrityViolationException.class);
        assertThatThrownBy(() -> jdbc.update(
                        "INSERT INTO purchase_orders (id,supplier_id,status,created_by,created_at,updated_at) "
                                + "VALUES (?,?,?,?,?,NULL)",
                        UUID.randomUUID(), UUID.randomUUID(), "DRAFT", "user-1", Timestamp.from(NOW)))
                .isInstanceOf(DataIntegrityViolationException.class);
    }

    @Test
    void rejectsStaleOrderUpdate() {
        PurchaseOrder saved = orders.save(order(PurchaseOrderStatus.DRAFT));
        PurchaseOrder firstCopy = orders.findById(saved.id()).orElseThrow();
        PurchaseOrder staleCopy = orders.findById(saved.id()).orElseThrow();

        orders.save(withStatus(firstCopy, PurchaseOrderStatus.SUBMITTED));

        assertThatThrownBy(() -> orders.save(withStatus(staleCopy, PurchaseOrderStatus.APPROVED)))
                .isInstanceOf(ObjectOptimisticLockingFailureException.class);
    }

    private PurchaseOrder order(PurchaseOrderStatus status) {
        UUID orderId = UUID.randomUUID();
        PurchaseOrderLine line = new PurchaseOrderLine(
                UUID.randomUUID(), orderId, UUID.randomUUID(), new BigDecimal("10.000"),
                BigDecimal.ZERO, NOW, NOW, 0);
        return new PurchaseOrder(
                orderId, UUID.randomUUID(), status, "user-1", NOW, NOW,
                null, null, null, null, 0, List.of(line));
    }

    private GoodsReceipt receipt(PurchaseOrder order, UUID receiptId, String key) {
        return new GoodsReceipt(
                receiptId, order.id(), order.lines().getFirst().id(), new BigDecimal("2.500"), key,
                ReceiptStatus.PENDING, "inventory-op-1", 0, null, "user-1", NOW, NOW, null, 0);
    }

    private PurchaseOrder withStatus(PurchaseOrder order, PurchaseOrderStatus status) {
        return new PurchaseOrder(
                order.id(), order.supplierId(), status, order.createdBy(), order.createdAt(), NOW,
                order.submittedAt(), order.approvedAt(), order.cancelledAt(), order.completedAt(),
                order.version(), order.lines());
    }

    private UUID insertOrder() {
        UUID id = UUID.randomUUID();
        jdbc.update(
                "INSERT INTO purchase_orders (id,supplier_id,status,created_by) VALUES (?,?,?,?)",
                id, UUID.randomUUID(), "DRAFT", "user-1");
        return id;
    }

    private void insertLine(
            UUID orderId, UUID productId, BigDecimal orderedQuantity, BigDecimal receivedQuantity) {
        jdbc.update(
                "INSERT INTO purchase_order_lines "
                        + "(id,purchase_order_id,product_id,ordered_quantity,received_quantity) "
                        + "VALUES (?,?,?,?,?)",
                UUID.randomUUID(), orderId, productId, orderedQuantity, receivedQuantity);
    }
}
