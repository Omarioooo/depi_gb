# Project File Structure (Minimal)

The smallest layout that still delivers everything in [`project.md`](project.md)
and [`requirements.md`](requirements.md): six independently deployable services,
one database per service, a single API gateway, RabbitMQ events, a React dashboard,
and the required DevOps pipeline.

Stack: **.NET 10 / ASP.NET Core**, **React + TypeScript + Vite**, **PostgreSQL**,
**RabbitMQ**, **Keycloak**, **YARP**, **Nginx Ingress**, **Docker**,
**Kubernetes**, **Jenkins**, **Terraform**, **Ansible**, **Prometheus + Grafana**.

> `bin/`, `obj/`, `node_modules/`, and `dist/` are build output and are not shown.

---

## 1. Non-Negotiable Rules

| Rule | How this layout keeps it |
|---|---|
| Database per service | Each service has its own `<Service>DbContext` and database. No shared tables or cross-database joins. |
| Independent deployability | Each service has its own `.csproj` and `Dockerfile`. Jenkins builds and deploys them separately. |
| Single entry point | Only the Nginx Ingress is public; the gateway and services are cluster-internal. |
| Loose coupling | Cross-service references are IDs only. Services never reference another service's project. |
| Async events | Inventory publishes stock events; Notification and Reporting consume them. |
| Contract-first | `DepiGb.Shared` contains event contracts and transport helpers only — no business logic. |
| Failure safety | Inventory uses idempotency keys and an outbox; consumers deduplicate event IDs. |
| Config over code | Secrets come from environment variables and Kubernetes Secrets, never from `appsettings.json`. |

---

## 2. Deliverables Map

| Deliverable | Location |
|---|---|
| Inventory tracking service | `backend/services/Inventory/` plus Product, Supplier, Procurement |
| Low-stock alert service | `backend/services/Notification/` |
| Web dashboard | `frontend/` |
| CI/CD pipeline | `Jenkinsfile` |
| Kubernetes deployment | `deploy/k8s/` |
| Prometheus monitoring | `infra/monitoring/`, `deploy/k8s/monitoring.yaml` |
| Nginx for dashboard access | `deploy/k8s/ingress.yaml` (Nginx Ingress) |
| Terraform for AWS EC2, S3, VPC | `infra/terraform/` |
| Ansible configuration | `infra/ansible/` |
| Git repository | Repository root |

`requirements.md` splits inventory tracking into Product, Inventory, Supplier, and
Procurement bounded contexts. Notification covers alerts and Reporting covers reports.

---

## 3. Root

```text
depi_gb/
├── README.md                    # Quick start and service map
├── .gitignore                   # Build output, secrets, local state
├── .env.example                 # Local ports and placeholder secrets
├── docker-compose.yml           # Local stack: services, Postgres, RabbitMQ, Keycloak
├── Jenkinsfile                  # Build, test, image, push, deploy pipeline
├── documentation/               # Project brief, requirements, architecture, this file
├── backend/                     # .NET gateway, services, shared contracts, tests
├── frontend/                    # React dashboard
├── deploy/
│   └── k8s/                     # Kubernetes manifests
├── infra/
│   ├── terraform/               # AWS VPC, EC2, S3
│   ├── ansible/                 # Server and cluster configuration
│   ├── keycloak/                # Realm and role definitions
│   ├── postgres/                # Local database bootstrap
│   └── monitoring/              # Prometheus and Grafana configuration
└── scripts/                     # Local development and deployment helpers
```

---

## 4. Backend

```text
backend/
├── DepiGb.sln
├── gateway/
│   └── DepiGb.Gateway/
├── services/
│   ├── Product/
│   ├── Inventory/
│   ├── Supplier/
│   ├── Procurement/
│   ├── Notification/
│   └── Reporting/
├── shared/
│   └── DepiGb.Shared/
└── tests/
    └── DepiGb.Tests/
```

### 4.1 Standard service files

Every service uses the same eight-file layout. No extra layers or folders.

```text
backend/services/<Service>/
├── <Service>.csproj             # Project; references DepiGb.Shared
├── Program.cs                   # DI, EF Core, JWT auth, endpoints, health, metrics
├── appsettings.json             # DB, RabbitMQ, service URLs; no secrets
├── Dockerfile                   # Multi-stage, non-root runtime image
├── Models.cs                    # Entities, enums, and API DTOs owned by this service
├── <Service>DbContext.cs        # This service's database only
├── <Service>Endpoints.cs        # Minimal API routes
└── Migrations/                  # EF Core migrations, applied at startup
```

### 4.2 Service-specific additions

