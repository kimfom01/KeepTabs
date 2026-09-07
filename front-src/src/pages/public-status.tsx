import { useCallback, useEffect, useState } from "react";
import { Link, useParams } from "react-router";
import { toast } from "sonner";
import { AvailabilityBars, type AvailabilityBar } from "@/components/uptime-bars";
import { PublicFooter } from "@/components/public-footer";
import { PublicHeader } from "@/components/public-header";
import { StatusLabel, monitorState } from "@/components/status-dot";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { ToggleGroup, ToggleGroupItem } from "@/components/ui/toggle-group";
import { ApiError, statusPagesApi, type PublicStatusMonitor, type PublicStatusPage } from "@/lib/api";
import { formatRelative, formatUptime } from "@/lib/format";

function hourLabel(iso: string): string {
  const date = new Date(iso);
  return `${date.toLocaleDateString([], { month: "numeric", day: "numeric" })} ${date.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })}`;
}

function hourlyItems(monitor: PublicStatusMonitor): AvailabilityBar[] {
  const ordered = (monitor.hourly ?? []).slice(-48);
  const padded: ({ uptime: number | null; label: string } | null)[] = [
    ...Array(Math.max(0, 48 - ordered.length)).fill(null),
    ...ordered.map((h) => ({
      uptime: h.totalChecks > 0 ? h.uptimePercentage : null,
      label: `${hourLabel(h.hour)}: ${h.totalChecks === 0 ? "no data" : `${h.uptimePercentage.toFixed(1)}% up (${h.upCount}/${h.totalChecks})`}`,
    })),
  ];

  return padded.map((item, index) => ({
    key: `h-${index}`,
    tooltip: item?.label ?? "No data",
    uptime: item?.uptime ?? null,
  }));
}

function dailyItems(monitor: PublicStatusMonitor): AvailabilityBar[] {
  const ordered = (monitor.daily ?? []).slice(-30);
  const padded = [
    ...Array(Math.max(0, 30 - ordered.length)).fill(null),
    ...ordered.map((d) => ({
      uptime: d.totalChecks > 0 ? d.uptimePercentage : null,
      label: `${d.date}: ${d.totalChecks === 0 ? "no data" : `${d.uptimePercentage.toFixed(2)}% up (${d.upCount}/${d.totalChecks})`}`,
    })),
  ];

  return padded.map((item, index) => ({
    key: `d-${index}`,
    tooltip: item?.label ?? "No data",
    uptime: item?.uptime ?? null,
  }));
}

export function PublicStatusPageView() {
  const { slug } = useParams();
  const [page, setPage] = useState<PublicStatusPage | null>(null);
  const [notFound, setNotFound] = useState(false);
  const [granularity, setGranularity] = useState<"hour" | "day">("hour");

  const load = useCallback(async () => {
    if (!slug) return;
    try {
      setPage(await statusPagesApi.public(slug));
    } catch (error) {
      if (error instanceof ApiError && error.status === 404) {
        setNotFound(true);
      } else {
        toast.error(error instanceof ApiError ? error.message : "Could not load the status page.");
      }
    }
  }, [slug]);

  useEffect(() => {
    load();
  }, [load]);

  const allUp = (page?.monitors ?? []).every((monitor) => monitor.lastStatusUp === true);
  const anyDown = (page?.monitors ?? []).some((monitor) => monitor.lastStatusUp === false);

  return (
    <div className="min-h-screen bg-background">
      <PublicHeader />
      <main className="mx-auto max-w-6xl px-4 py-8 sm:px-6">
        {notFound ? (
          <div className="py-16 text-center">
            <h1 className="text-2xl font-semibold tracking-tight">Page not found</h1>
            <p className="mt-2 text-sm text-muted-foreground">
              This status page does not exist or is not public.
            </p>
            <Button asChild className="mt-6">
              <Link to="/">Back to home</Link>
            </Button>
          </div>
        ) : page === null ? (
          <div className="flex flex-col gap-4">
            <Skeleton className="h-10 w-64" />
            <Skeleton className="h-24 w-full" />
            <Skeleton className="h-24 w-full" />
          </div>
        ) : (
          <>
            <div className="flex flex-wrap items-center gap-3">
              <span className="relative flex size-3 shrink-0">
                {anyDown && (
                  <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-rose-400 opacity-60" />
                )}
                <span
                  className={`relative inline-flex size-3 rounded-full ${anyDown ? "bg-rose-500" : "bg-emerald-500"}`}
                />
              </span>
              <h1 className="text-2xl font-semibold tracking-tight">{page.name}</h1>
            </div>
            <p className="mt-1 text-sm text-muted-foreground">
              {page.monitors.length === 0
                ? "No monitors on this page yet."
                : allUp
                  ? "All systems operational."
                  : anyDown
                    ? "Some systems are experiencing issues."
                    : "Status updates pending."}
            </p>

            <div className="mt-6 flex items-center justify-between gap-4">
              <h2 className="text-lg font-semibold tracking-tight">Services</h2>
              <ToggleGroup
                type="single"
                value={granularity}
                onValueChange={(value) => {
                  if (value === "hour" || value === "day") setGranularity(value);
                }}
                aria-label="Bar granularity"
              >
                <ToggleGroupItem value="hour">Per hour</ToggleGroupItem>
                <ToggleGroupItem value="day">Per day</ToggleGroupItem>
              </ToggleGroup>
            </div>
            <div className="mt-3 flex flex-col gap-3">
              {page.monitors.map((monitor) => (
                <Card key={monitor.monitorId}>
                  <CardContent className="flex flex-col gap-3 p-4 sm:p-5">
                    <div className="flex flex-wrap items-center gap-3">
                    <StatusLabel
                      state={monitorState(false, monitor.lastStatusUp)}
                    />
                    <div className="min-w-0">
                      <p className="truncate font-medium">{monitor.name}</p>
                      <p className="truncate font-mono text-xs text-muted-foreground">
                        {monitor.url}
                      </p>
                    </div>
                    <div className="ml-auto flex items-center gap-3">
                      {monitor.uptimePercentage !== null && (
                        <span className="font-mono text-sm tabular-nums text-muted-foreground">
                          {formatUptime(monitor.uptimePercentage)} up
                        </span>
                      )}
                      <Badge variant="outline" className="font-mono">
                        {monitor.protocol.toUpperCase()}
                      </Badge>
                      <span className="hidden text-xs text-muted-foreground sm:inline">
                        {formatRelative(monitor.lastCheckedAt)}
                      </span>
                    </div>
                    </div>
                    <AvailabilityBars
                      items={granularity === "hour" ? hourlyItems(monitor) : dailyItems(monitor)}
                    />
                  </CardContent>
                </Card>
              ))}
            </div>

            <Card className="mt-6">
              <CardHeader>
                <CardTitle className="text-sm font-medium text-muted-foreground">
                  About this page
                </CardTitle>
              </CardHeader>
              <CardContent>
                <p className="text-sm text-muted-foreground">
                  Published with KeepTabs, the open-source uptime monitor you run
                  yourself. Statuses refresh as checks complete.
                </p>
              </CardContent>
            </Card>
          </>
        )}
      </main>
      <PublicFooter />
    </div>
  );
}
