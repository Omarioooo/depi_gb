# PROC-002 — Evidence

## Sprint metadata

- Date: 2026-11-21
- Branch: `feat/procurement/proc-002-persistence`
- Commit: none
- Reviewer: pending

## 1. Empty-database migration

```bash
docker exec procurement-pg dropdb -U procurement procurement_db
docker exec procurement-pg createdb -U procurement procurement_db
PROCUREMENT_DB_URL=jdbc:postgresql://localhost:5432/procurement_db \
PROCUREMENT_DB_USERNAME=procurement \
PROCUREMENT_DB_PASSWORD=procurement \
SERVER_PORT=18081 \
./gradlew bootRun --console=plain
```

```text
> Task :compileJava UP-TO-DATE
> Task :processResources UP-TO-DATE
> Task :classes UP-TO-DATE
> Task :resolveMainClassName UP-TO-DATE

> Task :bootRun
2026-11-21T17:13:41.370+02:00  INFO 2618388 --- [procurement-service] [           main] c.d.procurement.ProcurementApplication   : Starting ProcurementApplication using Java 21.0.13.1 with PID 2618388 (/home/amr/Amr/projects/depi_grad/backend/services/Procurement/build/classes/java/main started by amr in /home/amr/Amr/projects/depi_grad/backend/services/Procurement)
2026-11-21T17:13:41.374+02:00  INFO 2618388 --- [procurement-service] [           main] c.d.procurement.ProcurementApplication   : No active profile set, falling back to 1 default profile: "default"
2026-11-21T17:13:42.495+02:00  INFO 2618388 --- [procurement-service] [           main] .s.d.r.c.RepositoryConfigurationDelegate : Bootstrapping Spring Data JPA repositories in DEFAULT mode.
2026-11-21T17:13:42.571+02:00  INFO 2618388 --- [procurement-service] [           main] .s.d.r.c.RepositoryConfigurationDelegate : Finished Spring Data repository scanning in 66 ms. Found 4 JPA repository interfaces.
2026-11-21T17:13:43.181+02:00  INFO 2618388 --- [procurement-service] [           main] o.s.b.w.embedded.tomcat.TomcatWebServer  : Tomcat initialized with port 18081 (http)
2026-11-21T17:13:43.195+02:00  INFO 2618388 --- [procurement-service] [           main] o.apache.catalina.core.StandardService   : Starting service [Tomcat]
2026-11-21T17:13:43.196+02:00  INFO 2618388 --- [procurement-service] [           main] o.apache.catalina.core.StandardEngine    : Starting Servlet engine: [Apache Tomcat/10.1.46]
2026-11-21T17:13:43.240+02:00  INFO 2618388 --- [procurement-service] [           main] o.a.c.c.C.[Tomcat].[localhost].[/]       : Initializing Spring embedded WebApplicationContext
2026-11-21T17:13:43.241+02:00  INFO 2618388 --- [procurement-service] [           main] w.s.c.ServletWebServerApplicationContext : Root WebApplicationContext: initialization completed in 1808 ms
2026-11-21T17:13:43.643+02:00  INFO 2618388 --- [procurement-service] [           main] com.zaxxer.hikari.HikariDataSource       : HikariPool-1 - Starting...
2026-11-21T17:13:43.915+02:00  INFO 2618388 --- [procurement-service] [           main] com.zaxxer.hikari.pool.HikariPool        : HikariPool-1 - Added connection org.postgresql.jdbc.PgConnection@50a7c72b
2026-11-21T17:13:43.917+02:00  INFO 2618388 --- [procurement-service] [           main] com.zaxxer.hikari.HikariDataSource       : HikariPool-1 - Start completed.
2026-11-21T17:13:43.956+02:00  INFO 2618388 --- [procurement-service] [           main] org.flywaydb.core.FlywayExecutor         : Database: jdbc:postgresql://localhost:5432/procurement_db (PostgreSQL 16.15)
2026-11-21T17:13:43.992+02:00  INFO 2618388 --- [procurement-service] [           main] o.f.c.i.s.JdbcTableSchemaHistory         : Schema history table "public"."flyway_schema_history" does not exist yet
2026-11-21T17:13:43.996+02:00  INFO 2618388 --- [procurement-service] [           main] o.f.core.internal.command.DbValidate     : Successfully validated 1 migration (execution time 00:00.017s)
2026-11-21T17:13:44.014+02:00  INFO 2618388 --- [procurement-service] [           main] o.f.c.i.s.JdbcTableSchemaHistory         : Creating Schema History table "public"."flyway_schema_history" ...
2026-11-21T17:13:44.337+02:00  INFO 2618388 --- [procurement-service] [           main] o.f.core.internal.command.DbMigrate      : Current version of schema "public": << Empty Schema >>
2026-11-21T17:13:44.354+02:00  INFO 2618388 --- [procurement-service] [           main] o.f.core.internal.command.DbMigrate      : Migrating schema "public" to version "1 - init"
2026-11-21T17:13:45.259+02:00  INFO 2618388 --- [procurement-service] [           main] o.f.core.internal.command.DbMigrate      : Successfully applied 1 migration to schema "public", now at version v1 (execution time 00:00.772s)
2026-11-21T17:13:45.399+02:00  INFO 2618388 --- [procurement-service] [           main] o.hibernate.jpa.internal.util.LogHelper  : HHH000204: Processing PersistenceUnitInfo [name: default]
2026-11-21T17:13:45.452+02:00  INFO 2618388 --- [procurement-service] [           main] org.hibernate.Version                    : HHH000412: Hibernate ORM core version 6.6.29.Final
2026-11-21T17:13:45.489+02:00  INFO 2618388 --- [procurement-service] [           main] o.h.c.internal.RegionFactoryInitiator    : HHH000026: Second-level cache disabled
2026-11-21T17:13:45.782+02:00  INFO 2618388 --- [procurement-service] [           main] o.s.o.j.p.SpringPersistenceUnitInfo      : No LoadTimeWeaver setup: ignoring JPA class transformer
2026-11-21T17:13:45.858+02:00  INFO 2618388 --- [procurement-service] [           main] org.hibernate.orm.connections.pooling    : HHH10001005: Database info:
	Database JDBC URL [Connecting through datasource 'HikariDataSource (HikariPool-1)']
	Database driver: undefined/unknown
	Database version: 16.15
	Autocommit mode: undefined/unknown
	Isolation level: undefined/unknown
	Minimum pool size: undefined/unknown
	Maximum pool size: undefined/unknown
2026-11-21T17:13:46.876+02:00  INFO 2618388 --- [procurement-service] [           main] o.h.e.t.j.p.i.JtaPlatformInitiator       : HHH000489: No JTA platform available (set 'hibernate.transaction.jta.platform' to enable JTA platform integration)
2026-11-21T17:13:46.924+02:00  INFO 2618388 --- [procurement-service] [           main] j.LocalContainerEntityManagerFactoryBean : Initialized JPA EntityManagerFactory for persistence unit 'default'
2026-11-21T17:13:48.117+02:00  INFO 2618388 --- [procurement-service] [           main] o.s.b.a.e.web.EndpointLinksResolver      : Exposing 2 endpoints beneath base path '/actuator'
2026-11-21T17:13:48.229+02:00  INFO 2618388 --- [procurement-service] [           main] o.s.b.w.embedded.tomcat.TomcatWebServer  : Tomcat started on port 18081 (http) with context path '/'
2026-11-21T17:13:48.245+02:00  INFO 2618388 --- [procurement-service] [           main] c.d.procurement.ProcurementApplication   : Started ProcurementApplication in 7.396 seconds (process running for 7.757)
```

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

