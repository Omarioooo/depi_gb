# PROC-002 — Runbook

Detailed, reproducible steps for this sprint.

## Preconditions

- PROC-001 is `done` (see [`../README.md`](../README.md) pre-flight).
- Docker is available.
- Port `5432` is free, or change the port in the commands below.
- Decisions D2, D3, D4, D6, and D7 are reviewed. They can remain `accepted` later,
  but the schema direction must not change after this sprint.

## 1. Prepare

```bash
cd backend/services/Procurement
git switch main
git pull
git switch -c feat/proc-002-persistence
./gradlew clean test
```

Expected: `BUILD SUCCESSFUL`.

## 2. Start PostgreSQL

```bash
docker run --rm -d --name procurement-pg \
  -e POSTGRES_DB=procurement_db \
  -e POSTGRES_USER=procurement \
  -e POSTGRES_PASSWORD=procurement \
  -p 5432:5432 postgres:16
```

Wait until it accepts connections:

```bash
until docker exec procurement-pg pg_isready -U procurement -d procurement_db; do sleep 1; done
```

## 3. Wire configuration

Edit `src/main/resources/application.yml`:

```yaml
spring:
  datasource:
    url: ${PROCUREMENT_DB_URL:jdbc:postgresql://localhost:5432/procurement_db}
    username: ${PROCUREMENT_DB_USERNAME:procurement}
    password: ${PROCUREMENT_DB_PASSWORD:procurement}
  jpa:
    open-in-view: false
    hibernate:
      ddl-auto: validate
    properties:
      hibernate:
        jdbc:
          time_zone: UTC
  flyway:
    enabled: true
    locations: classpath:db/migration
```

Remove only the datasource exclusion:

```yaml
  autoconfigure:
    exclude:
      - org.springframework.boot.autoconfigure.amqp.RabbitAutoConfiguration
```

Leave the RabbitMQ exclusion until PROC-009.

## 4. Write the migration

Create `src/main/resources/db/migration/V1__init.sql` using
[`schema.md`](./schema.md) as the contract.

Rules:

- UUID keys use `gen_random_uuid()` (available in PostgreSQL 13+ via `pgcrypto`; add
  `CREATE EXTENSION IF NOT EXISTS pgcrypto;` if needed).
- All quantities are `NUMERIC(19,3)`.
- All timestamps are `TIMESTAMPTZ NOT NULL DEFAULT now()`.
- Add check constraints for status and quantity ranges.
- Add indexes for the queries planned in PROC-008.

## 5. Add domain models

Create pure Java models under `domain/model/`:

- `PurchaseOrderStatus`
- `ReceiptStatus`
- `PurchaseOrder`
- `PurchaseOrderLine`
- `GoodsReceipt`

No Spring, JPA, Jackson, or Lombok imports in this package.

## 6. Add persistence adapters

Under `adapter/out/persistence/`:

- JPA entities with `@Version` fields.
- Spring Data repositories.
- Mappers.
- Repository port implementations.

Keep the application ports free of JPA types:

```java
public interface PurchaseOrderRepositoryPort {
    PurchaseOrder save(PurchaseOrder order);
    Optional<PurchaseOrder> findById(UUID id);
    // ...
}
```

## 7. Verify the migration

Run the application with the container settings:

```bash
PROCUREMENT_DB_URL=jdbc:postgresql://localhost:5432/procurement_db \
PROCUREMENT_DB_USERNAME=procurement \
PROCUREMENT_DB_PASSWORD=procurement \
./gradlew bootRun
```

Expected log lines:

```text
Successfully applied 1 migration to schema "public"
Started ProcurementApplication
```

Stop the app with `Ctrl+C`.

## 8. Inspect the live schema

```bash
PGPASSWORD=procurement psql -h localhost -U procurement -d procurement_db \
  -c '\d+ purchase_orders' \
  -c '\d+ purchase_order_lines' \
  -c '\d+ goods_receipts' \
  -c '\d+ event_publications'
```

Confirm every column, constraint, and index in [`schema.md`](./schema.md).

## 9. Run the tests

```bash
./gradlew clean test
./gradlew clean build
```

Expected: all tests green and `BUILD SUCCESSFUL`.

If you use Testcontainers for repository tests, Docker must be running. Reuse the
same `postgres:16` image to keep startup fast.

## 10. Clean up

```bash
docker rm -f procurement-pg
git status
```

## Failure recovery

| Symptom | Likely cause | Fix |
|---|---|---|
| `Connection refused` | PostgreSQL not ready or wrong port | Re-run the `pg_isready` loop; check `docker ps` |
| `Schema-validation: missing table` | Flyway did not run | Check `spring.flyway.enabled`, migration path, and startup logs |
| `gen_random_uuid does not exist` | `pgcrypto` extension missing | Add `CREATE EXTENSION IF NOT EXISTS pgcrypto;` to V1 |
| Duplicate migration checksum | V1 edited after being applied | Drop and recreate the local database; never edit applied migrations |
| Tests pass locally but not in CI | Testcontainers/Docker unavailable | Document the requirement or use a CI service container |
