import { cn } from "@/lib/utils";

type MonitorState = "up" | "down" | "paused" | "unknown";

const dotStyles: Record<MonitorState, string> = {
  up: "bg-emerald-500",
  down: "bg-rose-500",
  paused: "bg-amber-500",
  unknown: "bg-muted-foreground/50",
};

const labelStyles: Record<MonitorState, string> = {
  up: "text-emerald-700",
  down: "text-rose-700",
  paused: "text-amber-700",
  unknown: "text-muted-foreground",
};

export function monitorState(isPaused: boolean, lastStatusUp: boolean | null): MonitorState {
  if (isPaused) return "paused";
  if (lastStatusUp === true) return "up";
  if (lastStatusUp === false) return "down";
  return "unknown";
}

const stateLabels: Record<MonitorState, string> = {
  up: "Up",
  down: "Down",
  paused: "Paused",
  unknown: "No data",
};

export function StatusDot({ state, pulse = false }: { state: MonitorState; pulse?: boolean }) {
  return (
    <span className="relative flex size-2.5 shrink-0">
      {pulse && state === "down" && (
        <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-rose-400 opacity-60" />
      )}
      <span className={cn("relative inline-flex size-2.5 rounded-full", dotStyles[state])} />
    </span>
  );
}

export function StatusLabel({ state }: { state: MonitorState }) {
  return (
    <span className="flex items-center gap-2">
      <StatusDot state={state} pulse />
      <span className={cn("text-sm font-medium", labelStyles[state])}>{stateLabels[state]}</span>
    </span>
  );
}
