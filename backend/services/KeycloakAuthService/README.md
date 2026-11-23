# Keycloak Auth Service

Authentication and user-administration microservice for the Inventory Management
System. Keycloak owns authentication, passwords, tokens and roles. This service
owns a small local `User` table and exposes a consistent REST API in front of
Keycloak.

## What this service does

- Registers users in Keycloak and stores a linked local profile (`KeycloakId`).
- Logs users in, refreshes tokens and logs users out through Keycloak.
- Returns the current user's profile and roles (`/api/auth/me`).
- Changes the current user's password.
- Manages realm roles and user role assignments.
- Provides admin user management.
- Wraps every response in one `ApiResponse<T>` envelope.

It does **not** store passwords, issue JWTs itself, or keep a local roles table.
Keycloak is the source of truth for both.

## Project structure

```text
KeycloakAuthService/
├── KeycloakAuthService.Domain/          User entity, DefaultRoles constants
├── KeycloakAuthService.Application/     DTOs, interfaces, services, ApiResponse
├── KeycloakAuthService.Infrastructure/  EF Core + SQL Server, Keycloak Admin API client
├── KeycloakAuthService.API/             Controllers, JWT setup, middleware, Program.cs
├── KeycloakAuthService.Tests/           xUnit + FakeItEasy unit and API tests
├── Dockerfile
└── KeycloakAuthService.slnx
```

Dependencies point inward only: `API -> Infrastructure -> Application -> Domain`.

## Databases

One SQL Server instance, three logically independent databases:

| Database | Owner | Contents |
|---|---|---|
| `AuthDB` | this service | `Users` table only |
| `KeycloakDB` | Keycloak | Keycloak's internal tables |
| `InventoryDB` | Inventory business services | reserved; this service never connects |

There are no shared tables, joins or cross-database foreign keys. The service
reads its connection string from `ConnectionStrings:AuthDb` only.

## API

All routes are under `/api` and return the shared envelope.

| Method | Route | Access |
|---|---|---|
| POST | `/api/auth/register` | public |
| POST | `/api/auth/login` | public |
| POST | `/api/auth/logout` | public (refresh token is the credential) |
| POST | `/api/auth/refresh-token` | public (refresh token is the credential) |
| GET | `/api/auth/me` | authenticated |
| PUT | `/api/password/change` | authenticated |
| POST | `/api/roles` | `system_administrator` |
| GET | `/api/roles` | `system_administrator` |
| GET | `/api/roles/{id}` | `system_administrator` |
| PUT | `/api/roles/{id}` | `system_administrator` |
| DELETE | `/api/roles/{id}` | `system_administrator` |
| GET | `/api/users/{userId}/roles` | `system_administrator` |
| POST | `/api/users/{userId}/roles/{roleName}` | `system_administrator` |
| DELETE | `/api/users/{userId}/roles/{roleName}` | `system_administrator` |
| GET | `/api/admin/users?page=1&pageSize=10` | `system_administrator` |
| GET | `/api/admin/users/{id}` | `system_administrator` |
| PUT | `/api/admin/users/{id}` | `system_administrator` |
| DELETE | `/api/admin/users/{id}` | `system_administrator` |

Role CRUD uses Keycloak role IDs (GUIDs). Assigning and removing roles uses the
role name, because that is what administrators think in. Role names are the exact
Keycloak IDs from `DefaultRoles`: `manager`, `staff` and `system_administrator`.

### Response envelope

```json
{
  "success": true,
  "message": "Users retrieved successfully",
  "data": [],
  "errors": [],
  "statusCode": 200,
  "meta": { "page": 1, "pageSize": 10, "totalCount": 100, "totalPages": 10 }
}
```

- The HTTP status always matches `statusCode`.
- `meta` is `null` for non-paginated responses.
- `204` responses have no body.
- Validation errors return `422` with a populated `errors` list.
- Unknown routes, `401`, `403` and unhandled exceptions use the same envelope.
- Stack traces and upstream error bodies are logged, never returned.

## Configuration

Everything is read from configuration/environment variables. No secrets are
committed.