| Service | Additional files | Purpose |
|---|---|---|
| **Product** | — | Standard files cover CRUD, search, and deactivation. |
| **Inventory** | `StockService.cs` | Atomic stock-in/out/adjust/transfer rules, negative-stock guard, idempotency. |
| | `OutboxPublisher.cs` | Publishes `StockChanged` / `StockBelowThreshold` after the transaction commits. |
| **Supplier** | — | Standard files cover CRUD and product links. |
| **Procurement** | `InventoryClient.cs` | Typed HTTP client for stock updates, with timeout and idempotency key. |
| **Notification** | `InventoryEventConsumer.cs` | Consumes stock events, deduplicates by event ID, creates and resolves alerts. |
| **Reporting** | `ReportingEventConsumer.cs` | Updates reporting read models from stock and receipt events. |
| | `CsvExport.cs` | Streams inventory and movement reports as CSV. |

`Models.cs` holds the extra records a service needs, for example:

- Inventory: `StockItem`, `StockMovement`, `IdempotencyRecord`, `OutboxMessage`.
- Procurement: `PurchaseOrder`, `PurchaseOrderLine`, `GoodsReceipt`, status enum.
- Notification: `Alert`, `AlertStatus`, `ProcessedEvent`.
- Reporting: inventory and movement read models, `ProcessedEvent`.

### 4.3 Gateway

```text
backend/gateway/DepiGb.Gateway/
├── DepiGb.Gateway.csproj        # YARP + JWT bearer + rate limiting
├── Program.cs                   # Routing, auth, correlation ID, rate limits, health
├── appsettings.json             # Route table and downstream service URLs
└── Dockerfile
```

The gateway routes by path (`/api/products`, `/api/inventory`, …), validates the
Keycloak JWT, and forwards the correlation ID. It contains no business logic.

### 4.4 Shared project

```text
backend/shared/DepiGb.Shared/
├── DepiGb.Shared.csproj
├── Events.cs                    # EventEnvelope + all event contracts
├── RabbitMqBus.cs               # Publish/consume helpers with retry and dead-lettering
└── AuthExtensions.cs            # JWT validation setup shared by services
```

Only contracts and infrastructure helpers live here. No entities, repositories, or
business rules.

### 4.5 Tests

```text
backend/tests/DepiGb.Tests/
├── DepiGb.Tests.csproj
├── InventoryTests.cs            # Negative stock, concurrency, idempotency, transfers
├── ProcurementTests.cs          # State transitions, over-receipt, duplicate receipts
└── NotificationTests.cs         # Duplicate events, threshold recovery, alert states
```

One test project covers the rules most likely to break. Per-service test projects
can be added later if needed.

---

## 5. Frontend

```text
frontend/
├── package.json                 # React, Vite, TypeScript, TanStack Query, router
├── vite.config.ts               # Dev proxy to the gateway
├── tsconfig.json
├── index.html
├── Dockerfile                   # Builds static assets
├── nginx.conf                   # Serves the SPA; local Compose proxies /api to the gateway
└── src/
    ├── main.tsx                 # React bootstrap
    ├── App.tsx                  # Providers and router
    ├── api.ts                   # Fetch wrapper and all API calls
    ├── auth.tsx                 # Keycloak OIDC login, roles, protected routes
    ├── pages/
    │   ├── Dashboard.tsx        # Inventory summary and active alerts
    │   ├── Products.tsx         # Search, filter, create, edit, deactivate
    │   ├── Inventory.tsx        # Stock levels, in/out/adjust/transfer, history
    │   ├── Suppliers.tsx        # Supplier CRUD
    │   ├── Procurement.tsx      # Purchase orders, approval, receiving
    │   ├── Alerts.tsx           # Filter and acknowledge alerts
    │   └── Reports.tsx          # Reports and CSV download
    └── components/
        ├── Layout.tsx           # Sidebar, topbar, route outlet
        └── DataTable.tsx        # Shared table, pagination, empty/error states
```

---

## 6. Kubernetes

```text
deploy/k8s/
├── namespace.yaml               # depi-gb namespace
├── config.yaml                  # ConfigMap: service URLs, log level, thresholds
├── secrets.example.yaml         # Secret template: DB, JWT, RabbitMQ, Keycloak
├── postgres.yaml                # PostgreSQL StatefulSet, Service, init SQL
├── rabbitmq.yaml                # RabbitMQ StatefulSet and Service
├── keycloak.yaml                # Keycloak Deployment, Service, realm import
├── services.yaml                # Six service Deployments and Services (multi-doc)
├── gateway.yaml                 # Gateway Deployment and Service
├── frontend.yaml                # Dashboard Deployment and Service
├── ingress.yaml                 # Nginx Ingress: / -> frontend, /api -> gateway
├── hpa.yaml                     # HPA for gateway, frontend, and services
└── monitoring.yaml              # Prometheus, Grafana, node-exporter
```

Notes:

- Every Deployment defines resource requests/limits and readiness/liveness probes.
- Services run migrations at startup. For production, replace this with one
  Kubernetes Job per service.
