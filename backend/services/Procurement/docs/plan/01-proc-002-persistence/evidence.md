# PROC-002 — Evidence

## Sprint metadata

- Date: 2026-11-21
- Branch: `feat/procurement/proc-002-persistence`
- Database: PostgreSQL 16.15 (`postgres:16` container `procurement-pg`)
- Commit: feature branch tip (PR into `service/procurement`)
- Reviewer: pending PR review

## 1. Empty-database migration

```bash
docker exec procurement-pg dropdb -U procurement --if-exists procurement_db
docker exec procurement-pg createdb -U procurement procurement_db
PROCUREMENT_DB_URL=jdbc:postgresql://localhost:5432/procurement_db \
PROCUREMENT_DB_USERNAME=procurement \
PROCUREMENT_DB_PASSWORD=procurement \
SERVER_PORT=18081 \
./gradlew bootRun --console=plain
```

```text
2026-11-21T18:27:26.535+02:00  INFO 2747115 --- [procurement-service] [           main] org.flywaydb.core.FlywayExecutor         : Database: jdbc:postgresql://localhost:5432/procurement_db (PostgreSQL 16.15)
2026-11-21T18:27:26.569+02:00  INFO 2747115 --- [procurement-service] [           main] o.f.c.i.s.JdbcTableSchemaHistory         : Schema history table "public"."flyway_schema_history" does not exist yet
2026-11-21T18:27:26.572+02:00  INFO 2747115 --- [procurement-service] [           main] o.f.core.internal.command.DbValidate     : Successfully validated 1 migration (execution time 00:00.014s)
2026-11-21T18:27:26.725+02:00  INFO 2747115 --- [procurement-service] [           main] o.f.core.internal.command.DbMigrate      : Current version of schema "public": << Empty Schema >>
2026-11-21T18:27:26.740+02:00  INFO 2747115 --- [procurement-service] [           main] o.f.core.internal.command.DbMigrate      : Migrating schema "public" to version "1 - init"
2026-11-21T18:27:28.108+02:00  INFO 2747115 --- [procurement-service] [           main] o.f.core.internal.command.DbMigrate      : Successfully applied 1 migration to schema "public", now at version v1 (execution time 00:01.227s)
2026-11-21T18:27:29.735+02:00  INFO 2747115 --- [procurement-service] [           main] j.LocalContainerEntityManagerFactoryBean : Initialized JPA EntityManagerFactory for persistence unit 'default'
2026-11-21T18:27:30.865+02:00  INFO 2747115 --- [procurement-service] [           main] o.s.b.w.embedded.tomcat.TomcatWebServer  : Tomcat started on port 18081 (http) with context path '/'
2026-11-21T18:27:30.881+02:00  INFO 2747115 --- [procurement-service] [           main] c.d.procurement.ProcurementApplication   : Started ProcurementApplication in 7.713 seconds (process running for 8.07)
```

Health check against the started application:

```bash
curl --fail --silent --show-error --write-out '\nHTTP %{http_code}\n' \
  http://127.0.0.1:18081/actuator/health
```

```text
{"status":"UP","groups":["liveness","readiness"]}
HTTP 200
```

Result: Flyway saw an empty schema, applied `V1 - init`, Hibernate validated the
mapping, and the application started.

## 2. Live schema inspection

```bash
docker exec procurement-pg psql -U procurement -d procurement_db \
  -c '\d+ purchase_orders' \
  -c '\d+ purchase_order_lines' \
  -c '\d+ goods_receipts' \
  -c '\d+ event_publications'
```

