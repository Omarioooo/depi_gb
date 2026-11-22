# Scope: Procurement Service

This service manages supplier purchase orders and goods receiving for the inventory platform. The work is split into small backend slices so each slice can be reviewed, tested, committed, and pushed independently.

**Build approach:** Tracer Bullet (each slice leaves a working service and moves one complete path forward).
**Workflow:** Beta (build, verify, then test each slice before it is considered complete).

## How we will work

Each slice uses its `PROC` ID in the conversation, commit message, test report, and review notes.

1. Announce the slice, for example `Implementing PROC-004`.
2. Build only that slice and its required dependency changes.
3. Run the slice tests and the service build.
4. Show changed files, API examples, and test evidence.
5. Review the result before creating a commit.
6. Push only after the review is accepted.

Each slice must leave the repository buildable. A later slice may extend an earlier slice, but it must not bypass its acceptance criteria.

## At a glance

| ID | Feature | Phase | Status |
|---|---|---|---|
| PROC-001 | Service foundation | Foundation | done |
| PROC-002 | Persistence and schema | Foundation | done |
| PROC-003 | Gateway authorization boundary | Foundation | done |
| PROC-004 | Draft purchase orders | Slice 1 | planned |
| PROC-005 | Purchase order lifecycle | Slice 2 | planned |
| PROC-006 | Supplier and product validation | Slice 3 | planned |
| PROC-007 | Goods receiving and inventory coordination | Slice 4 | planned |
| PROC-008 | Search and receipt history | Slice 5 | planned |
| PROC-009 | Procurement events and recovery | Slice 6 | planned |
| PROC-010 | Runtime operations and deployment | Foundation | planned |
| PROC-011 | Full integration verification | Final | planned |

## Foundation

### PROC-001. Service foundation · done

Turn the existing Gradle starter into a runnable Java 21 Spring Boot service with the package boundaries used by every later slice.

**Done when:** the service compiles, starts with environment configuration, exposes health endpoints, has the hexagonal package layout, and produces a runnable Docker image.

Dependencies: none.

Review evidence:

1. Gradle build output
2. Application startup output
3. Health endpoint response
4. Docker image build output

### PROC-002. Persistence and schema · done

Create the Procurement database model and migration baseline for purchase orders, lines, receipts, and event publication records.

**Done when:** Flyway creates PostgreSQL tables with UUID keys, `NUMERIC(19,3)` quantities, foreign keys, unique idempotency keys, status constraints, timestamps, and optimistic locking fields.

Acceptance criteria:

1. `AC-002-1`: A clean database can be migrated from empty state.
2. `AC-002-2`: Purchase orders require a supplier ID, creator, status, and timestamps.
3. `AC-002-3`: Order lines require a product ID and positive ordered quantity.
4. `AC-002-4`: Receipts store one line, quantity, idempotency key, status, retry data, and Inventory operation ID.
5. `AC-002-5`: Duplicate idempotency keys are rejected by the database.
6. `AC-002-6`: The migration is applied and the live schema is inspected in PostgreSQL.

Review evidence:

1. Migration files
2. Database schema inspection
3. Constraint tests

### PROC-003. Gateway authorization boundary · done

Add the local authorization boundary that consumes trusted gateway headers without implementing authentication inside Procurement.

**Done when:** every protected endpoint requires a trusted user context, role checks are enforced, client supplied identity headers cannot override gateway values, and unauthorized requests return consistent errors.

Acceptance criteria:

1. `AC-003-1`: The service reads `X-User-Id`, `X-User-Roles`, and `X-Correlation-Id` from the gateway context.
2. `AC-003-2`: Missing user context is rejected.
3. `AC-003-3`: System Administrators have no automatic procurement business access.
4. `AC-003-4`: Managers can perform managerial operations.
5. `AC-003-5`: Staff can submit orders and record receipts.
6. `AC-003-6`: Authorization failures return `403` without leaking internal details.

Review evidence:

1. Role matrix tests
2. Header spoofing tests
3. Example `403` responses

## Slice 1: Draft purchase orders

### PROC-004. Draft purchase orders

Implement creation, retrieval, editing, and pagination for Draft purchase orders and their lines.

**Done when:** a Manager can create a valid Draft order, the creator and Managers can edit it, users can retrieve it, and invalid input is rejected with useful JSON errors.

