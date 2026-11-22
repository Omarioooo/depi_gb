# Sprint 02 — PROC-003: Gateway Authorization Boundary — Runbook

## Preconditions

- PROC-001 is `done`.
- D3 (error contract) and D4 (authorization model) are `accepted` in
  [`../decisions.md`](../decisions.md).
- PostgreSQL 16 is available for the full application context tests.

## 1. Prepare

```bash
cd backend/services/Procurement
git switch service/procurement
git pull
git switch -c feat/proc-003-authorization
./gradlew clean test
```

## 2. Start dependencies

```bash
docker run --rm -d --name procurement-pg \
  -e POSTGRES_DB=procurement_db \
  -e POSTGRES_USER=procurement \
  -e POSTGRES_PASSWORD=procurement \
  -p 5432:5432 postgres:16
```

No RabbitMQ is needed; PROC-003 does not publish events.

## 3. Implement

Work through [`tasks.md`](./tasks.md). Keep the build green after each group:

```bash
./gradlew test
```

The authorization boundary has three parts:

1. `GatewayHeaderFilter` reads and validates the trusted headers, stores a
   request-scoped `UserContext`, and manages the correlation ID in the MDC.
2. `AuthorizationRules` is the single place that maps operations to roles.
3. `ProblemDetailExceptionHandler` turns authorization and validation failures
   into safe RFC 7807 responses.

## 4. Verify

| Check | Command | Expected |
|---|---|---|
| Build | `./gradlew clean build` | `BUILD SUCCESSFUL` |
| Tests | `./gradlew clean test` | all green |
| Missing context | `curl -i http://localhost:18082/api/v1/procurement/purchase-orders` | `403`, `application/problem+json` |
| System admin | `curl -i -H 'X-User-Id: <uuid>' -H 'X-User-Roles: SYSTEM_ADMIN' ...` | `403`, no identity in body |
| Correlation | `curl -i -H 'X-Correlation-Id: test-correlation-1' http://localhost:18082/api/procurement/status` | echoed header and matching log line |

## 5. Capture evidence

Fill [`evidence.md`](./evidence.md) with real commands and output. Do not paste
fabricated or truncated results.

## 6. Clean up

```bash
docker rm -f procurement-pg 2>/dev/null || true
git status
```