```text
                                                      Table "public.purchase_orders"
    Column    |           Type           | Collation | Nullable |      Default      | Storage  | Compression | Stats target | Description 
--------------+--------------------------+-----------+----------+-------------------+----------+-------------+--------------+-------------
 id           | uuid                     |           | not null | gen_random_uuid() | plain    |             |              | 
 supplier_id  | uuid                     |           | not null |                   | plain    |             |              | 
 status       | character varying(32)    |           | not null |                   | extended |             |              | 
 created_by   | character varying(255)   |           | not null |                   | extended |             |              | 
 created_at   | timestamp with time zone |           | not null | now()             | plain    |             |              | 
 updated_at   | timestamp with time zone |           | not null | now()             | plain    |             |              | 
 submitted_at | timestamp with time zone |           |          |                   | plain    |             |              | 
 approved_at  | timestamp with time zone |           |          |                   | plain    |             |              | 
 cancelled_at | timestamp with time zone |           |          |                   | plain    |             |              | 
 completed_at | timestamp with time zone |           |          |                   | plain    |             |              | 
 version      | bigint                   |           | not null | 0                 | plain    |             |              | 
Indexes:
    "purchase_orders_pkey" PRIMARY KEY, btree (id)
    "idx_purchase_orders_created_at" btree (created_at DESC)
    "idx_purchase_orders_status_created_at" btree (status, created_at DESC)
    "idx_purchase_orders_supplier_created_at" btree (supplier_id, created_at DESC)
Check constraints:
    "ck_purchase_orders_status" CHECK (status::text = ANY (ARRAY['DRAFT'::character varying, 'SUBMITTED'::character varying, 'APPROVED'::character varying, 'PARTIALLY_RECEIVED'::character varying, 'COMPLETED'::character varying, 'CANCELLED'::character varying]::text[]))
Referenced by:
    TABLE "goods_receipts" CONSTRAINT "goods_receipts_purchase_order_id_fkey" FOREIGN KEY (purchase_order_id) REFERENCES purchase_orders(id)
    TABLE "purchase_order_lines" CONSTRAINT "purchase_order_lines_purchase_order_id_fkey" FOREIGN KEY (purchase_order_id) REFERENCES purchase_orders(id) ON DELETE CASCADE
Access method: heap

                                                     Table "public.purchase_order_lines"
      Column       |           Type           | Collation | Nullable |      Default      | Storage | Compression | Stats target | Description 
-------------------+--------------------------+-----------+----------+-------------------+---------+-------------+--------------+-------------
 id                | uuid                     |           | not null | gen_random_uuid() | plain   |             |              | 
 purchase_order_id | uuid                     |           | not null |                   | plain   |             |              | 
 product_id        | uuid                     |           | not null |                   | plain   |             |              | 
 ordered_quantity  | numeric(19,3)            |           | not null |                   | main    |             |              | 
 received_quantity | numeric(19,3)            |           | not null | 0                 | main    |             |              | 
 created_at        | timestamp with time zone |           | not null | now()             | plain   |             |              | 
 updated_at        | timestamp with time zone |           | not null | now()             | plain   |             |              | 
 version           | bigint                   |           | not null | 0                 | plain   |             |              | 
Indexes:
    "purchase_order_lines_pkey" PRIMARY KEY, btree (id)
    "idx_purchase_order_lines_order_id" btree (purchase_order_id)
    "uk_purchase_order_lines_order_product" UNIQUE CONSTRAINT, btree (purchase_order_id, product_id)
Check constraints:
    "ck_purchase_order_lines_ordered_quantity" CHECK (ordered_quantity > 0::numeric)
    "ck_purchase_order_lines_received_quantity" CHECK (received_quantity >= 0::numeric AND received_quantity <= ordered_quantity)
Foreign-key constraints:
    "purchase_order_lines_purchase_order_id_fkey" FOREIGN KEY (purchase_order_id) REFERENCES purchase_orders(id) ON DELETE CASCADE
Referenced by:
    TABLE "goods_receipts" CONSTRAINT "goods_receipts_purchase_order_line_id_fkey" FOREIGN KEY (purchase_order_line_id) REFERENCES purchase_order_lines(id)
Access method: heap

                                                           Table "public.goods_receipts"
         Column         |           Type           | Collation | Nullable |      Default      | Storage  | Compression | Stats target | Description 
------------------------+--------------------------+-----------+----------+-------------------+----------+-------------+--------------+-------------
 id                     | uuid                     |           | not null | gen_random_uuid() | plain    |             |              | 
 purchase_order_id      | uuid                     |           | not null |                   | plain    |             |              | 
 purchase_order_line_id | uuid                     |           | not null |                   | plain    |             |              | 
 quantity               | numeric(19,3)            |           | not null |                   | main     |             |              | 
 idempotency_key        | character varying(255)   |           | not null |                   | extended |             |              | 
 status                 | character varying(32)    |           | not null |                   | extended |             |              | 
 inventory_operation_id | character varying(255)   |           |          |                   | extended |             |              | 
 attempt_count          | integer                  |           | not null | 0                 | plain    |             |              | 
 last_error             | text                     |           |          |                   | extended |             |              | 
 created_by             | character varying(255)   |           | not null |                   | extended |             |              | 
 created_at             | timestamp with time zone |           | not null | now()             | plain    |             |              | 
 updated_at             | timestamp with time zone |           | not null | now()             | plain    |             |              | 
 applied_at             | timestamp with time zone |           |          |                   | plain    |             |              | 
 version                | bigint                   |           | not null | 0                 | plain    |             |              | 
Indexes:
    "goods_receipts_pkey" PRIMARY KEY, btree (id)
    "idx_goods_receipts_line_id" btree (purchase_order_line_id)
    "idx_goods_receipts_order_created_at" btree (purchase_order_id, created_at DESC)
    "idx_goods_receipts_status_created_at" btree (status, created_at)
    "uk_goods_receipts_idempotency_key" UNIQUE CONSTRAINT, btree (idempotency_key)
Check constraints:
    "ck_goods_receipts_attempt_count" CHECK (attempt_count >= 0)
    "ck_goods_receipts_quantity" CHECK (quantity > 0::numeric)
    "ck_goods_receipts_status" CHECK (status::text = ANY (ARRAY['PENDING'::character varying, 'APPLIED'::character varying, 'FAILED'::character varying]::text[]))
Foreign-key constraints:
    "goods_receipts_purchase_order_id_fkey" FOREIGN KEY (purchase_order_id) REFERENCES purchase_orders(id)
    "goods_receipts_purchase_order_line_id_fkey" FOREIGN KEY (purchase_order_line_id) REFERENCES purchase_order_lines(id)
Access method: heap

                                                     Table "public.event_publications"
     Column     |           Type           | Collation | Nullable |      Default      | Storage  | Compression | Stats target | Description 
----------------+--------------------------+-----------+----------+-------------------+----------+-------------+--------------+-------------
 id             | uuid                     |           | not null | gen_random_uuid() | plain    |             |              | 
 event_id       | uuid                     |           | not null |                   | plain    |             |              | 
 event_type     | character varying(100)   |           | not null |                   | extended |             |              | 
 aggregate_type | character varying(50)    |           | not null |                   | extended |             |              | 
 aggregate_id   | uuid                     |           | not null |                   | plain    |             |              | 
 payload        | jsonb                    |           | not null |                   | extended |             |              | 
 status         | character varying(32)    |           | not null |                   | extended |             |              | 
 attempt_count  | integer                  |           | not null | 0                 | plain    |             |              | 
 last_error     | text                     |           |          |                   | extended |             |              | 
 created_at     | timestamp with time zone |           | not null | now()             | plain    |             |              | 
 published_at   | timestamp with time zone |           |          |                   | plain    |             |              | 
 version        | bigint                   |           | not null | 0                 | plain    |             |              | 
Indexes:
    "event_publications_pkey" PRIMARY KEY, btree (id)
    "idx_event_publications_aggregate" btree (aggregate_type, aggregate_id)
    "idx_event_publications_status_created_at" btree (status, created_at)
    "uk_event_publications_event_id" UNIQUE CONSTRAINT, btree (event_id)
Check constraints:
    "ck_event_publications_attempt_count" CHECK (attempt_count >= 0)
    "ck_event_publications_status" CHECK (status::text = ANY (ARRAY['PENDING'::character varying, 'PUBLISHED'::character varying, 'FAILED'::character varying]::text[]))
Access method: heap

```

