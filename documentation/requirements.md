# 4. Functional Requirements

## 4.1 Authentication and Authorization

**FR-AUTH-01:** The system shall allow authorized users to authenticate before accessing protected application functions.

**FR-AUTH-02:** The system shall support Manager, Staff, and System Administrator roles.

**FR-AUTH-03:** Managers shall be permitted to manage business records and perform managerial operations.

**FR-AUTH-04:** Staff shall be permitted to view inventory and perform authorized stock operations.

**FR-AUTH-05:** System Administrators shall be permitted to perform authorized administrative operations without automatically receiving unrestricted business privileges.

**FR-AUTH-06:** The system shall deny requests from unauthenticated users when authentication is required.

**FR-AUTH-07:** The system shall enforce permissions on protected business operations.

**FR-AUTH-08:** The system shall prevent users from modifying their own roles unless explicitly authorized.

**FR-AUTH-09:** The system shall record security-relevant administrative actions.

**FR-AUTH-10:** Authentication credentials and token-signing secrets shall not be stored in source code or container images.

## 4.2 Product Service

**FR-PROD-01:** Authorized users shall be able to create products.

**FR-PROD-02:** Each product shall contain a unique SKU, name, category, unit of measurement, and applicable descriptive information.

**FR-PROD-03:** Authorized users shall be able to retrieve individual products and paginated product lists.

**FR-PROD-04:** Authorized users shall be able to update product information.

**FR-PROD-05:** Authorized users shall be able to deactivate products.

**FR-PROD-06:** The service shall reject duplicate SKUs.

**FR-PROD-07:** Users shall be able to search and filter products by SKU, name, category, and status.

**FR-PROD-08:** The service shall validate required fields and reject invalid product data.

**FR-PROD-09:** The service shall prevent deactivation or deletion rules from compromising existing inventory history.

**FR-PROD-10:** Product creation, modification, and deactivation shall be traceable through timestamps and relevant audit information.

## 4.3 Inventory Service

**FR-INV-01:** The service shall maintain stock quantities for products at individual storage locations.

**FR-INV-02:** Authorized users shall be able to record incoming stock.

**FR-INV-03:** Authorized users shall be able to record outgoing stock.

**FR-INV-04:** Authorized users shall be able to record inventory adjustments with a reason.

**FR-INV-05:** The service shall record every successful stock operation in an immutable stock movement history.

**FR-INV-06:** Each movement record shall include the product, location, movement type, quantity, timestamp, acting user where applicable, and reference to the originating operation.

**FR-INV-07:** The service shall prevent stock quantities from becoming negative under normal inventory rules.

**FR-INV-08:** The service shall support transfers between storage locations.

**FR-INV-09:** A stock transfer shall maintain quantity consistency between the source and destination locations.

**FR-INV-10:** The service shall prevent concurrent stock updates from producing lost updates or invalid quantities.

**FR-INV-11:** The service shall support idempotency keys for stock-changing requests.

**FR-INV-12:** Repeating a successfully processed request with the same idempotency key shall not repeat the stock movement.

**FR-INV-13:** The service shall reject requests referencing nonexistent or inactive products where the operation requires an active product.

**FR-INV-14:** The service shall expose inventory queries filtered by product, location, and stock status.

**FR-INV-15:** The service shall expose stock movement history with pagination and date filtering.

**FR-INV-16:** Successful stock changes shall generate appropriate inventory events for downstream processing.

**FR-INV-17:** A failed transaction shall not leave the stock quantity updated without its corresponding movement record.

**FR-INV-18:** Inventory operations shall distinguish between rejected business operations and unexpected technical failures.

## 4.4 Supplier Service

**FR-SUP-01:** Authorized users shall be able to create supplier records.

**FR-SUP-02:** Each supplier shall have a unique identifier and a name.

**FR-SUP-03:** Supplier records may contain contact information, address, email, telephone number, and status.

**FR-SUP-04:** Authorized users shall be able to retrieve, update, and deactivate suppliers.

**FR-SUP-05:** Users shall be able to search and filter suppliers.

**FR-SUP-06:** The service shall support associations between suppliers and products.

**FR-SUP-07:** The service shall validate supplier information before saving it.

**FR-SUP-08:** The service shall preserve supplier references required by existing procurement history.

**FR-SUP-09:** The service shall expose supplier information through documented APIs for authorized consumers.

## 4.5 Procurement Service

**FR-PROC-01:** Authorized users shall be able to create purchase orders for suppliers.

**FR-PROC-02:** Each purchase order shall contain a unique order identifier, supplier reference, creation date, creator, and one or more order lines.

**FR-PROC-03:** Each order line shall identify a product and the requested quantity.

**FR-PROC-04:** Purchase orders shall support the following lifecycle states: Draft, Submitted, Approved, Partially Received, Completed, and Cancelled.

**FR-PROC-05:** The service shall validate allowed purchase order state transitions.

**FR-PROC-06:** Managers shall be able to approve eligible purchase orders.

**FR-PROC-07:** Authorized users shall be able to submit purchase orders for processing.

**FR-PROC-08:** Users shall be able to record full or partial receipts against approved purchase orders.

**FR-PROC-09:** The service shall prevent received quantities from exceeding ordered quantities unless an explicitly configured business rule allows over-receiving.

**FR-PROC-10:** Receiving goods shall trigger an inventory update through a documented API or asynchronous event workflow.

**FR-PROC-11:** The service shall record receipt references and quantities for audit purposes.

**FR-PROC-12:** Repeated receipt requests shall not cause duplicate stock additions.

**FR-PROC-13:** The service shall provide purchase order searches filtered by supplier, status, and date range.

**FR-PROC-14:** The service shall retain completed and cancelled purchase orders for historical reference.