| Setting | Environment variable | Notes |
|---|---|---|
| `ConnectionStrings:AuthDb` | `ConnectionStrings__AuthDb` | required |
| `Keycloak:BaseUrl` | `Keycloak__BaseUrl` | required; public URL locally, `http://keycloak:8080` in Docker |
| `Keycloak:Realm` | `Keycloak__Realm` | required (in `appsettings.json`) |
| `Keycloak:ClientId` | `Keycloak__ClientId` | required (in `appsettings.json`) |
| `Keycloak:ClientSecret` | `Keycloak__ClientSecret` | required; never committed |
| `Keycloak:AdminClientId` | `Keycloak__AdminClientId` | required (in `appsettings.json`) |
| `Keycloak:AdminClientSecret` | `Keycloak__AdminClientSecret` | required; never committed |
| `Keycloak:Audience` | `Keycloak__Audience` | required; must match the `aud` claim of real access tokens |
| `Keycloak:Issuer` | `Keycloak__Issuer` | optional issuer override |
| `Keycloak:AdminTokenRefreshMarginSeconds` | `Keycloak__AdminTokenRefreshMarginSeconds` | default `30` |

Non-secret values live in `appsettings.json`; secrets come from environment
variables or user secrets. The options are validated on startup: the service
refuses to start when a required value is missing and names the exact key.

`Keycloak:Issuer` matters when the URL that mints tokens differs from the URL
this service calls. Locally both are `http://localhost:6666`, so it stays empty.
In Docker the API calls `http://keycloak:8080` while tokens carry the public
issuer, so Compose sets:

```bash
Keycloak__Issuer=http://localhost:6666/realms/auth-realm
```

Keycloak resolves backchannel URLs from the incoming request
(`KC_HOSTNAME_BACKCHANNEL_DYNAMIC=true`), so the OpenID metadata fetched from
`Keycloak:BaseUrl` still returns URLs that the caller can reach.

### Audience

Audience validation is always enabled. `Keycloak:Audience` (or
`Keycloak__Audience` / `KEYCLOAK_AUDIENCE`) must contain the audience that the
real access tokens carry in their `aud` claim. `appsettings.json` ships with the
placeholder `REPLACE_WITH_ACCESS_TOKEN_AUDIENCE`, which will never match a real
token, so you must replace it before logging in through this API.

