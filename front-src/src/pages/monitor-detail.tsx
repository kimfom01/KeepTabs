import { useCallback, useEffect, useState } from "react";
import { Link, useNavigate, useParams } from "react-router";
import { ArrowLeftIcon, PauseIcon, PencilIcon, PlayIcon, Trash2Icon } from "lucide-react";
import { toast } from "sonner";
import { AppHeader } from "@/components/app-header";
import { MonitorFormDialog } from "@/components/monitor-form-dialog";
import { ResponseChart } from "@/components/response-chart";
import { StatusLabel, monitorState } from "@/components/status-dot";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { ApiError, monitorsApi, type Monitor, type MonitorCheck, type MonitorSummary } from "@/lib/api";
import { formatRelative, formatTimestamp, formatUptime } from "@/lib/format";

export function MonitorDetailPage() {
  const { monitorId } = useParams();
  const navigate = useNavigate();
  const [monitor, setMonitor] = useState<Monitor | null>(null);
  const [summary, setSummary] = useState<MonitorSummary | null>(null);
  const [history, setHistory] = useState<MonitorCheck[] | null>(null);
  const [notFound, setNotFound] = useState(false);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [confirmDelete, setConfirmDelete] = useState(false);

  const load = useCallback(async () => {
    if (!monitorId) return;
    try {
      const [loaded, loadedSummary, loadedHistory] = await Promise.all([
        monitorsApi.get(monitorId),
        monitorsApi.summary(monitorId),
        monitorsApi.history(monitorId, 7),
      ]);
      setMonitor(loaded);
      setSummary(loadedSummary);
      setHistory(loadedHistory);
    } catch (error) {
      if (error instanceof ApiError && error.status === 404) {
        setNotFound(true);
      } else {
        toast.error(error instanceof ApiError ? error.message : "Could not load the monitor.");
      }
    }
  }, [monitorId]);

  useEffect(() => {
    load();
  }, [load]);

  async function togglePaused() {
    if (!monitor) return;
    try {
      const updated = monitor.isPaused
        ? await monitorsApi.resume(monitor.monitorId)
        : await monitorsApi.pause(monitor.monitorId);
      setMonitor(updated);
      toast.success(updated.isPaused ? "Monitor paused." : "Monitor resumed.");
    } catch (error) {
      toast.error(error instanceof ApiError ? error.message : "Could not update the monitor.");
    }
  }

  async function handleDelete() {
    if (!monitor) return;
    try {
      await monitorsApi.remove(monitor.monitorId);
      toast.success("Monitor deleted.");
      navigate("/", { replace: true });
    } catch (error) {
      toast.error(error instanceof ApiError ? error.message : "Could not delete the monitor.");
    } finally {
      setConfirmDelete(false);
    }
  }

  if (notFound) {
    return (
      <div className="min-h-screen bg-background">
        <AppHeader />
        <main className="mx-auto max-w-6xl px-4 py-16 text-center sm:px-6">
          <h1 className="text-2xl font-semibold tracking-tight">Monitor not found</h1>
          <p className="mt-2 text-sm text-muted-foreground">
            It may have been deleted, or you followed a bad link.
          </p>
          <Button asChild className="mt-6">
            <Link to="/">Back to monitors</Link>
          </Button>
        </main>
      </div>
    );
  }

  const state = monitor ? monitorState(monitor.isPaused, monitor.lastStatusUp) : "unknown";

  return (
    <div className="min-h-screen bg-background">
      <AppHeader />
      <main className="mx-auto max-w-6xl px-4 py-8 sm:px-6">
        <Button variant="ghost" size="sm" asChild className="mb-4">
          <Link to="/">
            <ArrowLeftIcon data-icon="inline-start" />
            Monitors
          </Link>
        </Button>

        {monitor === null ? (
          <div className="flex flex-col gap-4">
            <Skeleton className="h-10 w-64" />
            <Skeleton className="h-24 w-full" />
            <Skeleton className="h-64 w-full" />
          </div>
        ) : (
          <>
            <div className="flex flex-wrap items-start justify-between gap-4">
              <div>
                <div className="flex items-center gap-3">
                  <h1 className="text-2xl font-semibold tracking-tight">{monitor.name}</h1>
                  <Badge variant="outline" className="font-mono">
                    {monitor.protocol.toUpperCase()}
                  </Badge>
                </div>
                <p className="mt-1 font-mono text-sm text-muted-foreground">{monitor.url}</p>
                <div className="mt-2">
                  <StatusLabel state={state} />
                </div>
              </div>
              <div className="flex gap-2">
                <Button variant="outline" onClick={() => setDialogOpen(true)}>
                  <PencilIcon data-icon="inline-start" />
                  Edit
                </Button>
                <Button variant="outline" onClick={togglePaused}>
                  {monitor.isPaused ? (
                    <>
                      <PlayIcon data-icon="inline-start" />
                      Resume
                    </>
                  ) : (
                    <>
                      <PauseIcon data-icon="inline-start" />
                      Pause
                    </>
                  )}
                </Button>
                <Button variant="destructive" onClick={() => setConfirmDelete(true)}>
                  <Trash2Icon data-icon="inline-start" />
                  Delete
                </Button>
              </div>
            </div>

            <div className="mt-6 grid gap-4 sm:grid-cols-4">
              <Card>
                <CardHeader className="pb-2">
                  <CardTitle className="text-sm font-medium text-muted-foreground">
                    Uptime (all time)
                  </CardTitle>
                </CardHeader>
                <CardContent>
                  <p className="font-mono text-2xl font-semibold tabular-nums">
                    {summary ? formatUptime(summary.uptimePercentage) : "—"}
                  </p>
                </CardContent>
              </Card>
              <Card>
                <CardHeader className="pb-2">
                  <CardTitle className="text-sm font-medium text-muted-foreground">
                    Avg response
                  </CardTitle>
                </CardHeader>
                <CardContent>
                  <p className="font-mono text-2xl font-semibold tabular-nums">
                    {summary ? `${Math.round(summary.averageResponseTimeMs)} ms` : "—"}
                  </p>
                </CardContent>
              </Card>
              <Card>
                <CardHeader className="pb-2">
                  <CardTitle className="text-sm font-medium text-muted-foreground">Checks</CardTitle>
                </CardHeader>
                <CardContent>
                  <p className="font-mono text-2xl font-semibold tabular-nums">
                    {summary ? summary.totalChecks : "—"}
                  </p>
                </CardContent>
              </Card>
              <Card>
                <CardHeader className="pb-2">
                  <CardTitle className="text-sm font-medium text-muted-foreground">
                    Last checked
                  </CardTitle>
                </CardHeader>
                <CardContent>
                  <p className="text-2xl font-semibold">{formatRelative(monitor.lastCheckedAt)}</p>
                </CardContent>
              </Card>
            </div>

            <Card className="mt-6">
              <CardHeader>
                <CardTitle>Response time</CardTitle>
              </CardHeader>
              <CardContent>
                {history === null ? (
                  <Skeleton className="h-[220px] w-full" />
                ) : history.length === 0 ? (
                  <p className="text-sm text-muted-foreground">
                    No checks recorded in the last 7 days yet.
                  </p>
                ) : (
                  <ResponseChart checks={history} />
                )}
              </CardContent>
            </Card>

            <Card className="mt-6">
              <CardHeader>
                <CardTitle>Recent checks</CardTitle>
              </CardHeader>
              <CardContent className="p-0">
                {history === null ? (
                  <div className="flex flex-col gap-3 p-6">
                    <Skeleton className="h-8 w-full" />
                    <Skeleton className="h-8 w-full" />
                    <Skeleton className="h-8 w-full" />
                  </div>
                ) : history.length === 0 ? (
                  <p className="p-6 text-sm text-muted-foreground">
                    No checks recorded in the last 7 days yet.
                  </p>
                ) : (
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead>Time</TableHead>
                        <TableHead>Result</TableHead>
                        <TableHead>Status</TableHead>
                        <TableHead className="text-right">Response</TableHead>
                        <TableHead>Detail</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {history.map((check) => (
                        <TableRow key={check.checkId}>
                          <TableCell className="whitespace-nowrap text-muted-foreground">
                            {formatTimestamp(check.timestamp)}
                          </TableCell>
                          <TableCell>
                            <StatusLabel state={check.isUp ? "up" : "down"} />
                          </TableCell>
                          <TableCell className="font-mono tabular-nums">
                            {check.statusCode ?? "—"}
                          </TableCell>
                          <TableCell className="text-right font-mono tabular-nums">
                            {check.responseTimeMs} ms
                          </TableCell>
                          <TableCell className="max-w-64 truncate text-muted-foreground">
                            {check.errorMessage ?? "—"}
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                )}
              </CardContent>
            </Card>
          </>
        )}
      </main>

      {monitor && (
        <MonitorFormDialog
          open={dialogOpen}
          onOpenChange={setDialogOpen}
          initial={monitor}
          onSaved={(saved) => {
            setMonitor(saved);
            load();
          }}
        />
      )}

      <AlertDialog open={confirmDelete} onOpenChange={setConfirmDelete}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete this monitor?</AlertDialogTitle>
            <AlertDialogDescription>
              {monitor
                ? `“${monitor.name}” and its check history will be removed permanently.`
                : "This monitor and its check history will be removed permanently."}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction onClick={handleDelete}>Delete</AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
