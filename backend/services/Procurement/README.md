# Procurement Service

Java 21 / Spring Boot service for supplier purchase orders and goods receiving.
This service owns `procurement_db`; other services are reached through documented
APIs or RabbitMQ events.

## Delivery plan

The sprint-by-sprint plan for the remaining slices lives in
[`docs/plan/`](docs/plan/README.md). Each sprint directory contains a brief, task
checklist, acceptance criteria, and an evidence template.

## Requirements

- Java 21
- Docker (for the service image and local PostgreSQL 16)

The Gradle wrapper is committed, so a separate Gradle installation is not needed.

## Build and test

```bash
cd backend/services/Procurement
./gradlew clean build
```

## Run locally

```bash
docker run --rm -d --name procurement-pg \
  -e POSTGRES_DB=procurement_db \
  -e POSTGRES_USER=procurement \
  -e POSTGRES_PASSWORD=procurement \
  -p 5432:5432 postgres:16

PROCUREMENT_DB_URL=jdbc:postgresql://localhost:5432/procurement_db \
PROCUREMENT_DB_USERNAME=procurement \
PROCUREMENT_DB_PASSWORD=procurement \
SERVER_PORT=8080 \
APP_ENVIRONMENT=local \
APP_VERSION=1.0-SNAPSHOT \
./gradlew bootRun
```

Runtime configuration comes from environment variables:

| Variable | Default | Purpose |
|---|---|---|
| `SERVER_PORT` | `8080` | HTTP port |
| `APP_ENVIRONMENT` | `local` | Environment name reported by `/api/procurement/status` |
| `APP_VERSION` | `1.0-SNAPSHOT` | Version reported by `/api/procurement/status` |
| `PROCUREMENT_DB_URL` | `jdbc:postgresql://localhost:5432/procurement_db` | PostgreSQL JDBC URL |
| `PROCUREMENT_DB_USERNAME` | `procurement` | PostgreSQL user |
| `PROCUREMENT_DB_PASSWORD` | `procurement` | PostgreSQL password; local default only |

### Database migrations

This service exclusively owns `procurement_db`. Flyway applies forward-only
migrations from `src/main/resources/db/migration` during startup. To verify the
current migration against the runbook database:

```bash
./gradlew bootRun
docker exec procurement-pg psql -U procurement -d procurement_db -c '\d+ purchase_orders'
```

Never edit a migration after it has been applied; add a new migration instead.

### Health and status

```bash
curl http://localhost:8080/actuator/health
curl http://localhost:8080/actuator/health/liveness
curl http://localhost:8080/actuator/health/readiness
curl http://localhost:8080/api/procurement/status
```

## Docker

```bash
docker build -t procurement-service:local .
docker run --rm -p 8080:8080 procurement-service:local
```

The runtime image runs as a non-root user.

## Package layout (hexagonal)

```text
com.depigb.procurement
├── domain/                  Pure business model, rules, and exceptions
├── application/
│   ├── port/in/             Use case contracts exposed to inbound adapters
│   ├── port/out/            Persistence, external service, and event contracts
│   └── service/             Use case implementations
├── adapter/
│   ├── in/web/              REST controllers and HTTP DTOs
│   └── out/
│       ├── persistence/     JPA entities, repositories, mappers
│       ├── client/          Supplier, Product, Inventory HTTP clients
│       └── messaging/       Procurement event publisher
└── config/                  Spring wiring between ports and adapters
```

Dependency direction: `adapter` → `application` → `domain`. The domain layer
does not depend on Spring or infrastructure.

> `RabbitAutoConfiguration` remains excluded until PROC-009.
