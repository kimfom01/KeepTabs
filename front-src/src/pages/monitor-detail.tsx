import { useCallback, useEffect, useState } from "react";
import { Link, useNavigate, useParams } from "react-router";
import {
  ArrowLeftIcon,
  PauseIcon,
  PencilIcon,
  PlayIcon,
  PlusIcon,
  SendIcon,
  Trash2Icon,
} from "lucide-react";
import { toast } from "sonner";
import { AlertRuleDialog } from "@/components/alert-rule-dialog";
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
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectGroup,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Skeleton } from "@/components/ui/skeleton";
import { Switch } from "@/components/ui/switch";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { ApiError, alertsApi, monitorsApi } from "@/lib/api";
import type {
  AlertLog,
  AlertRule,
  Monitor,
  MonitorCheck,
  MonitorSummary,
} from "@/lib/api";
import { formatRelative, formatTimestamp, formatUptime } from "@/lib/format";

export function MonitorDetailPage() {
  const { monitorId } = useParams();
  const navigate = useNavigate();
  const [monitor, setMonitor] = useState<Monitor | null>(null);
  const [summary, setSummary] = useState<MonitorSummary | null>(null);
  const [history, setHistory] = useState<MonitorCheck[] | null>(null);
  const [rules, setRules] = useState<AlertRule[] | null>(null);
  const [lastDelivery, setLastDelivery] = useState<AlertLog | null>(null);
  const [notFound, setNotFound] = useState(false);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [confirmDelete, setConfirmDelete] = useState(false);
  const [ruleDialogOpen, setRuleDialogOpen] = useState(false);
  const [editingRule, setEditingRule] = useState<AlertRule | null>(null);
  const [deletingRule, setDeletingRule] = useState<AlertRule | null>(null);
  const [testingRuleId, setTestingRuleId] = useState<string | null>(null);
  const [resultFilter, setResultFilter] = useState<"all" | "up" | "down">("all");
  const [statusFilter, setStatusFilter] = useState("");
  const [checksPage, setChecksPage] = useState(0);
  const checksPageSize = 10;

  const load = useCallback(async () => {
    if (!monitorId) return;
    try {
      const [loaded, loadedSummary, loadedHistory, loadedRules, loadedLogs] = await Promise.all([
        monitorsApi.get(monitorId),
        monitorsApi.summary(monitorId),
        monitorsApi.history(monitorId, 7),
        alertsApi.list(monitorId),
        alertsApi.logs(monitorId),
      ]);
      setMonitor(loaded);
      setSummary(loadedSummary);
      setHistory(loadedHistory);
      setRules(loadedRules);
      setLastDelivery(loadedLogs[0] ?? null);
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

  async function toggleRule(rule: AlertRule) {
    try {
      const updated = await alertsApi.update(rule.alertRuleId, { isEnabled: !rule.isEnabled });
      setRules((current) =>
        current?.map((r) => (r.alertRuleId === updated.alertRuleId ? updated : r)) ?? null,
      );
    } catch (error) {
      toast.error(error instanceof ApiError ? error.message : "Could not update the rule.");
    }
  }

  async function testRule(rule: AlertRule) {
    setTestingRuleId(rule.alertRuleId);
    try {
      const result = await alertsApi.test(rule.alertRuleId);
      if (result.success) {
        toast.success("Test alert delivered.");
      } else {
        toast.error(result.error ?? "Test delivery failed.");
      }
      load();
    } catch (error) {
      toast.error(error instanceof ApiError ? error.message : "Could not send a test alert.");
    } finally {
      setTestingRuleId(null);
    }
  }

  async function confirmRuleDelete() {
    if (!deletingRule) return;
    try {
      await alertsApi.remove(deletingRule.alertRuleId);
      setRules((current) => current?.filter((r) => r.alertRuleId !== deletingRule.alertRuleId) ?? null);
      toast.success("Alert rule deleted.");
    } catch (error) {
      toast.error(error instanceof ApiError ? error.message : "Could not delete the rule.");
    } finally {
      setDeletingRule(null);
    }
  }

  const filteredChecks = (history ?? []).filter((check) => {
    if (resultFilter === "up" && !check.isUp) return false;
    if (resultFilter === "down" && check.isUp) return false;
    const needle = statusFilter.trim();
    if (needle && !(check.statusCode !== null && String(check.statusCode).includes(needle))) {
      return false;
    }
    return true;
  });
  const checksPageCount = Math.max(1, Math.ceil(filteredChecks.length / checksPageSize));
  const safeChecksPage = Math.min(checksPage, checksPageCount - 1);
  const pagedChecks = filteredChecks.slice(
    safeChecksPage * checksPageSize,
    safeChecksPage * checksPageSize + checksPageSize,
  );

  function updateResultFilter(value: "all" | "up" | "down") {
    setResultFilter(value);
    setChecksPage(0);
  }

  function updateStatusFilter(value: string) {
    setStatusFilter(value);
    setChecksPage(0);
  }

  const triggerLabels: Record<AlertRule["triggerType"], string> = {
    OnDown: "Goes down",
    OnUp: "Recovers",
    ConsecutiveFailures: "Fails repeatedly",
  };

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
              <CardHeader className="flex flex-row items-start justify-between gap-4">
                <div>
                  <CardTitle>Alerts</CardTitle>
                  <CardDescription>
                    {rules === null
                      ? "Loading rules…"
                      : rules.length === 0
                        ? "No rules yet. Add one to get notified."
                        : lastDelivery
                          ? `Last delivery ${lastDelivery.success ? "succeeded" : "failed"} ${formatRelative(lastDelivery.firedAt)}.`
                          : "No deliveries recorded yet."}
                  </CardDescription>
                </div>
                <Button
                  size="sm"
                  onClick={() => {
                    setEditingRule(null);
                    setRuleDialogOpen(true);
                  }}
                >
                  <PlusIcon data-icon="inline-start" />
                  Rule
                </Button>
              </CardHeader>
              <CardContent className="p-0">
                {rules === null ? (
                  <div className="flex flex-col gap-3 p-6">
                    <Skeleton className="h-8 w-full" />
                    <Skeleton className="h-8 w-full" />
                  </div>
                ) : rules.length === 0 ? (
                  <p className="p-6 pt-0 text-sm text-muted-foreground">
                    Rules watch this monitor and notify you through email or webhooks.
                  </p>
                ) : (
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead>Trigger</TableHead>
                        <TableHead>Channel</TableHead>
                        <TableHead>Destination</TableHead>
                        <TableHead>Last fired</TableHead>
                        <TableHead>Enabled</TableHead>
                        <TableHead className="w-24">
                          <span className="sr-only">Actions</span>
                        </TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {rules.map((rule) => (
                        <TableRow key={rule.alertRuleId}>
                          <TableCell className="font-medium">
                            {triggerLabels[rule.triggerType]}
                            {rule.triggerType === "ConsecutiveFailures" && (
                              <span className="text-muted-foreground"> × {rule.threshold}</span>
                            )}
                          </TableCell>
                          <TableCell>
                            <Badge variant="outline">{rule.type}</Badge>
                          </TableCell>
                          <TableCell className="max-w-48 truncate font-mono text-xs">
                            {rule.target}
                          </TableCell>
                          <TableCell className="text-muted-foreground">
                            {formatRelative(rule.lastFiredAt)}
                          </TableCell>
                          <TableCell>
                            <Switch
                              checked={rule.isEnabled}
                              onCheckedChange={() => toggleRule(rule)}
                              aria-label={`Enable rule for ${rule.target}`}
                            />
                          </TableCell>
                          <TableCell>
                            <div className="flex gap-1">
                              <Button
                                variant="ghost"
                                size="icon"
                                disabled={testingRuleId === rule.alertRuleId}
                                onClick={() => testRule(rule)}
                              >
                                <SendIcon data-icon="inline-start" />
                                <span className="sr-only">Send test alert</span>
                              </Button>
                              <Button
                                variant="ghost"
                                size="icon"
                                onClick={() => {
                                  setEditingRule(rule);
                                  setRuleDialogOpen(true);
                                }}
                              >
                                <PencilIcon data-icon="inline-start" />
                                <span className="sr-only">Edit rule</span>
                              </Button>
                              <Button variant="ghost" size="icon" onClick={() => setDeletingRule(rule)}>
                                <Trash2Icon data-icon="inline-start" />
                                <span className="sr-only">Delete rule</span>
                              </Button>
                            </div>
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
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
                  <>
                    <div className="flex flex-wrap items-center gap-3 border-b px-6 py-3">
                      <Select
                        value={resultFilter}
                        onValueChange={(value) =>
                          updateResultFilter(value as "all" | "up" | "down")
                        }
                      >
                        <SelectTrigger className="w-32">
                          <SelectValue />
                        </SelectTrigger>
                        <SelectContent>
                          <SelectGroup>
                            <SelectItem value="all">All results</SelectItem>
                            <SelectItem value="up">Up only</SelectItem>
                            <SelectItem value="down">Down only</SelectItem>
                          </SelectGroup>
                        </SelectContent>
                      </Select>
                      <Input
                        value={statusFilter}
                        onChange={(event) => updateStatusFilter(event.target.value)}
                        placeholder="Filter by status…"
                        inputMode="numeric"
                        className="w-40 font-mono"
                      />
                      <span className="ml-auto text-sm text-muted-foreground">
                        {filteredChecks.length} of {history.length} checks
                      </span>
                    </div>
                    {pagedChecks.length === 0 ? (
                      <p className="p-6 text-sm text-muted-foreground">
                        No checks match these filters.
                      </p>
                    ) : (
                      <Table>
                        <TableHeader>
                          <TableRow>
                            <TableHead>Time</TableHead>
                            <TableHead>Result</TableHead>
                            <TableHead>Status</TableHead>
                            <TableHead className="text-right">Response</TableHead>
                            <TableHead>Cert</TableHead>
                            <TableHead>Detail</TableHead>
                          </TableRow>
                        </TableHeader>
                        <TableBody>
                          {pagedChecks.map((check) => (
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
                              <TableCell className="font-mono tabular-nums">
                                {check.sslDaysRemaining === null ? "—" : `${check.sslDaysRemaining}d`}
                              </TableCell>
                              <TableCell className="max-w-64 truncate text-muted-foreground">
                                {check.errorMessage ?? "—"}
                              </TableCell>
                            </TableRow>
                          ))}
                        </TableBody>
                      </Table>
                    )}
                    {checksPageCount > 1 && (
                      <div className="flex items-center justify-between border-t px-6 py-3">
                        <span className="text-sm text-muted-foreground">
                          Page {safeChecksPage + 1} of {checksPageCount}
                        </span>
                        <div className="flex gap-2">
                          <Button
                            variant="outline"
                            size="sm"
                            disabled={safeChecksPage === 0}
                            onClick={() => setChecksPage(safeChecksPage - 1)}
                          >
                            Previous
                          </Button>
                          <Button
                            variant="outline"
                            size="sm"
                            disabled={safeChecksPage >= checksPageCount - 1}
                            onClick={() => setChecksPage(safeChecksPage + 1)}
                          >
                            Next
                          </Button>
                        </div>
                      </div>
                    )}
                  </>
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

      {monitor && (
        <AlertRuleDialog
          open={ruleDialogOpen}
          onOpenChange={setRuleDialogOpen}
          monitorId={monitor.monitorId}
          initial={editingRule}
          onSaved={(saved) => {
            setRules((current) => {
              if (!current) return [saved];
              return current.some((r) => r.alertRuleId === saved.alertRuleId)
                ? current.map((r) => (r.alertRuleId === saved.alertRuleId ? saved : r))
                : [...current, saved];
            });
          }}
        />
      )}

      <AlertDialog open={deletingRule !== null} onOpenChange={(open) => !open && setDeletingRule(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete this alert rule?</AlertDialogTitle>
            <AlertDialogDescription>
              Notifications for this rule stop immediately. Past deliveries stay in the log.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction onClick={confirmRuleDelete}>Delete</AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