- `ingress-nginx` is installed by Ansible; `ingress.yaml` only defines routes.
- `monitoring.yaml` mounts ConfigMaps generated from `infra/monitoring/` and runs
  node-exporter for host/container metrics.
- `NetworkPolicy` is optional at this stage; if added, allow only gateway → services,
  services → their own database, and publishers/consumers → RabbitMQ.

---

## 7. Infrastructure

### 7.1 Terraform — AWS

```text
infra/terraform/
├── network.tf                   # VPC, public/private subnets, IGW, NAT, security groups
├── compute.tf                   # EC2: Jenkins host and Kubernetes nodes
├── storage.tf                   # S3: Terraform state and build artifacts
├── variables.tf                 # Region, CIDRs, instance types, bucket names
├── outputs.tf                   # Instance IPs, VPC/subnet IDs, bucket names
├── backend.tf                   # S3 remote state with native locking
└── terraform.tfvars.example     # Sample values; copy to terraform.tfvars locally
```

No modules or per-environment directories yet. One root configuration is enough for
the course scope; environments can be split later.

### 7.2 Ansible

```text
infra/ansible/
├── inventory.ini                # Jenkins host, control-plane, and worker nodes
├── site.yml                     # Installs Docker, k3s, and ingress-nginx
├── deploy.yml                   # Applies deploy/k8s manifests and waits for rollout
└── group_vars/all.yml           # Versions, paths, cluster token reference
```

No roles yet. Tasks live in the two playbooks and can be extracted into roles when
they grow.

### 7.3 Supporting configuration

```text
infra/
├── keycloak/
│   └── realm.json               # Realm, OIDC clients, Manager/Staff/Admin roles
├── postgres/
│   └── init.sql                 # Creates one database and role per service (local stack)
└── monitoring/
    ├── prometheus.yml           # Scrapes gateway, services, node-exporter
    └── grafana-dashboard.json   # Inventory, gateway, and alert panels
```

---

## 8. CI/CD and Scripts

`Jenkinsfile` runs one pipeline:

1. Build and test the .NET solution and the frontend.
2. Build one Docker image per changed service, tagged with the commit SHA.
3. Push images to the registry.
4. Update only the changed Deployment's image with `kubectl set image`.
5. Wait for rollout and roll back with `kubectl rollout undo` on failure.

Terraform and Ansible are run manually (or from a separate Jenkins job) because
infrastructure changes are infrequent and need approval.

```text
scripts/
├── dev-up.ps1                   # Starts docker-compose and seeds sample data
├── dev-down.ps1                 # Stops the local stack, keeps volumes
├── seed.ps1                     # Loads products, suppliers, orders, and stock
└── deploy.ps1                   # Applies Kubernetes manifests and waits for rollout
```

---

## 9. Documentation

```text
documentation/
├── project.md                   # Course brief: deliverables and tools
├── requirements.md              # Authoritative functional requirements
├── simplified-requirements.md   # Condensed requirements reference
├── arch.md                      # Runtime architecture diagram
└── file-structure.md            # This document
```

API references, ADRs, and runbooks can be added once the implementation stabilizes.

---

## 10. Ownership Map

| Data | Owner | Database | Publishes | Consumes |
|---|---|---|---|---|
| Products | Product | `product_db` | — | — |
| Stock levels and movements | Inventory | `inventory_db` | `StockChanged`, `StockBelowThreshold` | — |
| Suppliers and product links | Supplier | `supplier_db` | — | — |
| Purchase orders and receipts | Procurement | `procurement_db` | `GoodsReceived` | — |
| Alerts and delivery attempts | Notification | `notification_db` | — | `StockChanged` |
| Report read models | Reporting | `reporting_db` | — | `StockChanged`, `GoodsReceived` |
| Users and roles | Keycloak | Keycloak DB | — | — |

No cross-database foreign keys. Product and supplier IDs are opaque references.

---

## 11. Deliberate Simplifications

- **One project per service**, with no `Domain/`, `Features/`, or `Infrastructure/`
  folders. Files are added only when a service needs them.
- **One shared project** instead of separate contracts, messaging, and observability
  libraries.
- **One test project** focused on inventory, procurement, and notification rules.
- **One Terraform root configuration**, no modules or environment folders.
- **Two Ansible playbooks**, no roles.
- **One Kubernetes folder**, no Kustomize overlays or Helm charts.
- **One Jenkinsfile**, no `ci/` helper tree.
- **Startup migrations** instead of migration Jobs.
- **No API docs, ADRs, or runbooks yet**; the five project documents are enough
  until the code is running.

What is not simplified away: database-per-service, gateway-only access, idempotent
stock changes, the transactional outbox, event deduplication, health checks, metrics,
and secrets kept out of Git.