Acceptance criteria:

1. `AC-004-1`: Managers can create an order with a supplier ID and one or more product lines.
2. `AC-004-2`: The service generates a UUID order ID.
3. `AC-004-3`: Only Draft orders can be edited.
4. `AC-004-4`: The creator can edit their own Draft and Managers can edit any Draft.
5. `AC-004-5`: Empty lines, missing IDs, zero quantities, and negative quantities are rejected.
6. `AC-004-6`: Orders and lines can be retrieved by ID.
7. `AC-004-7`: List responses are paginated and support stable ordering.

Review evidence:

1. Request and response examples
2. Controller and domain tests
3. Validation test output

## Slice 2: Purchase order lifecycle

### PROC-005. Purchase order lifecycle

Implement the explicit order state machine and role protected actions for submit, approve, and cancel.

**Done when:** only allowed transitions succeed, invalid transitions return `409`, and all lifecycle actions record the acting user and UTC timestamp.

Acceptance criteria:

1. `AC-005-1`: Draft orders can move to Submitted.
2. `AC-005-2`: Submitted orders can move to Approved.
3. `AC-005-3`: Draft, Submitted, Approved, and Partially Received orders can move to Cancelled.
4. `AC-005-4`: Completed and Cancelled orders are terminal.
5. `AC-005-5`: Staff can submit orders.
6. `AC-005-6`: Only Managers can approve or cancel orders.
7. `AC-005-7`: Invalid transitions return `409` and do not change data.
8. `AC-005-8`: Concurrent changes are detected through optimistic locking.

Review evidence:

1. State transition table tests
2. Role specific endpoint tests
3. Concurrent update test

## Slice 3: External reference validation

### PROC-006. Supplier and product validation

Add synchronous clients for the Supplier and Product services and validate references while creating and submitting orders.

**Done when:** unknown or inactive suppliers and products are rejected, dependency timeouts are handled clearly, and external service URLs and timeouts are configurable.

Acceptance criteria:

1. `AC-006-1`: Create validates the supplier reference.
2. `AC-006-2`: Create validates product references and active status.
3. `AC-006-3`: Submit validates references again so deactivated records cannot be submitted.
4. `AC-006-4`: A missing external record returns a business error without creating or changing an order.
5. `AC-006-5`: A dependency timeout returns `503` and does not partially update the order.
6. `AC-006-6`: Client base URLs and timeout values come from environment configuration.

Review evidence:

1. HTTP client tests with stubbed dependencies
2. Timeout and error mapping tests
3. Configuration example

## Slice 4: Goods receiving and Inventory coordination

### PROC-007. Goods receiving and Inventory coordination

Implement one line per receipt, idempotent receiving, over receiving protection, synchronous Inventory updates, and retryable pending receipts.

**Done when:** successful receiving updates Inventory exactly once and advances the order correctly, while Inventory failures remain visible and retryable.

Acceptance criteria:

1. `AC-007-1`: Staff and Managers can receive against Approved or Partially Received orders.
2. `AC-007-2`: Each request receives one purchase order line.
3. `AC-007-3`: `Idempotency-Key` is required for receiving.
4. `AC-007-4`: Repeating a request with the same key returns the original result and does not add stock twice.
5. `AC-007-5`: Received quantity cannot exceed ordered quantity.
6. `AC-007-6`: Inventory receives the receipt ID as its idempotency key.
7. `AC-007-7`: Inventory success applies the receipt and changes the order to Partially Received or Completed.
8. `AC-007-8`: Inventory technical failure leaves the receipt Pending and returns `202`.
9. `AC-007-9`: Pending receipts retry with bounded attempts and retain the last failure.
10. `AC-007-10`: Inventory business rejection marks the receipt Failed without changing received quantity.
11. `AC-007-11`: Concurrent receipts cannot over receive or lose updates.

Review evidence:

1. Receipt API examples
2. Duplicate request test
3. Inventory timeout and retry test
4. Concurrent receipt test
5. Partial and complete receiving test

## Slice 5: Search and history

### PROC-008. Search and receipt history

Expose filtered, paginated purchase order queries and receipt history for the dashboard and reporting consumers.

**Done when:** authorized users can filter by supplier, status, and date range, and receipt history is paginated with consistent ordering.