**FR-PROC-15:** If inventory updating fails after a receipt is initiated, the service shall retain sufficient state to retry or reconcile the operation without silently marking an unsuccessful receipt as completed.

## 4.6 Notification Service

**FR-NOT-01:** The service shall consume relevant inventory events.

**FR-NOT-02:** The service shall generate a low-stock alert when the available stock at a location falls below its configured threshold.

**FR-NOT-03:** Each low-stock alert shall identify the product, location, current quantity, threshold, creation time, and current status.

**FR-NOT-04:** The service shall prevent repeated processing of the same event from creating duplicate alerts.

**FR-NOT-05:** The service shall support Active, Acknowledged, and Resolved alert states.

**FR-NOT-06:** Authorized users shall be able to retrieve and filter alerts.

**FR-NOT-07:** Authorized users shall be able to acknowledge active alerts.

**FR-NOT-08:** The service shall update or resolve an alert when stock rises above the relevant threshold.

**FR-NOT-09:** The service shall track notification delivery attempts when external notification channels are enabled.

**FR-NOT-10:** Transient notification failures shall be retried according to a configurable retry policy.

**FR-NOT-11:** Repeatedly failing messages shall be routed to a dead-letter queue when RabbitMQ is configured for this purpose.

**FR-NOT-12:** The service shall expose metrics for processed events, failed deliveries, retry counts, and pending notifications.

**FR-NOT-13:** Failure of a notification consumer shall not silently reverse an inventory transaction that has already committed successfully.

**FR-NOT-14:** The initial release shall support dashboard-based alerts. External email delivery may be implemented as an extension.

## 4.7 Reporting Service

**FR-REP-01:** The service shall provide a summary of active products, stock quantities, and low-stock items.

**FR-REP-02:** The service shall provide inventory reports filtered by product, category, location, and stock status.

**FR-REP-03:** The service shall provide stock movement reports for a specified date range.

**FR-REP-04:** The service shall provide low-stock reports containing current quantities and configured thresholds.

**FR-REP-05:** The service shall support CSV exports of inventory and stock movement reports.

**FR-REP-06:** The service shall validate report filters and reject invalid date ranges.

**FR-REP-07:** The service shall paginate large report results where applicable.

**FR-REP-08:** The service shall identify the applicable reporting period and generation time.

**FR-REP-09:** The service shall not directly query or modify private tables belonging to other services.

**FR-REP-10:** The service may maintain its own read model populated from documented APIs or events.

**FR-REP-11:** Reports generated from asynchronously updated read models shall communicate their freshness limitations where relevant.

## 4.8 Web Dashboard

**FR-UI-01:** The dashboard shall provide authenticated access to the application's functions.

**FR-UI-02:** The dashboard shall provide navigation to products, inventory, suppliers, procurement, notifications, and reports.

**FR-UI-03:** The dashboard shall display a summary of inventory status and active alerts.

**FR-UI-04:** The dashboard shall provide product search, filtering, sorting, and pagination.

**FR-UI-05:** The dashboard shall provide forms for authorized inventory operations.

**FR-UI-06:** The dashboard shall display validation errors and operation results clearly.

**FR-UI-07:** The dashboard shall display loading, empty, unavailable, and error states.

**FR-UI-08:** The dashboard shall prevent unauthorized operations from being presented as permitted actions, while relying on backend authorization as the security boundary.

**FR-UI-09:** The dashboard shall display purchase order status and receiving history.

**FR-UI-10:** The dashboard shall provide alert acknowledgement and filtering functionality.

**FR-UI-11:** The dashboard shall allow users to download supported CSV reports.

**FR-UI-12:** The dashboard shall refresh relevant information after successful operations.

**FR-UI-13:** The dashboard shall provide useful feedback when a backend service is temporarily unavailable.

## 4.9 API Gateway

**FR-GW-01:** The gateway shall provide a single external entry point for application API requests.

**FR-GW-02:** The gateway shall route requests to the appropriate internal service based on configured paths or routing rules.

**FR-GW-03:** The gateway shall enforce authentication requirements for protected routes.

**FR-GW-04:** The gateway shall apply configured rate limits to selected endpoints.

**FR-GW-05:** The gateway shall enforce request size limits and configurable timeouts.

**FR-GW-06:** The gateway shall propagate an incoming correlation ID or generate one when absent.

**FR-GW-07:** The gateway shall produce structured access logs containing relevant request metadata.

**FR-GW-08:** The gateway shall expose metrics covering request counts, latency, error rates, and upstream failures.

**FR-GW-09:** The gateway shall return consistent error responses for gateway-generated failures.

**FR-GW-10:** The gateway shall not expose internal service endpoints directly to public clients.

**FR-GW-11:** The gateway shall support health checking and controlled configuration updates.

**FR-GW-12:** The gateway shall not implement core inventory, procurement, or notification business logic.

## 4.10 Inter-Service Communication

**FR-COM-01:** Each service shall publish and consume only documented APIs and event contracts.

**FR-COM-02:** Synchronous calls shall use explicit connection and response timeouts.

**FR-COM-03:** Consumers shall handle duplicate message delivery safely.

**FR-COM-04:** Events shall contain an event identifier, event type, occurrence timestamp, schema version, and relevant business identifiers.

**FR-COM-05:** Events shall not contain unnecessary credentials or sensitive user information.

**FR-COM-06:** The system shall support retry handling for transient asynchronous processing failures.

**FR-COM-07:** Messages that repeatedly fail processing shall be made available for inspection and controlled recovery.

**FR-COM-08:** The system shall document which service owns each business record.

**FR-COM-09:** The system shall not rely on distributed database transactions across all services.

**FR-COM-10:** The team shall document the consistency expectations and recovery behavior for procurement receipts and inventory updates.
