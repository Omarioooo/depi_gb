# Sprint 02 — PROC-003: Gateway Authorization Boundary

**Status:** done
**Phase:** Foundation
**Depends on:** PROC-001
**Unblocks:** PROC-004 and every protected endpoint

## Goal

Consume the trusted gateway identity headers, expose a request-scoped user context,
and enforce role rules on procurement operations. Authentication itself stays at the
gateway/Keycloak; this service only authorizes.

## In scope

- A servlet filter that reads `X-User-Id`, `X-User-Roles`, and `X-Correlation-Id`.
- A request-scoped `UserContext` and `Role` enum.
- A rule layer used by controllers/use cases.
- Consistent `403` responses with `ProblemDetail`.
- Correlation ID propagation into logs.
- Role-matrix, missing-context, and header-spoofing tests.

## Out of scope

- Token validation, login, or Keycloak configuration.
- User management.
- Per-field authorization.
- Gateway routing rules.

## Deliverables

```text
config/SecurityConfig.java
adapter/in/web/security/GatewayHeaderFilter.java
adapter/in/web/security/UserContext.java
adapter/in/web/security/Role.java
adapter/in/web/security/AuthorizationRules.java
adapter/in/web/ProblemDetailExceptionHandler.java
src/test/java/.../security/GatewayHeaderFilterTest.java
src/test/java/.../security/RoleMatrixTest.java
```

## Public endpoints

Only these bypass the identity check:

- `/actuator/health/**`
- `/actuator/info`
- `/api/procurement/status`

Everything else requires a valid gateway context.

## Role matrix (confirm D4)

| Action | MANAGER | STAFF | SYSTEM_ADMIN |
|---|---|---|---|
| Create / edit draft | yes | no | no |
| Read orders and receipts | yes | yes | no |
| Submit order | yes | yes | no |
| Approve / cancel order | yes | no | no |
| Record / retry receipt | yes | yes | no |

## Security notes

- Reject a request with missing or blank `X-User-Id` with `403`.
- Reject duplicate identity headers rather than silently choosing one.
- Normalize roles to uppercase, trim whitespace, and ignore unknown role names.
- `SYSTEM_ADMIN` is a valid role but has no procurement business access by default.
- The service must be reachable only through the gateway (Kubernetes/internal
  network). Header trust is a deployment property, not something the service can
  prove on its own.
- Never include the user ID, role list, stack traces, or internal class names in a
  `403` body.

## Definition of done

- Every protected endpoint rejects missing context with `403`.
- The role matrix is covered by tests.
- Duplicate/spoofed identity headers are rejected.
- Correlation IDs appear in request logs.
- `./gradlew clean test` passes and evidence is recorded.

## Files in this sprint

- [`tasks.md`](./tasks.md)
- [`acceptance.md`](./acceptance.md)
- [`evidence.md`](./evidence.md)

Runbook: write it at sprint start by copying
[`../runbook-template.md`](../runbook-template.md).

## Next sprint

Sprint 03 — PROC-004 Draft purchase orders.
