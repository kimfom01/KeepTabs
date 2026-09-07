import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router";
import { EllipsisIcon, EyeIcon, PauseIcon, PencilIcon, PlayIcon, PlusIcon, RadarIcon, Trash2Icon } from "lucide-react";
import { toast } from "sonner";
import { AppHeader } from "@/components/app-header";
import { MonitorFormDialog } from "@/components/monitor-form-dialog";
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
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import {
  Empty,
  EmptyContent,
  EmptyDescription,
  EmptyHeader,
  EmptyMedia,
  EmptyTitle,
} from "@/components/ui/empty";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { ApiError, monitorsApi, type Monitor } from "@/lib/api";
import { formatInterval, formatRelative } from "@/lib/format";

export function DashboardPage() {
  const [monitors, setMonitors] = useState<Monitor[] | null>(null);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editing, setEditing] = useState<Monitor | null>(null);
  const [deleting, setDeleting] = useState<Monitor | null>(null);

  const load = useCallback(async () => {
    try {
      setMonitors(await monitorsApi.list());
    } catch (error) {
      toast.error(error instanceof ApiError ? error.message : "Could not load monitors.");
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  const up = monitors?.filter((m) => !m.isPaused && m.lastStatusUp === true).length ?? 0;
  const down = monitors?.filter((m) => !m.isPaused && m.lastStatusUp === false).length ?? 0;
  const paused = monitors?.filter((m) => m.isPaused).length ?? 0;

  function openCreate() {
    setEditing(null);
    setDialogOpen(true);
  }

  function openEdit(monitor: Monitor) {
    setEditing(monitor);
    setDialogOpen(true);
  }

  async function togglePaused(monitor: Monitor) {
    try {
      const updated = monitor.isPaused
        ? await monitorsApi.resume(monitor.monitorId)
        : await monitorsApi.pause(monitor.monitorId);
      setMonitors((current) =>
        current?.map((m) => (m.monitorId === updated.monitorId ? updated : m)) ?? null,
      );
      toast.success(updated.isPaused ? "Monitor paused." : "Monitor resumed.");
    } catch (error) {
      toast.error(error instanceof ApiError ? error.message : "Could not update the monitor.");
    }
  }

  async function confirmDelete() {
    if (!deleting) return;
    try {
      await monitorsApi.remove(deleting.monitorId);
      setMonitors((current) => current?.filter((m) => m.monitorId !== deleting.monitorId) ?? null);
      toast.success("Monitor deleted.");
    } catch (error) {
      toast.error(error instanceof ApiError ? error.message : "Could not delete the monitor.");
    } finally {
      setDeleting(null);
    }
  }

  return (
    <div className="min-h-screen bg-background">
      <AppHeader />
      <main className="mx-auto max-w-6xl px-4 py-8 sm:px-6">
        <div className="flex items-center justify-between gap-4">
          <div>
            <h1 className="text-2xl font-semibold tracking-tight">Monitors</h1>
            <p className="text-sm text-muted-foreground">
              Every target you keep tabs on, and its latest heartbeat.
            </p>
          </div>
          <Button onClick={openCreate}>
            <PlusIcon data-icon="inline-start" />
            New monitor
          </Button>
        </div>

        <div className="mt-6 grid gap-4 sm:grid-cols-3">
          <Card>
            <CardHeader className="pb-2">
              <CardTitle className="text-sm font-medium text-muted-foreground">Operational</CardTitle>
            </CardHeader>
            <CardContent>
              {monitors === null ? (
                <Skeleton className="h-8 w-16" />
              ) : (
                <p className="font-mono text-3xl font-semibold tabular-nums">{up}</p>
              )}
            </CardContent>
          </Card>
          <Card>
            <CardHeader className="pb-2">
              <CardTitle className="text-sm font-medium text-muted-foreground">Down</CardTitle>
            </CardHeader>
            <CardContent>
              {monitors === null ? (
                <Skeleton className="h-8 w-16" />
              ) : (
                <p className="font-mono text-3xl font-semibold tabular-nums">{down}</p>
              )}
            </CardContent>
          </Card>
          <Card>
            <CardHeader className="pb-2">
              <CardTitle className="text-sm font-medium text-muted-foreground">Paused</CardTitle>
            </CardHeader>
            <CardContent>
              {monitors === null ? (
                <Skeleton className="h-8 w-16" />
              ) : (
                <p className="font-mono text-3xl font-semibold tabular-nums">{paused}</p>
              )}
            </CardContent>
          </Card>
        </div>

        <Card className="mt-6">
          <CardContent className="p-0">
            {monitors === null ? (
              <div className="flex flex-col gap-3 p-6">
                <Skeleton className="h-10 w-full" />
                <Skeleton className="h-10 w-full" />
                <Skeleton className="h-10 w-full" />
              </div>
            ) : monitors.length === 0 ? (
              <Empty className="py-12">
                <EmptyHeader>
                  <EmptyMedia variant="icon">
                    <RadarIcon />
                  </EmptyMedia>
                  <EmptyTitle>No monitors yet</EmptyTitle>
                  <EmptyDescription>
                    Add your first target and KeepTabs will start checking it within a minute.
                  </EmptyDescription>
                </EmptyHeader>
                <EmptyContent>
                  <Button onClick={openCreate}>
                    <PlusIcon data-icon="inline-start" />
                    New monitor
                  </Button>
                </EmptyContent>
              </Empty>
            ) : (
              <div className="overflow-x-auto">
                <Table className="min-w-[680px]">
                <TableHeader>
                  <TableRow>
                    <TableHead>Status</TableHead>
                    <TableHead>Monitor</TableHead>
                    <TableHead>Protocol</TableHead>
                    <TableHead>Interval</TableHead>
                    <TableHead>Last checked</TableHead>
                    <TableHead className="w-12">
                      <span className="sr-only">Actions</span>
                    </TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {monitors.map((monitor) => {
                    const state = monitorState(monitor.isPaused, monitor.lastStatusUp);
                    return (
                      <TableRow key={monitor.monitorId}>
                        <TableCell>
                          <StatusLabel state={state} />
                        </TableCell>
                        <TableCell>
                          <Link
                            to={`/monitors/${monitor.monitorId}`}
                            className="font-medium hover:underline"
                          >
                            {monitor.name}
                          </Link>
                          <p className="truncate font-mono text-xs text-muted-foreground">
                            {monitor.url}
                          </p>
                        </TableCell>
                        <TableCell>
                          <Badge variant="outline" className="font-mono">
                            {monitor.protocol.toUpperCase()}
                          </Badge>
                        </TableCell>
                        <TableCell className="font-mono tabular-nums">
                          {formatInterval(monitor.checkIntervalSeconds)}
                        </TableCell>
                        <TableCell className="text-muted-foreground">
                          {formatRelative(monitor.lastCheckedAt)}
                        </TableCell>
                        <TableCell>
                          <DropdownMenu>
                            <DropdownMenuTrigger asChild>
                              <Button variant="ghost" size="icon">
                                <EllipsisIcon data-icon="inline-start" />
                                <span className="sr-only">Monitor actions</span>
                              </Button>
                            </DropdownMenuTrigger>
                            <DropdownMenuContent align="end">
                              <DropdownMenuGroup>
                                <DropdownMenuItem asChild>
                                  <Link to={`/monitors/${monitor.monitorId}`}>
                                    <EyeIcon data-icon="inline-start" />
                                    View details
                                  </Link>
                                </DropdownMenuItem>
                                <DropdownMenuItem onSelect={() => openEdit(monitor)}>
                                  <PencilIcon data-icon="inline-start" />
                                  Edit
                                </DropdownMenuItem>
                                <DropdownMenuItem onSelect={() => togglePaused(monitor)}>
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
                                </DropdownMenuItem>
                                <DropdownMenuItem
                                  variant="destructive"
                                  onSelect={() => setDeleting(monitor)}
                                >
                                  <Trash2Icon data-icon="inline-start" />
                                  Delete
                                </DropdownMenuItem>
                              </DropdownMenuGroup>
                            </DropdownMenuContent>
                          </DropdownMenu>
                        </TableCell>
                      </TableRow>
                    );
                  })}
                </TableBody>
                </Table>
              </div>
            )}
          </CardContent>
        </Card>
      </main>

      <MonitorFormDialog
        open={dialogOpen}
        onOpenChange={setDialogOpen}
        initial={editing}
        onSaved={(saved) => {
          setMonitors((current) => {
            if (!current) return [saved];
            return current.some((m) => m.monitorId === saved.monitorId)
              ? current.map((m) => (m.monitorId === saved.monitorId ? saved : m))
              : [saved, ...current];
          });
        }}
      />

      <AlertDialog open={deleting !== null} onOpenChange={(open) => !open && setDeleting(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete this monitor?</AlertDialogTitle>
            <AlertDialogDescription>
              {deleting
                ? `“${deleting.name}” and its check history will be removed permanently.`
                : "This monitor and its check history will be removed permanently."}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction onClick={confirmDelete}>Delete</AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
