# PROC-003 — Evidence

## Sprint metadata

- Date: 2026-11-22
- Branch: `feat/proc-003-authorization`
- Commit: `55c2b57` (implementation); evidence recorded in the follow-up docs commit
- Reviewer: pending

## 1. Role matrix

Command:

```bash
./gradlew test --tests 'com.depigb.procurement.adapter.in.web.security.*'
```

```text
> Task :test

BUILD SUCCESSFUL in 19s
4 actionable tasks: 2 executed, 2 up-to-date
```

Test result summaries from `build/test-results/test/`:

```text
AuthorizationRulesTest: tests=7 skipped=0 failures=0 errors=0
GatewayHeaderFilterTest: tests=15 skipped=0 failures=0 errors=0
ProblemDetailExceptionHandlerTest: tests=3 skipped=0 failures=0 errors=0
RoleMatrixTest: tests=28 skipped=0 failures=0 errors=0
```

Role-matrix cases (`RoleMatrixTest`):

```text
managerCanPerformEveryProcurementOperation: CREATE_DRAFT, EDIT_DRAFT, READ_ORDERS,
  READ_RECEIPTS, SUBMIT_ORDER, APPROVE_ORDER, CANCEL_ORDER, RECORD_RECEIPT,
  RETRY_RECEIPT -> 200
staffCanPerformOperationalTasks: READ_ORDERS, READ_RECEIPTS, SUBMIT_ORDER,
  RECORD_RECEIPT, RETRY_RECEIPT -> 200
staffCannotPerformManagerialOperations: CREATE_DRAFT, EDIT_DRAFT, APPROVE_ORDER,
  CANCEL_ORDER -> 403
systemAdministratorIsDeniedEveryProcurementOperation: all nine operations -> 403
systemAdministratorWithStaffRoleGetsOnlyStaffAccess: READ_ORDERS -> 200,
  CREATE_DRAFT -> 403
```

Rule-layer cases (`AuthorizationRulesTest`):

```text
managerCanPerformEveryOperation()
staffCanPerformOnlyOperationalTasks()
systemAdministratorIsDeniedEveryOperation()
systemAdministratorWithStaffRoleGetsOnlyStaffAccess()
userWithoutKnownRolesIsDenied()
requireThrowsWhenOperationIsDenied()
requireAllowsWhenOperationIsPermitted()
```

## 2. Missing and spoofed context

`GatewayHeaderFilterTest` covers the boundary:

```text
readsGatewayIdentityAndCorrelationHeaders()
generatesCorrelationIdWhenGatewayHeaderIsMissing()
missingUserIdIsRejected()
blankUserIdIsRejected()
missingRolesAreRejected()
unknownRolesOnlyAreRejected()
duplicateUserIdHeadersAreRejected()
duplicateRoleHeadersAreRejected()
queryParametersCannotOverrideGatewayIdentity()
publicStatusEndpointDoesNotRequireIdentityHeaders()
publicEndpointStillPropagatesCorrelationId()
systemAdministratorOnlyIsRejectedAtTheBoundary()
correlationIdIsClearedFromMdcAfterRequest()
forbiddenResponseDoesNotLeakIdentityOrInternals()
```

The spoofing checks prove that query parameters do not replace gateway headers
and that duplicate identity headers are rejected instead of silently choosing
one. `unknownRolesOnlyAreRejected` proves an unrecognized role cannot grant
access.

`ProblemDetailExceptionHandlerTest` also covers the shared error contract:

```text
validationFailureReturnsSafeProblemDetail: 400, VALIDATION_FAILED, field errors
unexpectedFailureReturnsGenericProblemDetail: 500, INTERNAL_ERROR, no exception text
unknownPathReturnsNotFoundInsteadOfInternalError: 404, NOT_FOUND
```

## 3. Example 403

Application started with:

```bash
SERVER_PORT=18082 APP_ENVIRONMENT=local APP_VERSION=1.0-SNAPSHOT ./gradlew bootRun
```

Missing user context:

```bash
curl -i -H 'X-Correlation-Id: missing-context-corr' \
  http://localhost:18082/api/v1/procurement/purchase-orders
```

```text
HTTP/1.1 403
X-Correlation-Id: missing-context-corr
Content-Type: application/problem+json;charset=UTF-8

{"type":"https://depigb.example/problems/access-denied","title":"Forbidden","status":403,"detail":"Missing trusted user context.","instance":"/api/v1/procurement/purchase-orders","code":"ACCESS_DENIED"}
```

`SYSTEM_ADMIN` only:

```bash
curl -i -H 'X-Correlation-Id: system-admin-corr' \
  -H 'X-User-Id: 00000000-0000-0000-0000-000000000001' \
  -H 'X-User-Roles: SYSTEM_ADMIN' \
  http://localhost:18082/api/v1/procurement/purchase-orders
```

```text
HTTP/1.1 403
X-Correlation-Id: system-admin-corr
Content-Type: application/problem+json;charset=UTF-8

{"type":"https://depigb.example/problems/access-denied","title":"Forbidden","status":403,"detail":"Access denied.","instance":"/api/v1/procurement/purchase-orders","code":"ACCESS_DENIED"}
```