Acceptance criteria:

1. `AC-008-1`: Orders can be filtered by supplier ID.
2. `AC-008-2`: Orders can be filtered by lifecycle status.
3. `AC-008-3`: Orders can be filtered by creation date range.
4. `AC-008-4`: Invalid date ranges return `400`.
5. `AC-008-5`: Results are paginated with total count and page metadata.
6. `AC-008-6`: Receipt history is available for an order.
7. `AC-008-7`: Completed and Cancelled orders remain queryable.

Review evidence:

1. Filtered API examples
2. Pagination tests
3. Date boundary tests

## Slice 6: Events and recovery

### PROC-009. Procurement events and recovery

Publish documented purchase order and receipt events to RabbitMQ after successful business operations, with confirms, retries, metrics, and reconciliation support.

**Done when:** successful operations publish versioned events, transient publication failures retry, failed publications remain inspectable, and duplicate event delivery is safe for consumers.

Acceptance criteria:

1. `AC-009-1`: Events contain event ID, type, occurrence time, schema version, aggregate ID, and business identifiers.
2. `AC-009-2`: Events publish only after the database operation succeeds.
3. `AC-009-3`: RabbitMQ publisher confirms are enabled.
4. `AC-009-4`: Transient publish failures retry with bounded backoff.
5. `AC-009-5`: Failed publications are recorded for inspection and retry.
6. `AC-009-6`: Duplicate event IDs can be safely consumed downstream.
7. `AC-009-7`: Correlation IDs appear in event logs.

Review evidence:

1. Event payload examples
2. RabbitMQ integration test
3. Publish failure and retry test
4. Reconciliation test

## Foundation: Runtime and delivery

### PROC-010. Runtime operations and deployment

Package the service for local and Kubernetes environments with safe configuration, probes, metrics, and non root execution.

**Done when:** the service runs in Docker and Kubernetes with externalized configuration, readiness and liveness probes, Prometheus metrics, and no secrets in source or images.

Acceptance criteria:

1. `AC-010-1`: Docker builds a Java 21 runtime image.
2. `AC-010-2`: The runtime image runs as a non root user.
3. `AC-010-3`: Database, RabbitMQ, service URLs, and credentials come from environment or Kubernetes Secrets.
4. `AC-010-4`: Liveness and readiness endpoints are configured.
5. `AC-010-5`: Prometheus metrics are exposed through Actuator.
6. `AC-010-6`: Kubernetes deployment includes resource settings, probes, and an internal Service.
7. `AC-010-7`: The service is not exposed directly as a public entry point.

Review evidence:

1. Docker build output
2. Container health output
3. Kubernetes manifest validation
4. Metrics endpoint response

### PROC-011. Full integration verification

Prove the complete Procurement flow with PostgreSQL, RabbitMQ, and stubbed neighboring services, then document the repeatable verification commands.

**Done when:** the complete test suite passes and a reviewer can create, submit, approve, receive, retry, query, and inspect events using documented commands.

Acceptance criteria:

1. `AC-011-1`: Testcontainers starts PostgreSQL and RabbitMQ for integration tests.
2. `AC-011-2`: The migration runs against a clean PostgreSQL container.
3. `AC-011-3`: The full lifecycle test passes from Draft to Completed.
4. `AC-011-4`: Authorization tests cover Manager, Staff, and System Administrator behavior.
5. `AC-011-5`: Duplicate receipt and concurrent receipt tests pass.
6. `AC-011-6`: Inventory outage and retry tests pass.
7. `AC-011-7`: Event publication failure and recovery tests pass.
8. `AC-011-8`: The service build and all tests pass from a clean checkout.
9. `AC-011-9`: The review report lists changed files, commands, results, and any known limitation.

Review evidence:

1. Full Gradle test output
2. Clean build output
3. Manual API walkthrough
4. Final review report

## Deferred

The following remain outside this implementation plan:

1. Authentication and token issuance
2. Product and Supplier data ownership
3. Prices, currency, payments, and invoices
4. Email or external notification delivery
5. Transactional outbox replacement for the selected direct event publishing strategy

## Legend

`planned` means the slice is defined but not started.

`in-progress` means starter code exists and the slice is ready to continue.

Each `PROC` ID is the unit we can implement, test, review, commit, and push separately.
