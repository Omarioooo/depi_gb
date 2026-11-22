# PROC-003 — Acceptance Criteria

Source: [`docs/scope/scope.md`](../../../../../../docs/scope/scope.md), PROC-003.

| ID | Criterion | How to verify | Status |
|---|---|---|---|
| AC-003-1 | The service reads `X-User-Id`, `X-User-Roles`, and `X-Correlation-Id` from the gateway context. | Filter test with all headers; assert `UserContext` values. | [x] |
| AC-003-2 | Missing user context is rejected. | Request without `X-User-Id`; expect `403`. | [x] |
| AC-003-3 | System Administrators have no automatic procurement business access. | Role-matrix test for every operation with `SYSTEM_ADMIN`; expect `403`. | [x] |
| AC-003-4 | Managers can perform managerial operations. | Create/approve/cancel tests with `MANAGER`; expect success. | [x] |
| AC-003-5 | Staff can submit orders and record receipts. | Submit/receive tests with `STAFF`; expect success. | [x] |
| AC-003-6 | Authorization failures return `403` without leaking internal details. | Inspect response body; assert no user ID, stack trace, or internal class name. | [x] |

## Additional checks from the plan

- [x] Public endpoints remain reachable without identity headers.
- [x] Duplicate identity headers are rejected.
- [x] Unknown roles are ignored, not granted.
- [x] Correlation ID is propagated or generated.
- [x] Correlation ID appears in logs.

## Evidence required

1. Role-matrix test output.
2. Missing-context and spoofing test output.
3. Example `403` response.
4. Build output.
