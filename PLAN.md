# KeepTabs — Plan

Living build plan for the KeepTabs uptime-monitoring platform. Items marked `[x]`
are implemented in the current tree; `[ ]` items are outstanding. This file
replaces the old milestone/issue sketch (`PROJECTS.md`), corrected against what
actually exists.

**Stack (as built):** .NET 10 Minimal API + Worker (Quartz), PostgreSQL 17,
React 19 + React Router v8 SPA (library mode) served from the API's `wwwroot`,
Tailwind CSS v4 + shadcn, Aspire orchestration, Docker Compose.

**Status legend:** ✅ done · 🚧 partially done · ⬜ not started

---

## v0.1.0 — Core MVP ✅ (complete)

Goal: basic uptime monitoring via HTTP + core UI + worker.

### Authentication & API ✅
- [x] ASP.NET Identity (`ApplicationUser` + hashed `ApiKeyHash`, roles)
- [x] POST `/api/auth/register` — register, returns JWT
- [x] POST `/api/auth/login` — login, returns JWT
- [x] POST `/api/auth/api-key/regenerate` — one-time raw key, hash only stored
- [x] JWT bearer + `X-Api-Key` authentication, `UserAccess` policy
- [x] Centralized RFC 7807 errors + `ValidationFilter<T>` endpoint validation

### Monitor management (backend) ✅
- [x] `Monitor` entity + EF Core model, `SecureApiKeys` + `MonitorFieldLengths` migrations
- [x] CRUD endpoints (`GET/POST/PUT/DELETE /api/monitors`)
- [x] Summary endpoint (`GET /api/monitors/{id}/summary`)
- [x] History endpoint (`GET /api/monitors/{id}/history?days=`)
- [x] Pause/resume endpoints
- [x] Owner-scoped service layer, per-protocol FluentValidation

### Worker engine ✅
- [x] Worker Service (.NET 10) + Quartz scheduler
- [x] `MonitorScanJob` (finds due monitors) + `MonitorCheckJob` (single check)
- [x] `MonitorCheckRunner` persists `MonitorCheck` rows explicitly and tolerates
      concurrent monitor changes without failing the job
- [x] Per-monitor timeouts, bounded scans, history caps
- [x] Worker composition excludes Identity/user services (API-only)

### Frontend ✅ (with corrections)
- [x] ~~React Router v7 server mode~~ → plain Vite SPA, React 19 + React Router v8
      (library mode), built into the API's `wwwroot`, one `dotnet run` serves all
- [x] Login/register + token session, auth-guarded routes
- [x] Dashboard (monitor list, status, protocol badges)
- [x] Create/edit monitor dialog, monitor detail view, API-keys page
- [x] History chart (response-time area chart on the detail view, plus history table)
- [x] Public landing page (`/`), app under `/dashboard`
- [x] Light/dark themes with OS-preference detection
- [x] Responsive layouts down to phones
- [x] Integration docs page (webhooks, API keys, Swagger links)

### DevOps ✅
- [x] `docker-compose.yaml` (postgres, rabbitmq, api, worker) + `.env` secrets
- [x] Healthchecks, healthy-dependency ordering, restart policies, `.env.example`
- [x] API `Dockerfile` (incl. node stage building the SPA) + `Dockerfile.Worker`
- [x] Environment documentation in README (user secrets for local dev)

### Tests ✅
- [x] 120+ test xUnit suite: domain, validators, auth services, CORS, DI composition
- [x] SQLite-free integration tests on Testcontainers Postgres 17 (prod 1:1)
- [x] Regression tests for the worker check-save bug

### Notes / deviations from the old sketch
- Hangfire was removed entirely (server, dashboard, packages): scheduling lives
  in the Quartz worker, and nothing ever enqueued Hangfire jobs.
- RabbitMQ is provisioned (compose + Aspire references) but **no messaging code
  exists yet**. It is the intended transport for worker fan-out / alerts.
- `AlertRule` / `AlertLog` entities exist in the data model only — no endpoints,
  detection, or delivery (see v0.2.0).

---

## v0.2.0 — Alert system ✅ (complete)

Goal: notify users of state changes.

### Alert rules ✅
- [x] `AlertRule` entity (+ `AlertLog`)
- [x] CRUD endpoints (`/api/alerts`)
- [x] UI: create/edit alert-rule dialog (per monitor)
- [x] UI: rules managed on the monitor detail page

### Worker alert engine ✅
- [x] Detect UP → DOWN / DOWN → UP transitions
- [x] Detect consecutive failures (threshold-aware, current check included)
- [x] Apply cooldown periods (`CoolDownMinutes` + `LastFiredAt`)
- [x] Store `AlertLog` entries (success flag + error)
- [x] Evaluation runs in the check's unit of work; concurrent changes drop the
      result with a warning instead of failing the job

