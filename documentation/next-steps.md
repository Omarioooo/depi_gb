# What Must Be Done

## Project goal

Build a secure, deployable warehouse inventory system. Users manage products,
stock, suppliers, purchase orders, alerts, and reports through a web dashboard.
The project also demonstrates containerization, CI/CD, Kubernetes, monitoring,
and automated AWS infrastructure.

The key business rule is that every stock change must be accurate and auditable:
stock cannot go negative, movements cannot be lost or duplicated, and failures
must not leave inventory in a partial state.

## Current state

The documentation defines the requirements, architecture, technology stack, and
target file structure. The implementation is only at its starting point: the
backend currently contains a Product service scaffold with the default weather
forecast endpoint. The other services, dashboard, deployment configuration, and
infrastructure deliverables still need to be built.

## Work to do

1. **Set up the foundation:** create the solution, shared event/authentication
   contracts, local development stack, configuration, and service health checks.
2. **Build the backend services:** implement Product, Inventory, Supplier,
   Procurement, Notification, and Reporting, each owning its data and exposing
   documented APIs. Start with Inventory's atomic updates, movement history,
   idempotency, and event outbox.
3. **Add identity and access control:** configure ASP.NET Core Identity and enforce Manager,
   Staff, and System Administrator permissions in protected backend operations.
4. **Connect services safely:** add the API gateway and RabbitMQ workflows,
   including event deduplication, retries, and recovery for failed receipts.
5. **Build the dashboard:** implement sign-in and the product, inventory,
   supplier, procurement, alerts, and reporting screens, including CSV exports.
6. **Prove the core rules:** cover authorization, stock boundaries, concurrent
   updates, duplicate requests/events, purchase-order transitions, and failure
   recovery.
7. **Deliver the DevOps stack:** containerize components; add Jenkins CI/CD,
   Kubernetes manifests, Nginx ingress, Prometheus/Grafana monitoring, Terraform
   for AWS resources, and Ansible configuration.
8. **Make it runnable and document it:** provide setup and deployment instructions,
   sample configuration without real secrets, and operational guidance.

## Completion check

The project is complete when the documented workflows run through the dashboard
and gateway, permissions are enforced by services, inventory and event retries
remain safe, reports reflect their stated freshness, and the stack can be built,
deployed, and monitored using the documented automation.

For detailed behavior and architecture, see [requirements.md](requirements.md),
[simplified-requirements.md](simplified-requirements.md), and [arch.md](arch.md).