## 3. Database constraint evidence

The script inserted one valid order, line, and receipt, then attempted a duplicate
idempotency key, zero and negative ordered quantities, received quantity above
ordered quantity, a null product, a zero receipt quantity, a null supplier, and an
invalid status.

```bash
docker exec -i procurement-pg psql -U procurement -d procurement_db -f - \
  < /tmp/opencode/proc-002-constraints.sql
```

```text
INSERT 0 1
INSERT 0 1
INSERT 0 1
psql:<stdin>:9: ERROR:  duplicate key value violates unique constraint "uk_goods_receipts_idempotency_key"
DETAIL:  Key (idempotency_key)=(duplicate-key) already exists.
psql:<stdin>:11: ERROR:  new row for relation "purchase_order_lines" violates check constraint "ck_purchase_order_lines_ordered_quantity"
DETAIL:  Failing row contains (77777777-7777-7777-7777-777777777777, 11111111-1111-1111-1111-111111111111, 88888888-8888-8888-8888-888888888888, 0.000, 0.000, 2026-11-21 16:27:48.433618+00, 2026-11-21 16:27:48.433618+00, 0).
psql:<stdin>:13: ERROR:  new row for relation "purchase_order_lines" violates check constraint "ck_purchase_order_lines_ordered_quantity"
DETAIL:  Failing row contains (99999999-9999-9999-9999-999999999999, 11111111-1111-1111-1111-111111111111, aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa, -1.000, 0.000, 2026-11-21 16:27:48.433834+00, 2026-11-21 16:27:48.433834+00, 0).
psql:<stdin>:15: ERROR:  new row for relation "purchase_order_lines" violates check constraint "ck_purchase_order_lines_received_quantity"
DETAIL:  Failing row contains (bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb, 11111111-1111-1111-1111-111111111111, cccccccc-cccc-cccc-cccc-cccccccccccc, 1.000, 1.001, 2026-11-21 16:27:48.434003+00, 2026-11-21 16:27:48.434003+00, 0).
psql:<stdin>:17: ERROR:  null value in column "product_id" of relation "purchase_order_lines" violates not-null constraint
DETAIL:  Failing row contains (dddddddd-dddd-dddd-dddd-dddddddddddd, 11111111-1111-1111-1111-111111111111, null, 1.000, 0.000, 2026-11-21 16:27:48.434273+00, 2026-11-21 16:27:48.434273+00, 0).
psql:<stdin>:19: ERROR:  new row for relation "goods_receipts" violates check constraint "ck_goods_receipts_quantity"
DETAIL:  Failing row contains (eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee, 11111111-1111-1111-1111-111111111111, 33333333-3333-3333-3333-333333333333, 0.000, zero-receipt, PENDING, null, 0, null, evidence-user, 2026-11-21 16:27:48.434463+00, 2026-11-21 16:27:48.434463+00, null, 0).
psql:<stdin>:21: ERROR:  null value in column "supplier_id" of relation "purchase_orders" violates not-null constraint
DETAIL:  Failing row contains (ffffffff-ffff-ffff-ffff-ffffffffffff, null, DRAFT, evidence-user, 2026-11-21 16:27:48.43472+00, 2026-11-21 16:27:48.43472+00, null, null, null, null, 0).
psql:<stdin>:23: ERROR:  new row for relation "purchase_orders" violates check constraint "ck_purchase_orders_status"
DETAIL:  Failing row contains (12121212-1212-1212-1212-121212121212, 22222222-2222-2222-2222-222222222222, BOGUS, evidence-user, 2026-11-21 16:27:48.434915+00, 2026-11-21 16:27:48.434915+00, null, null, null, null, 0).
```

