# KeepTabs

**KeepTabs** is an open-source, self-hosted uptime and performance monitoring
platform. Watch websites, TCP endpoints, and hosts — get alerted by email or
webhook when they go down, and inspect response times, uptime, and certificate
expiry from a built-in dashboard.

- **Backend:** .NET 10 Minimal API + Quartz worker, PostgreSQL 17
- **Frontend:** React 19 + React Router v8 SPA (served by the API as one app),
  Tailwind CSS v4 + shadcn
- **Shipping:** Docker Compose, Aspire orchestration

---

## Features

- **Monitors** — HTTP (GET or HEAD), TCP port, and Ping targets with per-monitor
  intervals, timeouts, expected status codes, and pause/resume
- **Checks** — background worker records every result: status, response time,
  and TLS certificate days-remaining for HTTPS targets
- **Alerts** — email, webhook, and Telegram rules on down / recovery /
  consecutive failures, with cooldowns, a delivery log, and a send-test action
- **Dashboard** — status overview, response-time charts, filterable check
  history, API-key management, SMTP/Telegram settings UI, dark mode,
  fully responsive down to phones
- **Auth** — ASP.NET Identity with JWT sessions plus per-user API keys
  (`X-Api-Key` header); RFC 7807 errors and endpoint validation throughout

---

## Prerequisites

| Tool | Version | Notes |
| --- | --- | --- |
| .NET SDK | 10 (`global.json` pins roll-forward) | API, worker, tests |
| Node.js | 24 + pnpm | Frontend (`front-src/`) |
| Docker + Compose | recent | Full stack and the test suite |

---

## Quickstart (Docker Compose)

Create a `.env` file next to `docker-compose.yaml` (copy `.env.example` to get started):

```bash
POSTGRES_PASSWORD=replace-with-a-strong-password
RABBITMQ_DEFAULT_PASS=replace-with-a-strong-password
JWT_KEY=replace-with-at-least-32-characters
JWT_ISSUER=KeepTabs
JWT_AUDIENCE=KeepTabs
JWT_EXPIRY_MINUTES=60
```

Start everything (Postgres, RabbitMQ, API + UI, worker):

```bash
docker compose up -d --build
```

Open the UI at `http://localhost:8080`, register an account, and add your first
monitor. API keys are one-time values: only a hash is stored, so save a
regenerated key immediately.

---

## Local development

### Backend (API + worker)

```bash
# 1. Start Postgres + RabbitMQ (or run the whole stack via Aspire)
docker compose up -d postgres rabbitmq

# 2. Never commit secrets — use user secrets for the JWT signing key:
dotnet user-secrets set "Jwt:Key" "replace-with-at-least-32-characters" --project KeepTabs/KeepTabs.csproj

# 3a. Run everything orchestrated:
dotnet run --project AppHost/AppHost.csproj

# 3b. Or run the API and worker directly (needs ConnectionStrings__keeptabsdb):
ConnectionStrings__keeptabsdb='Host=localhost;Port=5432;Database=keeptabs;Username=keeptabs;Password=<password>' \
  dotnet run --project KeepTabs
```

The API applies EF Core migrations automatically on startup. In `Development`,
interactive API docs are served at `/swagger`.

### Frontend

```bash
# Split dev loop — Vite on :5173, /api proxied to the API on :5104:
pnpm --dir front-src install
pnpm --dir front-src dev
```

For an integrated build, the API project builds the SPA into `KeepTabs/wwwroot`
automatically on Debug `dotnet build`/`dotnet run` (skip with
`/p:BuildFrontend=false`; Release/Docker images build it in the Dockerfile
frontend stage instead). Client-side routes fall back to `index.html`; unknown
`/api/*` routes stay JSON 404s.

The UI opens with a public landing page at `/`; the app lives under
`/dashboard`, with `/alerts`, `/api-keys`, `/settings`, and integration
`/docs` (webhook + API-key guides, links to Swagger UI, served at `/swagger`
in every environment). The interface is responsive down to phones and ships
light/dark themes with OS-preference detection.

### Tests

```bash
dotnet test KeepTabs.slnx
```

Integration tests spin up throwaway Postgres 17 containers (Testcontainers), so
Docker must be running — no manual database setup needed. Everything else runs
offline.

---

## Configuration reference

| Variable | Required | Default | Purpose |
| --- | --- | --- | --- |
| `ConnectionStrings__keeptabsdb` | dev-direct | Aspire/compose provides it | Postgres connection |
| `Jwt__Key` | yes | — | HMAC signing key, **≥ 32 chars** (user secrets / `.env`) |
| `Jwt__Issuer` / `Jwt__Audience` | yes | — | Token issuer/audience |
| `Jwt__ExpiryMinutes` | compose default `60` | Session lifetime (5–1440; required when running the image directly) |
| `Cors__AllowedOrigins` | no | `http://localhost:5173` | Comma-separated browser origins |
| `ASPNETCORE_ENVIRONMENT` | no | `Production` | `Development` enables Swagger UI |

Email (SMTP) delivery is configured at runtime in **Settings** (persisted in the
database, password never returned by the API) rather than via environment
variables. The `SecureApiKeys` migration clears any legacy plaintext API keys.

### Telegram alerts

1. Talk to [@BotFather](https://t.me/BotFather) on Telegram and create a bot to
   get a token (it looks like `123456:ABC-DEF...`).
2. Create your notifications group, add the bot to it.
3. Find the group chat ID (e.g. `-1001234567890`) — a helper like `@getmyid_bot`
   (added to the group) will report it.
4. In KeepTabs **Settings**, enable Telegram and save the bot token.
5. Create an alert rule of type Telegram with the chat ID as its target, then
   use **Send test alert** to verify delivery.

---

## Database migrations

```bash
# Tools + design-time connection (migrations never touch a live DB to generate):
export PATH="$PATH:$HOME/.dotnet/tools"
dotnet tool install -g dotnet-ef # first time only

dotnet add KeepTabs/KeepTabs.csproj package Microsoft.EntityFrameworkCore.Design
ConnectionStrings__keeptabsdb='Host=localhost;Database=dummy;Username=dummy;Password=dummy' \
  dotnet ef migrations add <Name> --project KeepTabs.Infrastructure --startup-project KeepTabs
dotnet remove KeepTabs/KeepTabs.csproj package Microsoft.EntityFrameworkCore.Design
```

The `Design` package must not stay referenced — it is added only to scaffold the
migration and removed right after (see above).

---

## Project structure

```
KeepTabs/                 Minimal API (endpoints, auth, CORS, SPA hosting)
KeepTabs.Worker/          Quartz jobs: scan due monitors, execute checks
KeepTabs.Application/     Use cases, ports, validators (no infra dependencies)
KeepTabs.Infrastructure/  EF Core, Identity, probes, alert channels, security
KeepTabs.Domain/          Entities and invariants
KeepTabs.Tests/           xUnit suite (unit + Testcontainers Postgres)
front-src/                React SPA (built into KeepTabs/wwwroot)
AppHost/                  Aspire orchestration
```

Explore the interactive API reference at `/swagger` (served in every
environment); the build plan lives in [`PLAN.md`](PLAN.md).

---

## Contributing

Issues and pull requests are welcome. Please keep Clean Architecture boundaries
(Application never references Infrastructure), add regression tests for behavior
changes (`dotnet test KeepTabs.slnx`), and update `PLAN.md` when milestones move.

## License

Open-source under the MIT License — see [LICENSE](LICENSE).
