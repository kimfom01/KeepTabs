# 🟢 Keep Tabs

**Keep Tabs** is an open-source uptime and performance monitoring platform built with **.NET 10**, **React 19 + React Router v8**, and **PostgreSQL**.  
It provides simple, self-hosted website/API monitoring with a background worker that runs periodic checks and records availability, response times, and alerts.

This project aims to offer a free alternative to commercial uptime services, with a modern tech stack and a fully self-hosted deployment model.

---

## ✨ Features (MVP)
- Monitor any URL or endpoint
- Periodic HTTP checks powered by a .NET Worker
- Store check results in PostgreSQL
- Dashboard UI built with React, Tailwind CSS, and shadcn
- Basic uptime summaries & history
- Self-hostable via Docker Compose

---

## 🚀 Tech Stack
- **.NET 10 Minimal API**
- **.NET 10 Worker Service + Quartz**
- **PostgreSQL**
- **React 19 + React Router v8 SPA (library mode) + Tailwind CSS/shadcn**
- **Docker Compose**

---

## 📦 Deployment
You can run the entire stack locally or on any server using:

```bash
docker compose up -d
```

Create a `.env` file next to `docker-compose.yaml` before starting the stack:

```bash
POSTGRES_PASSWORD=replace-with-a-strong-password
RABBITMQ_DEFAULT_PASS=replace-with-a-strong-password
JWT_KEY=replace-with-at-least-32-characters
JWT_ISSUER=KeepTabs
JWT_AUDIENCE=KeepTabs
JWT_EXPIRY_MINUTES=60
```

For local API development, do not commit secrets. Use user secrets instead:

```bash
dotnet user-secrets set "Jwt:Key" "replace-with-at-least-32-characters" --project KeepTabs/KeepTabs.csproj
```

API keys are one-time values: only a hash is stored, so save the regenerated key immediately. Existing plaintext API-key values are cleared by the `SecureApiKeys` migration.

## 🖥️ Frontend

The UI is a plain Vite + React Router SPA in `front-src/`. Production builds emit
static assets into `KeepTabs/wwwroot`, which the API serves directly (client-side
routes fall back to `index.html`), so one `dotnet run` hosts UI and API together.

```bash
# Split dev loop (Vite proxies /api to the API on :5104)
pnpm --dir front-src dev

# Integrated build served by the API
pnpm --dir front-src build
dotnet run --project KeepTabs
```

## 📄 License

This project is open-source under the MIT License.