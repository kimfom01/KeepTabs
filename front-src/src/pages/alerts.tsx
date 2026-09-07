import { useCallback, useEffect, useState } from "react";
import { BellRingIcon } from "lucide-react";
import { toast } from "sonner";
import { AppHeader } from "@/components/app-header";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
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
import { ApiError, alertsApi, type AlertLog } from "@/lib/api";
import { formatTimestamp } from "@/lib/format";

export function AlertsPage() {
  const [logs, setLogs] = useState<AlertLog[] | null>(null);

  const load = useCallback(async () => {
    try {
      setLogs(await alertsApi.logs());
    } catch (error) {
      toast.error(error instanceof ApiError ? error.message : "Could not load alert deliveries.");
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  return (
    <div className="min-h-screen bg-background">
      <AppHeader />
      <main className="mx-auto max-w-6xl px-4 py-8 sm:px-6">
        <h1 className="text-2xl font-semibold tracking-tight">Alerts</h1>
        <p className="text-sm text-muted-foreground">
          Every alert delivery attempt across your monitors, newest first.
        </p>

        <Card className="mt-6">
          <CardHeader>
            <CardTitle>Delivery log</CardTitle>
          </CardHeader>
          <CardContent className="p-0">
            {logs === null ? (
              <div className="flex flex-col gap-3 p-6">
                <Skeleton className="h-10 w-full" />
                <Skeleton className="h-10 w-full" />
                <Skeleton className="h-10 w-full" />
              </div>
            ) : logs.length === 0 ? (
              <Empty className="py-12">
                <EmptyHeader>
                  <EmptyMedia variant="icon">
                    <BellRingIcon />
                  </EmptyMedia>
                  <EmptyTitle>No alerts yet</EmptyTitle>
                  <EmptyDescription>
                    Add an alert rule on a monitor and deliveries will appear here.
                  </EmptyDescription>
                </EmptyHeader>
                <EmptyContent />
              </Empty>
            ) : (
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Time</TableHead>
                    <TableHead>Monitor</TableHead>
                    <TableHead>Message</TableHead>
                    <TableHead>Result</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {logs.map((log) => (
                    <TableRow key={log.alertLogId}>
                      <TableCell className="whitespace-nowrap text-muted-foreground">
                        {formatTimestamp(log.firedAt)}
                      </TableCell>
                      <TableCell className="font-medium">{log.monitorName}</TableCell>
                      <TableCell className="max-w-72 truncate">{log.message}</TableCell>
                      <TableCell>
                        {log.success ? (
                          <Badge variant="secondary">Delivered</Badge>
                        ) : (
                          <Badge variant="destructive">
                            Failed{log.error ? `: ${log.error}` : ""}
                          </Badge>
                        )}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </CardContent>
        </Card>
      </main>
    </div>
  );
}
