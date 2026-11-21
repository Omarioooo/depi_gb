# Shared Decisions

These decisions affect more than one sprint. Do not start a sprint whose decisions
are still `open`. When a decision is accepted, add the date and a one-line
rationale, then implement it.

| ID | Decision | Recommendation | Needed by | Status |
|---|---|---|---|---|
| D1 | Receiving consistency model | Synchronous Inventory call with a persisted `PENDING` receipt and bounded retry. Receipt ID is the Inventory idempotency key. | Sprint 06 | open |
| D2 | API surface and versioning | Base path `/api/v1/procurement`; breaking HTTP contracts use a new URL version; version event schemas independently. | Sprint 03 | accepted 2026-11-21 |
| D3 | Error contract | RFC 7807 `ProblemDetail` for every error; no stack traces or internal identifiers. | Sprint 02 | accepted 2026-11-21 |
| D4 | Authorization model | Trusted gateway headers only; `MANAGER`, `STAFF`, `SYSTEM_ADMIN`; System Administrators get no procurement business access by default. | Sprint 02 | accepted 2026-11-21 |
| D5 | Pagination shape | `page`, `size`, stable sort `createdAt DESC, id ASC`; response includes `content`, `page`, `size`, `totalElements`, `totalPages`. | Sprint 03 | open |
| D6 | Event publication strategy | Post-commit publish with a persisted `event_publications` row and bounded retry, hidden behind a port so an outbox can replace it later. | Sprint 08 | accepted 2026-11-21 |
| D7 | Order and receipt shape | Order creation accepts one supplier and one or more lines; each receipt request covers exactly one order line. | Sprint 03 | accepted 2026-11-21 |
| D8 | Receiving destination location | Each receipt carries an optional `locationId`; when absent, Inventory applies its configured default location. | Sprint 06 | open |

## D1 — Receiving consistency model

**Recommendation:** synchronous call to Inventory from the receiving flow.

Flow:

1. Persist the receipt as `PENDING` and reserve the quantity on the order line in
   one transaction.
2. After commit, call Inventory with the receipt ID as the idempotency key.
3. On success, mark the receipt `APPLIED` and advance the order.
4. On technical failure/timeout, keep `PENDING`, return `202`, and retry with
   bounded attempts.
5. On business rejection, mark `FAILED` and do not change received quantity.

**Rejected alternative:** publish a `GoodsReceived` event and let Inventory
consume it. It removes the synchronous failure surface but makes the receipt
result eventually consistent and harder for the dashboard to explain.

## D2 — API surface and versioning

**Accepted 2026-11-21:** use `/api/v1/procurement` as the base path. Breaking HTTP
contract changes use a new URL version. Event contracts carry `schemaVersion`
independently from day one.

**Rationale:** explicit HTTP versioning gives gateway and external consumers a
stable compatibility boundary.

## D3 — Error contract

**Recommendation:** Spring `ProblemDetail` with a stable `type` URI and a short
`detail`. Business errors use a domain-specific `code` extension when the client
must branch, for example `INVALID_STATE_TRANSITION` or `OVER_RECEIPT`.

**Accepted 2026-11-21. Rationale:** one safe, machine-readable error shape keeps
clients consistent without exposing internals.

## D4 — Authorization model

**Recommendation:** a servlet filter reads the three gateway headers into a
request-scoped `UserContext`. A small rule layer decides access per use case.
`SYSTEM_ADMIN` is explicitly denied procurement business operations.

**Accepted 2026-11-21. Rationale:** the gateway owns authentication while the
service retains explicit use-case authorization.

Proposed matrix (confirm before sprint 02):

| Action | MANAGER | STAFF | SYSTEM_ADMIN |
|---|---|---|---|
| Create / edit draft | yes | no | no |
| Read orders and receipts | yes | yes | no |
| Submit | yes | yes | no |
| Approve / cancel | yes | no | no |
| Record / retry receipt | yes | yes | no |

## D5 — Pagination shape

**Recommendation:** a shared `PageResponse<T>` record with `content`, `page`,
`size`, `totalElements`, `totalPages`. Reject `size > 100` with `400`.

## D6 — Event publication strategy

**Recommendation:** write the publication record in the same transaction as the
business change, publish after commit, and retry failed rows. The
`ProcurementEventPublisherPort` keeps this replaceable by a transactional outbox.

**Accepted 2026-11-21. Rationale:** committed changes cannot lose their event or
publish an event for a rolled-back transaction.

## D7 — Order and receipt shape

**Recommendation:** an order has one supplier and at least one line. A receipt
request identifies one line and one quantity. Do not support multi-line receipts in
this release; it keeps over-receipt checks and retries simple.

**Accepted 2026-11-21. Rationale:** one-line receipts keep idempotency and
over-receipt enforcement unambiguous.

## D8 — Receiving destination location

**Recommendation:** add an optional `locationId` to the receipt request and store it
on `goods_receipts`. If absent, the Inventory service applies its configured default
location. This keeps procurement independent of Inventory's location master data.

**Why it matters:** Inventory tracks stock per location (FR-INV-01), but the
procurement requirements do not specify where received goods are stored. Decide
before sprint 06; the receipt payload, database column, and Inventory contract all
depend on it.

## Recording a decision

```md
### D<n> — <title>
Status: accepted
Date: YYYY-MM-DD
Decision: <one paragraph>
Rationale: <one paragraph>
Consequences: <what changes in the plan>
```
