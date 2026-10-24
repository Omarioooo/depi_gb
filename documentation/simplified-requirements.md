# Simplified Requirements

A condensed view of the full requirements (`requirements.md`) and the system
architecture (`arch.md`). Use this as the quick reference; the original documents
remain the source of truth.

---

## 1. What We Are Building

A multi-service inventory management system for a warehouse operation. It tracks
products, stock levels per storage location, suppliers, purchase orders, low-stock
alerts, and reports — all accessed through a web dashboard behind a single API
gateway.

**Core goal:** every stock change is recorded, auditable, and never lost or
duplicated — even when things fail.

---

## 2. Users and Roles

| Role | Can do |
|---|---|
| **Staff** | View inventory, perform authorized stock operations |
| **Manager** | Manage business records (products, suppliers, purchase orders), approve orders |
| **System Administrator** | Administrative operations only — no automatic business privileges |

Rules:
- All protected functions require authentication.
- Permissions are enforced by the backend, not the UI.
- Users cannot change their own roles unless explicitly authorized.
- Security-relevant admin actions are logged.
- Secrets live in configuration/env, never in source code or images.

---

## 3. Architecture at a Glance

```
Web Dashboard
      ↓
Nginx / Reverse Proxy
      ↓
API Gateway
      ↓
Product · Inventory · Supplier · Procurement · Notification · Reporting
      ↓                                        ↑
 each service owns its own database       RabbitMQ (events)
```

Key principles:
- The gateway is the only public entry point. Internal services are never exposed.
- Each service owns its data; no service reads or writes another service's tables.
- Services communicate via documented APIs or events — no shared database transactions.
- Inventory and Procurement publish events; Notification and Reporting consume them.

---

## 4. Services

### 4.1 Product Service
- Create, read (single + paginated list), update, and deactivate products.
- Each product has a unique SKU, name, category, unit of measure, and description.
- Duplicate SKUs are rejected; required fields are validated.
- Search/filter by SKU, name, category, and status.
- Deactivation must not break existing inventory history.
- Changes are traceable via timestamps and audit info.

### 4.2 Inventory Service
- Maintains stock quantities per product **per storage location**.
- Records stock in, stock out, adjustments (with reason), and location transfers.
- Every successful change writes an immutable stock movement record containing:
  product, location, type, quantity, timestamp, acting user, source operation.
- Stock can never go negative under normal rules.
- Transfers keep source and destination quantities consistent.
- Concurrency-safe: no lost updates.
- Supports idempotency keys — a repeated request never repeats a movement.
- Quantity change and movement record succeed or fail together (atomic).
- Rejects operations referencing nonexistent or inactive products.
- Queries by product, location, and stock status; movement history is paginated
  and date-filterable.
- Emits inventory events on success.
- Distinguishes business rejections from technical failures.

### 4.3 Supplier Service
- Create, read, update, deactivate suppliers.
- Unique ID plus name; optional contact, address, email, phone, status.
- Search and filter supported.
- Suppliers can be associated with products.
- Supplier data is validated before saving.
- References needed by procurement history are preserved.

### 4.4 Procurement Service
- Purchase orders belong to a supplier and contain a unique ID, creation date,
  creator, and one or more lines (product + requested quantity).
- Lifecycle: **Draft → Submitted → Approved → Partially Received → Completed / Cancelled**.
  Only valid transitions are allowed.
- Managers approve eligible orders; authorized users submit them.
- Full or partial receipts can be recorded against approved orders.
- Received quantity cannot exceed ordered quantity unless a business rule allows it.
- Receiving triggers an inventory update via API or event.
- Receipt references and quantities are retained for audit.
- Repeated receipt requests never add stock twice.
- If inventory update fails, the receipt is not silently marked complete — state is
  kept for retry/reconciliation.
- Completed and cancelled orders are retained for history.
- Search by supplier, status, and date range.

### 4.5 Notification Service
- Consumes inventory events.
- Creates a low-stock alert when available stock at a location falls below its
  configured threshold.
- Alert contains: product, location, current quantity, threshold, created time, status.
- Alert states: **Active → Acknowledged → Resolved**.
- Duplicate events do not create duplicate alerts.
- Alert auto-updates/resolves when stock rises above the threshold.
- Users can list, filter, and acknowledge alerts.
- Delivery attempts are tracked; transient failures retried per configurable policy.
- Repeatedly failing messages go to a dead-letter queue.
- Metrics: events processed, failed deliveries, retries, pending notifications.
- A notification failure never rolls back an already-committed inventory transaction.
- **Initial release:** dashboard alerts only. Email is a future extension.

### 4.6 Reporting Service
- Summary of active products, stock quantities, and low-stock items.
- Inventory reports filtered by product, category, location, stock status.
- Stock movement reports for a date range.
- Low-stock report with current quantities and thresholds.
- CSV export for inventory and movement reports.
- Invalid filters (e.g., bad date ranges) are rejected; large results paginated.
- Reports state their reporting period and generation time.
- Uses its own read model fed by APIs/events; flags data freshness when relevant.

### 4.7 Web Dashboard
- Authenticated access with navigation to products, inventory, suppliers,
  procurement, notifications, and reports.
- Dashboard home shows inventory summary and active alerts.
- Product search, filtering, sorting, pagination.
- Forms for authorized inventory operations.
- Clear validation errors, operation results, and loading/empty/error/unavailable states.
- Shows purchase order status and receiving history.
- Alert acknowledgement and filtering.
- CSV report downloads.
- Refreshes relevant data after successful operations.
- Hides actions the user cannot perform — but the backend remains the security boundary.

### 4.8 API Gateway
- Single external entry point; routes by configured paths.
- Enforces authentication on protected routes.
- Rate limits, request size limits, and configurable timeouts.
- Propagates or generates a correlation ID per request.
- Structured access logs and metrics (requests, latency, errors, upstream failures).
- Consistent error responses for gateway-generated failures.
- Health checks and controlled config updates.
- Contains **no** core inventory, procurement, or notification business logic.

---

## 5. Inter-Service Communication

- Only documented APIs and event contracts are used.
- Synchronous calls always set explicit connection and response timeouts.
- Consumers must safely handle duplicate message delivery.
- Every event carries: event ID, event type, occurrence timestamp, schema version,
  and business identifiers.
- Events never contain credentials or unnecessary sensitive user data.
- Transient async failures are retried; repeatedly failing messages are quarantined
  for inspection and controlled recovery.
- Ownership of every business record is documented.
- No distributed transactions across services — consistency is achieved through
  events and explicit recovery behavior.

---

## 6. Non-Negotiable Invariants

These are the requirements most likely to cause real damage if broken:

1. Stock never goes negative under normal rules.
2. Every stock change has exactly one matching movement record.
3. Retries (idempotency keys, duplicate events, repeated receipts) never double-apply.
4. Concurrent updates never lose data.
5. A failed downstream step (notification, inventory update) never silently corrupts
   or reverses a committed transaction.
6. No service touches another service's private data.
7. Only the gateway is publicly reachable.

---

## 7. Explicitly Out of Scope (Initial Release)

- Email or SMS notification delivery (dashboard alerts only).
- Distributed database transactions.
- Public exposure of internal service endpoints.
- Business logic inside the gateway.

---

## 8. Definition of Done (High Level)

- All listed operations work through the gateway and dashboard.
- Role permissions are enforced server-side.
- Duplicate/retry scenarios are proven safe.
- Failure paths (inventory down, notification consumer down) behave as described.
- Reports and CSV exports match current inventory data (or clearly state staleness).
- Audit trails exist for stock movements, product changes, and admin actions.
