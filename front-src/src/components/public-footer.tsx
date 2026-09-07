import { Link } from "react-router";
import { ActivityIcon } from "lucide-react";

export function PublicFooter() {
  return (
    <footer className="border-t">
      <div className="mx-auto grid max-w-6xl gap-8 px-4 py-10 sm:grid-cols-[1.5fr_1fr_1fr] sm:px-6">
        <div className="flex flex-col items-start gap-3">
          <span className="flex items-center gap-2">
            <span className="flex size-7 items-center justify-center rounded-md bg-primary text-primary-foreground">
              <ActivityIcon data-icon="inline-start" className="size-4" />
            </span>
            <span className="text-base font-semibold tracking-tight">KeepTabs</span>
          </span>
          <p className="max-w-xs text-sm text-muted-foreground">
            Open-source uptime monitoring you run yourself. Your data never
            leaves your server.
          </p>
        </div>
        <nav className="flex flex-col items-start gap-2" aria-label="Product">
          <span className="text-sm font-semibold">Product</span>
          <Link to="/dashboard" className="text-sm text-muted-foreground hover:text-foreground">
            Dashboard
          </Link>
          <Link to="/docs" className="text-sm text-muted-foreground hover:text-foreground">
            Docs
          </Link>
          <Link to="/register" className="text-sm text-muted-foreground hover:text-foreground">
            Get started
          </Link>
        </nav>
        <nav className="flex flex-col items-start gap-2" aria-label="Developers">
          <span className="text-sm font-semibold">Developers</span>
          <a
            href="swagger"
            target="_blank"
            rel="noreferrer"
            className="text-sm text-muted-foreground hover:text-foreground"
          >
            Swagger UI
          </a>
          <a
            href="openapi/v1.json"
            target="_blank"
            rel="noreferrer"
            className="text-sm text-muted-foreground hover:text-foreground"
          >
            OpenAPI JSON
          </a>
        </nav>
      </div>
      <div className="border-t">
        <div className="mx-auto flex max-w-6xl flex-col gap-1 px-4 py-4 text-xs text-muted-foreground sm:flex-row sm:items-center sm:justify-between sm:px-6">
          <span>KeepTabs — open-source uptime monitoring.</span>
          <span className="font-mono">docker compose up -d</span>
        </div>
      </div>
    </footer>
  );
}