## 3. Constraint evidence

The psql command inserted one valid order, line, and receipt, followed by deliberate
duplicate-idempotency, zero/negative quantity, and over-receipt inserts.

```text
INSERT 0 1
INSERT 0 1
INSERT 0 1
ERROR:  duplicate key value violates unique constraint "uk_goods_receipts_idempotency_key"
DETAIL:  Key (idempotency_key)=(duplicate-key) already exists.
ERROR:  new row for relation "purchase_order_lines" violates check constraint "ck_purchase_order_lines_ordered_quantity"
DETAIL:  Failing row contains (00000000-0000-0000-0000-000000000007, 00000000-0000-0000-0000-000000000001, 00000000-0000-0000-0000-000000000008, 0.000, 0.000, 2026-11-21 15:14:03.175816+00, 2026-11-21 15:14:03.175816+00, 0).
ERROR:  new row for relation "purchase_order_lines" violates check constraint "ck_purchase_order_lines_ordered_quantity"
DETAIL:  Failing row contains (00000000-0000-0000-0000-000000000009, 00000000-0000-0000-0000-000000000001, 00000000-0000-0000-0000-000000000010, -1.000, 0.000, 2026-11-21 15:14:03.17628+00, 2026-11-21 15:14:03.17628+00, 0).
ERROR:  new row for relation "purchase_order_lines" violates check constraint "ck_purchase_order_lines_received_quantity"
DETAIL:  Failing row contains (00000000-0000-0000-0000-000000000011, 00000000-0000-0000-0000-000000000001, 00000000-0000-0000-0000-000000000012, 1.000, 1.001, 2026-11-21 15:14:03.176707+00, 2026-11-21 15:14:03.176707+00, 0).
ERROR:  new row for relation "goods_receipts" violates check constraint "ck_goods_receipts_quantity"
DETAIL:  Failing row contains (00000000-0000-0000-0000-000000000013, 00000000-0000-0000-0000-000000000001, 00000000-0000-0000-0000-000000000003, 0.000, zero-receipt, PENDING, null, 0, null, evidence-user, 2026-11-21 15:14:03.177074+00, 2026-11-21 15:14:03.177074+00, null, 0).
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
2026-11-21T17:14:22.810+02:00  INFO 2619496 --- [procurement-service] [ionShutdownHook] o.s.b.w.e.tomcat.GracefulShutdown        : Commencing graceful shutdown. Waiting for active requests to complete
2026-11-21T17:14:22.815+02:00  INFO 2619496 --- [procurement-service] [tomcat-shutdown] o.s.b.w.e.tomcat.GracefulShutdown        : Graceful shutdown complete
2026-11-21T17:14:22.821+02:00  INFO 2619496 --- [procurement-service] [ionShutdownHook] j.LocalContainerEntityManagerFactoryBean : Closing JPA EntityManagerFactory for persistence unit 'default'
2026-11-21T17:14:22.824+02:00  INFO 2619496 --- [procurement-service] [ionShutdownHook] com.zaxxer.hikari.HikariDataSource       : HikariPool-1 - Shutdown initiated...
2026-11-21T17:14:22.829+02:00  INFO 2619496 --- [procurement-service] [ionShutdownHook] com.zaxxer.hikari.HikariDataSource       : HikariPool-1 - Shutdown completed.
2026-11-21T17:14:22.843+02:00  INFO 2619496 --- [procurement-service] [ionShutdownHook] j.LocalContainerEntityManagerFactoryBean : Closing JPA EntityManagerFactory for persistence unit 'default'
2026-11-21T17:14:22.844+02:00  INFO 2619496 --- [procurement-service] [ionShutdownHook] com.zaxxer.hikari.HikariDataSource       : HikariPool-2 - Shutdown initiated...
2026-11-21T17:14:22.849+02:00  INFO 2619496 --- [procurement-service] [ionShutdownHook] com.zaxxer.hikari.HikariDataSource       : HikariPool-2 - Shutdown completed.
> Task :test

BUILD SUCCESSFUL in 16s
5 actionable tasks: 5 executed
Consider enabling configuration cache to speed up this build: https://docs.gradle.org/9.6.0/userguide/configuration_cache_enabling.html
```

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
2026-11-21T17:15:02.474+02:00  INFO 2620975 --- [procurement-service] [ionShutdownHook] o.s.b.w.e.tomcat.GracefulShutdown        : Commencing graceful shutdown. Waiting for active requests to complete
2026-11-21T17:15:02.479+02:00  INFO 2620975 --- [procurement-service] [tomcat-shutdown] o.s.b.w.e.tomcat.GracefulShutdown        : Graceful shutdown complete
2026-11-21T17:15:02.499+02:00  INFO 2620975 --- [procurement-service] [ionShutdownHook] j.LocalContainerEntityManagerFactoryBean : Closing JPA EntityManagerFactory for persistence unit 'default'
2026-11-21T17:15:02.502+02:00  INFO 2620975 --- [procurement-service] [ionShutdownHook] com.zaxxer.hikari.HikariDataSource       : HikariPool-1 - Shutdown initiated...
2026-11-21T17:15:02.509+02:00  INFO 2620975 --- [procurement-service] [ionShutdownHook] com.zaxxer.hikari.HikariDataSource       : HikariPool-1 - Shutdown completed.
2026-11-21T17:15:02.533+02:00  INFO 2620975 --- [procurement-service] [ionShutdownHook] j.LocalContainerEntityManagerFactoryBean : Closing JPA EntityManagerFactory for persistence unit 'default'
2026-11-21T17:15:02.535+02:00  INFO 2620975 --- [procurement-service] [ionShutdownHook] com.zaxxer.hikari.HikariDataSource       : HikariPool-2 - Shutdown initiated...
2026-11-21T17:15:02.539+02:00  INFO 2620975 --- [procurement-service] [ionShutdownHook] com.zaxxer.hikari.HikariDataSource       : HikariPool-2 - Shutdown completed.
> Task :test
> Task :check
> Task :build

