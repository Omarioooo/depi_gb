# PROC-003 — Tasks

## Prepare

- [x] Accept D3 and D4 in [`../decisions.md`](../decisions.md).
- [x] Create branch `feat/proc-003-authorization`.
- [x] Confirm the public endpoint list.
- [x] Run the baseline `./gradlew clean test`.

## Identity context

- [x] Add `Role` enum: `MANAGER`, `STAFF`, `SYSTEM_ADMIN`.
- [x] Add `UserContext` with user ID, roles, and correlation ID.
- [x] Add `GatewayHeaderFilter` implementing `OncePerRequestFilter`.
- [x] Read exactly one value for each identity header.
- [x] Reject missing/blank user ID with `403`.
- [x] Reject duplicate `X-User-Id` or `X-User-Roles` values with `403`.
- [x] Parse roles case-insensitively and ignore unknown roles.
- [x] Generate a correlation ID when the gateway header is absent; otherwise
      propagate it.
- [x] Put the correlation ID into the logging MDC and clear it after the request.
- [x] Store `UserContext` as a request attribute.

## Authorization

- [x] Add `AuthorizationRules` with methods for each operation group.
- [x] Deny `SYSTEM_ADMIN` all procurement business operations.
- [x] Allow `MANAGER` managerial operations.
- [x] Allow `STAFF` submit and receipt operations.
- [x] Allow `MANAGER` and `STAFF` read operations.
- [x] Map access-denied errors to `403` `ProblemDetail`.

## Error handling

- [x] Add `ProblemDetailExceptionHandler` for access denied, validation, and
      unexpected errors.
- [x] Ensure `403` bodies contain no user IDs, roles, stack traces, or internal
      class names.
- [x] Add a stable `type` or `code` for authorization failures.

## Tests

- [x] Missing `X-User-Id` → `403`.
- [x] Missing roles → `403` for protected operations.
- [x] Unknown role only → `403`.
- [x] `MANAGER` allowed for each managerial operation.
- [x] `STAFF` allowed to submit and receive; denied create/approve/cancel.
- [x] `SYSTEM_ADMIN` denied every procurement business operation.
- [x] Duplicate identity headers → `403`.
- [x] Public endpoints work without identity headers.
- [x] Correlation ID appears in the response/log context.

## Documentation

- [x] Document the header contract and role matrix in the service `README.md`.
- [x] Record evidence in [`evidence.md`](./evidence.md).

## Review

- [x] `./gradlew clean build` passes.
- [x] No authentication/token code was added.
- [x] Diff reviewed and accepted.