## 4. Repository and migration tests

```bash
./gradlew clean test --console=plain
```

```text
> Task :clean
> Task :compileJava
> Task :processResources
> Task :classes
> Task :compileTestJava
> Task :processTestResources NO-SOURCE
> Task :testClasses
OpenJDK 64-Bit Server VM warning: Sharing is only supported for boot loader classes because bootstrap classpath has been appended
2026-11-21T18:28:21.516+02:00  INFO 2748674 --- [procurement-service] [ionShutdownHook] o.s.b.w.e.tomcat.GracefulShutdown        : Commencing graceful shutdown. Waiting for active requests to complete
2026-11-21T18:28:21.520+02:00  INFO 2748674 --- [procurement-service] [tomcat-shutdown] o.s.b.w.e.tomcat.GracefulShutdown        : Graceful shutdown complete
2026-11-21T18:28:21.527+02:00  INFO 2748674 --- [procurement-service] [ionShutdownHook] j.LocalContainerEntityManagerFactoryBean : Closing JPA EntityManagerFactory for persistence unit 'default'
2026-11-21T18:28:21.529+02:00  INFO 2748674 --- [procurement-service] [ionShutdownHook] com.zaxxer.hikari.HikariDataSource       : HikariPool-1 - Shutdown initiated...
2026-11-21T18:28:21.533+02:00  INFO 2748674 --- [procurement-service] [ionShutdownHook] com.zaxxer.hikari.HikariDataSource       : HikariPool-1 - Shutdown completed.
2026-11-21T18:28:21.553+02:00  INFO 2748674 --- [procurement-service] [ionShutdownHook] j.LocalContainerEntityManagerFactoryBean : Closing JPA EntityManagerFactory for persistence unit 'default'
2026-11-21T18:28:21.554+02:00  INFO 2748674 --- [procurement-service] [ionShutdownHook] com.zaxxer.hikari.HikariDataSource       : HikariPool-2 - Shutdown initiated...
2026-11-21T18:28:21.558+02:00  INFO 2748674 --- [procurement-service] [ionShutdownHook] com.zaxxer.hikari.HikariDataSource       : HikariPool-2 - Shutdown completed.
> Task :test

BUILD SUCCESSFUL in 16s
5 actionable tasks: 5 executed
Consider enabling configuration cache to speed up this build: https://docs.gradle.org/9.6.0/userguide/configuration_cache_enabling.html
```

