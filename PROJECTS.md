
# 🚀 KEEP TABS — MILESTONES & ISSUES BREAKDOWN

This document contains the official GitHub milestone and issue structure for the Keep Tabs project.

---

# 🟩 Milestone: v0.1.0 — Core MVP
**Goal:** Basic uptime monitoring via HTTP + core UI + worker.  
**Status:** Planned

### Authentication & API
- [ ] Implement ASP.NET Identity (User + ApiKey fields)
- [ ] POST /auth/register — Register
- [ ] POST /auth/login — Login & issue JWT
- [ ] POST /auth/api-key/regenerate — New API key
- [ ] JWT + API key authentication middleware

### Monitor Management (Backend)
- [ ] Monitor entity + EF Core model
- [ ] CRUD endpoints (GET/POST/PUT/DELETE /monitors)
- [ ] Summary endpoint (/monitors/{id}/summary)
- [ ] History endpoint (/monitors/{id}/history)
- [ ] Pause/resume monitor

### Worker Engine
- [ ] Implement Worker Service (.NET 10)
- [ ] Add Quartz scheduler
- [ ] MonitorScanJob (determines which monitors run)
- [ ] MonitorCheckJob (runs HTTP checks)
- [ ] Persist MonitorCheck entries
- [ ] Update monitor state on each check

### Frontend (React Router v7)
- [ ] Setup RRv7 server mode
- [ ] Login + JWT session storage
- [ ] Dashboard (list monitors)
- [ ] Create/Edit monitor UI
- [ ] Monitor details view
- [ ] Basic history chart

### DevOps
- [ ] docker-compose.yml
- [ ] Dockerfile api
- [ ] Dockerfile worker
- [ ] Environment variable documentation
- [ ] Initial README

---

# 🟩 Milestone: v0.2.0 — Alert System
**Goal:** Notify users of state changes.

### Alert Rules
- [ ] AlertRule entity
- [ ] CRUD endpoints (/alerts)
- [ ] UI: Create alert rule
- [ ] UI: Alert rule management page

### Worker Alert Engine
- [ ] Detect UP → DOWN transitions
- [ ] Detect DOWN → UP transitions
- [ ] Detect consecutive failures
- [ ] Apply cooldown periods
- [ ] Store AlertLogs entries

### Delivery Channels
- [ ] SMTP email alerts
- [ ] Webhook POST alerts
- [ ] "Test Alert" endpoint

### UI
- [ ] Alert logs list
- [ ] Show alert state in monitor detail view

---

# 🟩 Milestone: v0.3.0 — Multi-Protocol Support
**Goal:** More monitor types.

### Protocols
- [ ] Ping (ICMP)
- [ ] TCP Port checks
- [ ] HTTP HEAD checks
- [ ] SSL certificate expiry checks

### Worker Updates
- [ ] Extend executor to support protocols
- [ ] Timeout per protocol type

### UI
- [ ] Show protocol selection dropdown
- [ ] Display protocol badge in monitor list

---

# 🟩 Milestone: v0.4.0 — Dashboard & Analytics Upgrade
**Goal:** Improve UX & data visibility.

### Backend
- [ ] Pre-aggregate daily uptime summaries
- [ ] Faster history querying
- [ ] Summary caching

### UI Enhancements
- [ ] Response time graph
- [ ] Uptime chart (24h/7d/30d)
- [ ] Monitor search
- [ ] Filter by UP/DOWN
- [ ] Group monitors by Project

---

# 🟩 Milestone: v0.5.0 — Public Status Pages
**Goal:** Public uptime pages.

### Backend
- [ ] StatusPage entity
- [ ] Slug system (/status/{slug})
- [ ] Public API tokens
- [ ] Public summary endpoints

### UI
- [ ] Status page builder
- [ ] Public uptime page template
- [ ] Custom colors/themes

---

# 🟩 Milestone: v0.6.0 — Teams & Collaboration
**Goal:** Multi-user organizations.

### Backend
- [ ] Organization entity
- [ ] Membership table
- [ ] Role model (Owner, Admin, Viewer)
- [ ] Invite system
- [ ] Organization-scoped monitors

### UI
- [ ] Team switcher
- [ ] Member management page

---

# 🟩 Milestone: v0.7.0 — Scaling & Performance
**Goal:** Handle large load.

### Database
- [ ] Partitioning MonitorChecks
- [ ] TimescaleDB optional support
- [ ] Purge old logs
- [ ] Add missing indexes

### Worker
- [ ] Multi-worker (horizontal scaling)
- [ ] Distributed scheduling

### Backend
- [ ] Query optimization
- [ ] Bulk insert support

---

# 🟩 Milestone: v0.8.0 — Extended Alert Channels
**Goal:** More notification options.

- [ ] Telegram Bot alerts
- [ ] Slack webhooks
- [ ] Discord webhooks
- [ ] SMS alerts (Twilio / Africa’s Talking)

---

# 🟩 Milestone: v0.9.0 — Multi-Region Monitoring
**Goal:** Check from multiple global regions.

### Backend
- [ ] Region field on monitors
- [ ] Region field on checks
- [ ] Region-based summaries

### Worker
- [ ] Worker Region ID
- [ ] Region-scoped execution

### UI
- [ ] Region selector
- [ ] Map view

---

# 🟩 Milestone: v0.10.0 — API Maturity
**Goal:** Stabilize API for developers.

- [ ] Versioned API (v1)
- [ ] Rate limiting
- [ ] API usage metrics
- [ ] Full OpenAPI spec
- [ ] Swagger UI
- [ ] Validation improvements
- [ ] Better error structure

---

# 🟩 Milestone: v1.0.0 — Stable Production Release
**Goal:** Final polish & stability.

- [ ] Full documentation
- [ ] Installation guides
- [ ] Upgrade guide
- [ ] Security review
- [ ] Code cleanup & refactor
- [ ] Final UI polish
- [ ] Publish stable Docker images
- [ ] Long-term support notes

---

# OPTIONAL FUTURE MILESTONES

---

# 🟣 Milestone: v1.1.x — Browser Checks
- [ ] Playwright checks
- [ ] Content validation
- [ ] Screenshots on failure

---

# 🟣 Milestone: v1.2.x — Incident Management
- [ ] Incident timeline
- [ ] Manual incident entries
- [ ] Root cause notes
- [ ] Postmortem report

---

# 🟣 Milestone: v1.3.x — Synthetic Workflows
- [ ] Multi-step API checks
- [ ] JSON assertion rules
- [ ] Workflow builder

---

# 🟣 Milestone: v1.4.x — Cloud-Native
- [ ] Kubernetes manifests
- [ ] Redis caching
- [ ] Redis pub/sub worker sync
- [ ] Loki centralized logging
- [ ] OpenTelemetry integration
