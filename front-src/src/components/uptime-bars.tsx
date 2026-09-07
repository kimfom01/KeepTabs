import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";
import type { DailyUptime } from "@/lib/api";
import { cn } from "@/lib/utils";

export interface AvailabilityBar {
  key: string;
  tooltip: string;
  uptime: number | null;
}

function barColor(uptime: number | null): string {
  if (uptime === null) return "bg-muted";
  if (uptime >= 99.9) return "bg-emerald-500";
  if (uptime > 0) return "bg-amber-500";
  return "bg-rose-500";
}

export function AvailabilityBars({ items }: { items: AvailabilityBar[] }) {
  return (
    <div className="flex items-end gap-1" role="img" aria-label="Availability bars">
      {items.map((item) => (
        <Tooltip key={item.key}>
          <TooltipTrigger asChild>
            <div
              className={cn("min-w-0 flex-1 rounded-sm", barColor(item.uptime))}
              style={{ height: item.uptime === null ? "8px" : `${Math.max(12, item.uptime)}px` }}
            />
          </TooltipTrigger>
          <TooltipContent>{item.tooltip}</TooltipContent>
        </Tooltip>
      ))}
    </div>
  );
}

function dayLabel(day: DailyUptime): string {
  if (day.totalChecks === 0) return `${day.date}: no data`;
  return `${day.date}: ${day.uptimePercentage.toFixed(2)}% up (${day.upCount}/${day.totalChecks} checks)`;
}

export function UptimeBars({ days, loading }: { days: DailyUptime[] | null; loading: boolean }) {
  // Fixed 30-day window, oldest first; missing days render as empty slots.
  const ordered = (days ?? []).slice(-30);
  const filled: (DailyUptime | null)[] = [
    ...Array<DailyUptime | null>(Math.max(0, 30 - ordered.length)).fill(null),
    ...ordered,
  ];

  return (
    <Card>
      <CardHeader className="pb-2">
        <CardTitle className="text-sm font-medium text-muted-foreground">
          Uptime · last 30 days
        </CardTitle>
      </CardHeader>
      <CardContent>
        {loading ? (
          <Skeleton className="h-12 w-full" />
        ) : (
          <AvailabilityBars
            items={filled.map((day, index) => ({
              key: day ? day.date : `empty-${index}`,
              tooltip: day ? dayLabel(day) : "No data",
              uptime: day && day.totalChecks > 0 ? day.uptimePercentage : null,
            }))}
          />
        )}
      </CardContent>
    </Card>
  );
}