Test result summary from `build/test-results/test/`:

```text
ServiceStatusControllerTest        tests=1 failures=0 errors=0
PersistenceIntegrationTest         tests=6 failures=0 errors=0
DomainModelTest                    tests=3 failures=0 errors=0
ProcurementApplicationTests        tests=3 failures=0 errors=0
Total                              tests=13 failures=0 errors=0
```

The persistence suite covers the clean-migration table set, order-with-lines round
trip, receipt round trip, duplicate idempotency key rejection, zero/negative
quantities, received quantity above ordered, null product, missing required order
values, and stale optimistic-lock updates.

## 5. Full build

```bash
./gradlew clean build --console=plain
```

```text
> Task :clean
> Task :compileJava
> Task :processResources
> Task :classes
> Task :resolveMainClassName
> Task :bootJar
> Task :jar
> Task :assemble
> Task :compileTestJava
> Task :processTestResources NO-SOURCE
> Task :testClasses
OpenJDK 64-Bit Server VM warning: Sharing is only supported for boot loader classes because bootstrap classpath has been appended
2026-11-21T18:28:44.286+02:00  INFO 2749597 --- [procurement-service] [ionShutdownHook] o.s.b.w.e.tomcat.GracefulShutdown        : Commencing graceful shutdown. Waiting for active requests to complete
2026-11-21T18:28:44.291+02:00  INFO 2749597 --- [procurement-service] [tomcat-shutdown] o.s.b.w.e.tomcat.GracefulShutdown        : Graceful shutdown complete
2026-11-21T18:28:44.305+02:00  INFO 2749597 --- [procurement-service] [ionShutdownHook] j.LocalContainerEntityManagerFactoryBean : Closing JPA EntityManagerFactory for persistence unit 'default'
2026-11-21T18:28:44.309+02:00  INFO 2749597 --- [procurement-service] [ionShutdownHook] com.zaxxer.hikari.HikariDataSource       : HikariPool-1 - Shutdown initiated...
2026-11-21T18:28:44.315+02:00  INFO 2749597 --- [procurement-service] [ionShutdownHook] com.zaxxer.hikari.HikariDataSource       : HikariPool-1 - Shutdown completed.
2026-11-21T18:28:44.334+02:00  INFO 2749597 --- [procurement-service] [ionShutdownHook] j.LocalContainerEntityManagerFactoryBean : Closing JPA EntityManagerFactory for persistence unit 'default'
2026-11-21T18:28:44.335+02:00  INFO 2749597 --- [procurement-service] [ionShutdownHook] com.zaxxer.hikari.HikariDataSource       : HikariPool-2 - Shutdown initiated...
2026-11-21T18:28:44.340+02:00  INFO 2749597 --- [procurement-service] [ionShutdownHook] com.zaxxer.hikari.HikariDataSource       : HikariPool-2 - Shutdown completed.
> Task :test
> Task :check
> Task :build

BUILD SUCCESSFUL in 19s
8 actionable tasks: 8 executed
Consider enabling configuration cache to speed up this build: https://docs.gradle.org/9.6.0/userguide/configuration_cache_enabling.html
```