Duplicate `X-User-Id` headers:

```bash
curl -i -H 'X-Correlation-Id: duplicate-corr' \
  -H 'X-User-Id: 00000000-0000-0000-0000-000000000003' \
  -H 'X-User-Id: 00000000-0000-0000-0000-000000000004' \
  -H 'X-User-Roles: MANAGER' \
  http://localhost:18082/api/v1/procurement/purchase-orders
```

```text
HTTP/1.1 403
X-Correlation-Id: duplicate-corr
Content-Type: application/problem+json;charset=UTF-8

{"type":"https://depigb.example/problems/access-denied","title":"Forbidden","status":403,"detail":"Ambiguous trusted user context.","instance":"/api/v1/procurement/purchase-orders","code":"ACCESS_DENIED"}
```

A valid `MANAGER` context passes the filter; the path has no business endpoint
until PROC-004, so the request reaches routing and returns `404`:

```text
HTTP/1.1 404
X-Correlation-Id: manager-passes-corr
Content-Type: application/problem+json

{"type":"https://depigb.example/problems/not-found","title":"Not Found","status":404,"detail":"Resource not found.","instance":"/api/v1/procurement/purchase-orders","code":"NOT_FOUND"}
```

## 4. Correlation ID

Public status endpoint with a supplied correlation ID:

```bash
curl -i -H 'X-Correlation-Id: test-correlation-1' \
  http://localhost:18082/api/procurement/status
```

```text
HTTP/1.1 200
X-Correlation-Id: test-correlation-1
Content-Type: application/json

{"service":"procurement-service","status":"UP","version":"1.0-SNAPSHOT","environment":"local"}
```

Matching log lines show the correlation ID in the MDC:

```text
2026-11-21T18:45:47.851+02:00  WARN [correlationId:missing-context-corr] 2781700 --- [procurement-service] [io-18082-exec-3] c.d.p.a.i.w.s.GatewayHeaderFilter : Rejected request to /api/v1/procurement/purchase-orders: Missing trusted user context.
2026-11-21T18:45:47.864+02:00  WARN [correlationId:system-admin-corr] 2781700 --- [procurement-service] [io-18082-exec-2] c.d.p.a.i.w.s.GatewayHeaderFilter : Rejected request to /api/v1/procurement/purchase-orders: Access denied.
2026-11-21T18:45:47.889+02:00  WARN [correlationId:duplicate-corr] 2781700 --- [procurement-service] [io-18082-exec-6] c.d.p.a.i.w.s.GatewayHeaderFilter : Rejected request to /api/v1/procurement/purchase-orders: Ambiguous trusted user context.
```

When the header is absent, the filter generates a UUID and echoes it in the
`X-Correlation-Id` response header; `generatesCorrelationIdWhenGatewayHeaderIsMissing`
asserts this.

## 5. Build

```bash
./gradlew clean build
```

```text
> Task :test
> Task :check
> Task :build

BUILD SUCCESSFUL in 39s
8 actionable tasks: 8 executed
```

The build ran 66 tests with 0 failures and 0 errors, including the existing
PROC-001/PROC-002 tests.

## 6. Files changed

```text
backend/services/Procurement/README.md
backend/services/Procurement/src/main/resources/application.yml
backend/services/Procurement/src/main/java/com/depigb/procurement/config/SecurityConfig.java
backend/services/Procurement/src/main/java/com/depigb/procurement/adapter/in/web/ProblemDetailExceptionHandler.java
backend/services/Procurement/src/main/java/com/depigb/procurement/adapter/in/web/security/
  AuthorizationDeniedException.java
  AuthorizationRules.java
  GatewayHeaderFilter.java
  Role.java
  UserContext.java
  package-info.java
backend/services/Procurement/src/test/java/com/depigb/procurement/adapter/in/web/security/
  AuthorizationRulesTest.java
  AuthorizationTestController.java
  GatewayHeaderFilterTest.java
  ProblemDetailExceptionHandlerTest.java
  RoleMatrixTest.java
backend/services/Procurement/docs/plan/02-proc-003-authorization/
backend/services/Procurement/docs/plan/README.md
docs/scope/scope.md
```

## 7. Known limitations

- Business endpoints are introduced in PROC-004 and later. The role matrix is
  exercised through a test-only controller that calls `AuthorizationRules`;
  the live checks use a protected path that currently returns `404` after the
  filter accepts a valid role.
- Header trust is a deployment property. The service must only be reachable
  through the gateway or from the internal network.
- Authentication and token validation are intentionally out of scope; no
  Spring Security or Keycloak code was added.
- `SYSTEM_ADMIN` is rejected at the filter when it is the only role. A user
  who also has `MANAGER` or `STAFF` is evaluated by the normal matrix.

## 8. Review sign-off

- [x] All acceptance criteria verified.
- [x] No internal details leak in `403` responses.
- [x] Diff reviewed and accepted.