### Delivery channels ✅
- [x] SMTP email alerts (DB-backed settings UI, misconfiguration per delivery)
- [x] Webhook POST alerts (JSON payload, 15s timeout)
- [x] Telegram alerts (bot token in settings, group chat ID per rule)
- [x] "Test alert" endpoint (`POST /api/alerts/{id}/test`, always logged)

### UI ✅
- [x] Alert logs list (`/alerts`)
- [x] Alert state in monitor detail view (rules table, last delivery)

---

## v0.3.0 — Multi-protocol support ✅ (complete)

Goal: more monitor types.

### Protocols
- [x] Ping (ICMP) probe
- [x] TCP port probe
- [x] HTTP HEAD option (`UseHeadRequest`, HTTP-only, validated)
- [x] SSL certificate expiry capture (`SslDaysRemaining` on every HTTPS check)

### Worker updates
- [x] Protocol-aware executor (`IMonitorProbe` per protocol)
- [x] Timeout per monitor (shared HttpClient enforces per-check deadlines)

### UI
- [x] Protocol selection dropdown (+ HEAD toggle for HTTP)
- [x] Display protocol badge in monitor list
- [x] Certificate days-remaining column in check history

---

## v0.4.0 — Dashboard & analytics upgrade ✅ (complete)

Goal: improve UX & data visibility.

### Backend ✅
- [x] Pre-aggregate daily uptime summaries (`DailyUptimeSummaries` + worker `DailySummaryJob`, idempotent)
- [x] Pre-aggregate hourly buckets (`HourlyUptimeSummaries`, trailing 3 days)
- [x] Faster history querying (composite index, 1000-item cap, client pagination)
- [x] Summary caching (60s TTL; TTL-based since worker/API are separate processes)

### UI ✅
- [x] Response-time graph + 30-day daily uptime bars
- [x] Monitor search + filter by UP/DOWN/paused
- [ ] Group monitors by project (no project model exists; deferred — overlaps v0.6 teams)

---

## v0.5.0 — Public status pages ✅ (complete)

Goal: public uptime pages.

- [x] `StatusPage` entity + ordered monitor links, slug system (`/status/{slug}`, normalized, globally unique)
- [x] Slugs auto-generated from the name with `-2`, `-3` suffixing; editable with live availability check
- [x] No public API tokens needed: published pages are anonymously readable, nothing else is exposed
- [x] Public summary data per monitor (state, uptime %, no owner data)
- [x] Status page builder (name, slug, visibility, monitor picker) + public template
- [x] Per-hour / per-day availability bars with green/amber/red states
- [ ] Custom colors/themes

---

## v0.6.0 — Teams & collaboration ⬜

Goal: multi-user organizations.

- [ ] Organization entity, memberships, roles (Owner/Admin/Viewer)
- [ ] Invite system, organization-scoped monitors
- [ ] Team switcher, member management page

---

## v0.7.0 — Scaling & performance ⬜

Goal: handle large load.

- [ ] Partition `MonitorChecks`, purge old data, missing indexes
- [ ] Multi-worker horizontal scaling (RabbitMQ fan-out), distributed scheduling
- [ ] Query optimization, bulk inserts

---

## v0.8.0 — Extended alert channels ⬜

Goal: more notification options.

- [ ] Slack, Discord webhooks; SMS (Twilio / Africa's Talking)

---

## v0.9.0 — Multi-region monitoring ⬜

Goal: check from multiple global regions.

- [ ] Region fields on monitors/checks, region-based summaries
- [ ] Worker region ID, region-scoped execution
- [ ] Region selector, map view

---

## v0.10.0 — API maturity 🚧 (partial)

Goal: stabilize the API for developers.

- [ ] Versioned API (`/api/v1`)
- [ ] Rate limiting, API usage metrics
- [x] Full OpenAPI spec + Swagger UI (dev)
- [x] Validation improvements + RFC 7807 error structure

---

## v1.0.0 — Stable production release ⬜

Goal: final polish & stability.

- [ ] Full documentation, installation + upgrade guides
- [ ] Security review, code cleanup, final UI polish
- [ ] Published stable Docker images, LTS notes

---

## Optional future milestones ⬜

- **v1.1.x — Browser checks:** Playwright checks, content validation, screenshots
- **v1.2.x — Incident management:** timeline, manual entries, postmortems
- **v1.3.x — Synthetic workflows:** multi-step API checks, JSON assertions
- **v1.4.x — Cloud-native:** Kubernetes manifests, Redis caching/pub-sub,
  centralized logging, OpenTelemetry