## 6. Architecture checks

```bash
grep -rnE 'org\.springframework|jakarta\.persistence|javax\.persistence|lombok|com\.fasterxml' \
  src/main/java/com/depigb/procurement/domain/model
grep -rnE 'jakarta\.persistence|org\.springframework\.data' \
  src/main/java/com/depigb/procurement/application/port/out
```

Both commands returned no matches. Domain models and application ports are free of
Spring/JPA types.

## 7. Git state

```bash
git status --short
git diff --stat
```

```text
 M backend/services/Procurement/docs/plan/01-proc-002-persistence/README.md
 M backend/services/Procurement/docs/plan/01-proc-002-persistence/evidence.md
 M backend/services/Procurement/docs/plan/01-proc-002-persistence/tasks.md
 M backend/services/Procurement/docs/plan/README.md
 M docs/scope/scope.md
?? backend/services/Procurement/docs/plan/02-proc-003-authorization/
?? backend/services/Procurement/docs/plan/03-proc-004-draft-orders/
?? backend/services/Procurement/docs/plan/04-proc-005-lifecycle/
?? backend/services/Procurement/docs/plan/05-proc-006-reference-validation/
?? backend/services/Procurement/docs/plan/06-proc-007-goods-receiving/
?? backend/services/Procurement/docs/plan/07-proc-008-search-history/
?? backend/services/Procurement/docs/plan/08-proc-009-events-recovery/
?? backend/services/Procurement/docs/plan/09-proc-010-runtime-ops/
?? backend/services/Procurement/docs/plan/10-proc-011-integration/

 .../docs/plan/01-proc-002-persistence/README.md    |   2 +-
 .../docs/plan/01-proc-002-persistence/evidence.md  | 228 ++++++++++++---------
 .../docs/plan/01-proc-002-persistence/tasks.md     |   2 +-
 backend/services/Procurement/docs/plan/README.md   |   2 +-
 docs/scope/scope.md                                |   4 +-
 5 files changed, 136 insertions(+), 102 deletions(-)
```

The only untracked paths are the later sprint planning folders
(`02-proc-003-authorization` through `10-proc-011-integration`); they are not part
of PROC-002 and were left untouched. The implementation and tests were already
present in commit `63874eb`; this change refreshes evidence and marks the sprint
done.

## 8. Known limitations

- Persistence tests require the runbook PostgreSQL 16 container on port 5432.
- HTTP APIs, authorization, use cases, receiving, clients, and event publishing
  remain intentionally unimplemented for later slices.
- `event_publications` is created in V1 with its entity and Spring Data repository;
  the publishing port/adapter is intentionally deferred to PROC-009.

## 9. Review sign-off

- [x] AC-002-1: clean database migrated from empty state.
- [x] AC-002-2: required order columns rejected when null.
- [x] AC-002-3: order lines require a product and positive ordered quantity.
- [x] AC-002-4: receipts round-trip line, quantity, key, status, retry data, and Inventory operation ID.
- [x] AC-002-5: duplicate idempotency keys rejected by the database.
- [x] AC-002-6: live schema inspected in PostgreSQL.
- [x] `./gradlew clean test` green (13 tests).
- [x] `./gradlew clean build` green.
- [x] No REST, authorization, use-case, client, receiving, or event-publishing code added.
- [x] Approved to commit and push.
