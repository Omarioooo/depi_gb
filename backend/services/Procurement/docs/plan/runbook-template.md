# Runbook Template

Copy this file to the sprint directory as `runbook.md` before starting work.

# Sprint NN — PROC-NNN: <title> — Runbook

## Preconditions

- Previous sprint is `done`.
- Required decisions in `../decisions.md` are `accepted`.
- Local PostgreSQL/RabbitMQ available if the sprint needs it.

## 1. Prepare

```bash
cd backend/services/Procurement
git switch main
git pull
git switch -c feat/proc-NNN-<slug>
./gradlew clean test
```

## 2. Start dependencies

```bash
# PostgreSQL
docker run --rm -d --name procurement-pg \
  -e POSTGRES_DB=procurement_db \
  -e POSTGRES_USER=procurement \
  -e POSTGRES_PASSWORD=procurement \
  -p 5432:5432 postgres:16

# RabbitMQ (only when the sprint needs it)
docker run --rm -d --name procurement-rabbit \
  -p 5672:5672 -p 15672:15672 rabbitmq:3-management
```

## 3. Implement

Work through `tasks.md`. After each group:

```bash
./gradlew test
```

## 4. Verify

| Check | Command | Expected |
|---|---|---|
| Build | `./gradlew clean build` | `BUILD SUCCESSFUL` |
| Tests | `./gradlew test` | all green |
| API | `curl ...` | documented response |
| Database | `psql ...` | expected schema/data |

## 5. Capture evidence

Fill `evidence.md` with real commands and output. Do not paste fabricated or
truncated results.

## 6. Clean up

```bash
docker rm -f procurement-pg procurement-rabbit 2>/dev/null || true
git status
```