To find the correct value, log in once and decode the access token
(for example at <https://jwt.io>), then read `aud`. Use exactly that value.
Do not add a Keycloak audience mapper for this service; the configured value
must simply match what Keycloak already issues.

## Keycloak setup (manual, one time)

Start the stack first (see below), then open <http://localhost:6666> and log in
with the bootstrap admin from `.env`.

1. **Create realm** `auth-realm` (or set `KEYCLOAK_REALM` to your realm name).
2. **Create realm roles** with these exact names: `manager`, `staff`,
   `system_administrator`. These names are case-sensitive and must match the
   constants in `DefaultRoles`; renaming them breaks registration and
   authorization.
3. **Create client** `auth-client`:
   - Client authentication: **On** (confidential)
   - Direct access grants: **On** (needed for login and password verification)
   - Standard flow: not needed by this API
   - Copy the client secret into `KEYCLOAK_CLIENT_SECRET`.
4. **Create client** `auth-service`:
   - Client authentication: **On**
   - Service accounts roles: **On**
   - Standard flow and direct access grants: **Off**
   - Service account roles -> assign `realm-management` client roles:
     `view-users`, `manage-users`, `manage-realm`
   - Copy the client secret into `KEYCLOAK_ADMIN_CLIENT_SECRET`.
5. **Create the first administrator**: add a user manually in Keycloak, set a
   password and assign `system_administrator`. Public registration always gets
   the `staff` role only.
6. Check the access token audience: decode a token issued for `auth-client` and
   copy its `aud` value into `Keycloak:Audience` in `appsettings.json` (or
   `KEYCLOAK_AUDIENCE` in `.env`). Do not add an audience mapper.
7. Set the secrets in `.env` and restart: `docker compose up -d auth-service`.

Roles are intentionally **not** recreated on startup. If you delete a role in
Keycloak, it stays deleted after a restart.

## Run with Docker Compose

From the repository root:

```bash
cp .env.example .env        # then edit every value
docker compose up -d --build
```

| Service | URL |
|---|---|
| Auth API | <http://localhost:5080> |
| Keycloak | <http://localhost:6666> |
| SQL Server | `localhost:1433` |

`.env.example` is the single source of truth for the Compose stack: host ports
(`SQLSERVER_HOST_PORT`, `KEYCLOAK_HOST_PORT`, `KEYCLOAK_MANAGEMENT_HOST_PORT`,
`AUTH_SERVICE_HOST_PORT`), database names (`AUTH_DB_NAME`, `KEYCLOAK_DB_NAME`,
`INVENTORY_DB_NAME`), Keycloak URLs, realm/client IDs, audience, and every
password or secret. Compose fails with the variable name when one is missing.

A one-shot `sqlserver-init` service runs `docker/sql/init-databases.sql` after
SQL Server is healthy. It creates the three databases (including the KeycloakDB
collation and snapshot setting), then exits; it is not part of the running stack.

The API does not create its tables on startup. Run the migration command in the
[Migrations](#migrations) section once after SQL Server is up, otherwise the API
starts but every user endpoint fails. Keycloak takes longer to boot than the API;
requests that need Keycloak may return `502` until it is ready.

## Migrations

EF Core migrations are applied **manually**; the application never touches the
schema at startup. The EF tools load the normal application configuration, so
`ConnectionStrings:AuthDb` must be available through an environment variable or
user secrets. No placeholder connection string exists in the code.

```bash
cd backend/services/KeycloakAuthService

export ConnectionStrings__AuthDb="Server=localhost,1433;Database=AuthDB;User Id=sa;Password=<SQLSERVER_SA_PASSWORD>;TrustServerCertificate=True;Encrypt=False"

dotnet ef database update \
  --project KeycloakAuthService.Infrastructure \
  --startup-project KeycloakAuthService.API
```

To create a new migration:

```bash
dotnet ef migrations add <MigrationName> \
  --project KeycloakAuthService.Infrastructure \
  --startup-project KeycloakAuthService.API \
  --output-dir Persistence/Migrations
```

## Run locally without Docker

1. Start SQL Server and Keycloak (`docker compose up -d sqlserver sqlserver-init keycloak`).
2. Store the connection string and secrets in user secrets:

```bash
cd backend/services/KeycloakAuthService/KeycloakAuthService.API
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:AuthDb" "Server=localhost,1433;Database=AuthDB;User Id=sa;Password=...;TrustServerCertificate=True;Encrypt=False"
dotnet user-secrets set "Keycloak:ClientSecret" "..."
dotnet user-secrets set "Keycloak:AdminClientSecret" "..."
```

`Keycloak:BaseUrl` comes from `appsettings.json` (`http://localhost:6666`), so
the Keycloak container must be published on port `6666` (Compose does this).
`Keycloak:Issuer` is not needed locally: it falls back to the same public URL.

3. Apply the migrations (see [Migrations](#migrations)).
4. Run it:

```bash
dotnet run
```

The API listens on <http://localhost:5080>.

## Build and test

```bash
cd backend/services/KeycloakAuthService

dotnet build KeycloakAuthService.slnx
dotnet test KeycloakAuthService.slnx
```

Tests use xUnit and FakeItEasy. Unit tests fake `IKeycloakService` and
`IUserRepository`. Integration tests use `WebApplicationFactory` with an
in-memory database, a fake Keycloak client and a test authentication scheme.
No real Keycloak or Docker is required.

The test authentication scheme reads two headers, which is only available inside
the test host:

```text
X-Test-User: <keycloak user id>
X-Test-Roles: staff,system_administrator
```

## Design notes and limitations

- **Direct Access Grant.** Login and current-password verification use the
  Resource Owner Password Credentials flow. It fails when the realm requires
  MFA, conditional OTP, consent or a custom authentication flow. That is
  acceptable for a trusted backend-for-frontend, but it means users with MFA
  cannot log in through this API. The long-term alternative is a browser-based
  Authorization Code flow.
- **Admin token caching.** The service-account token is cached in memory and
  renewed shortly before expiry. A lock prevents duplicate token requests. No
  Redis is involved; running multiple replicas means one token per replica.
- **Compensation, not distributed transactions.** If role assignment or saving
  the local user fails after the Keycloak user was created, the service tries to
  delete that Keycloak user. If the cleanup itself fails, the failure is logged
  and the Keycloak user must be removed manually.
- **No local authorization data.** Roles are read from Keycloak on demand, so
  every role change is immediately reflected. This costs one Keycloak call per
  `/api/auth/me` request.
- **Manual migrations.** The API never applies migrations at startup. Every
  deployment runs `dotnet ef database update` as its own step before starting the
  service.
- **SQL Server credentials.** The local Compose file connects as `sa` for
  simplicity. For anything beyond local development, create dedicated logins per
  database and store them in Kubernetes Secrets.
- **Keycloak hostname.** Compose sets `KC_HOSTNAME` to the public URL
  (`http://localhost:6666`) and enables
  `KC_HOSTNAME_BACKCHANNEL_DYNAMIC=true`. Tokens therefore carry the public
  issuer, while containers still reach Keycloak at `http://keycloak:8080`.
  The Admin Console is at the same public URL.
- **First and last name** are stored only in `AuthDB`. Keycloak user profile
  settings are managed separately and are not modified by this service.