BUILD SUCCESSFUL in 15s
8 actionable tasks: 8 executed
Consider enabling configuration cache to speed up this build: https://docs.gradle.org/9.6.0/userguide/configuration_cache_enabling.html
```

## 6. Git state

```bash
git diff --stat
git status --short
```

```text
 backend/services/Procurement/build.gradle.kts           |  2 +-
 .../Procurement/src/main/java/org/example/Main.java     | 17 -----------------
 2 files changed, 1 insertion(+), 18 deletions(-)
 M backend/services/Procurement/build.gradle.kts
 D backend/services/Procurement/src/main/java/org/example/Main.java
?? backend/services/Procurement/README.md
?? backend/services/Procurement/docs/
?? backend/services/Procurement/src/main/java/com/
?? backend/services/Procurement/src/main/resources/
?? backend/services/Procurement/src/test/
?? docs/
```

Most Procurement service files were already untracked in the starting worktree, so
`git diff --stat` does not include them.

## 7. Known limitations

- Persistence tests require the runbook PostgreSQL 16 container on port 5432.
- HTTP APIs, authorization, use cases, receiving, clients, and event publishing
  remain intentionally unimplemented for later slices.

## 8. Review sign-off

- [x] All acceptance criteria verified.
- [x] Evidence is reproducible.
- [x] No unrelated changes.
- [ ] Approved to commit and push.
