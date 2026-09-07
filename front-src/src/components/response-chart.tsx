import { Area, AreaChart, CartesianGrid, XAxis, YAxis } from "recharts";
import {
  ChartContainer,
  ChartTooltip,
  ChartTooltipContent,
  type ChartConfig,
} from "@/components/ui/chart";
import type { MonitorCheck } from "@/lib/api";

const chartConfig = {
  response: {
    label: "Response",
    color: "var(--chart-2)",
  },
} satisfies ChartConfig;

function formatTick(iso: string): string {
  const date = new Date(iso);
  return `${date.toLocaleDateString([], { month: "numeric", day: "numeric" })} ${date.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })}`;
}

export function ResponseChart({ checks }: { checks: MonitorCheck[] }) {
  const data = [...checks]
    .reverse()
    .map((check) => ({ time: formatTick(check.timestamp), ms: check.responseTimeMs }));

  return (
    <ChartContainer config={chartConfig} className="h-[220px] w-full">
      <AreaChart accessibilityLayer data={data} margin={{ left: 0, right: 8 }}>
        <CartesianGrid vertical={false} />
        <XAxis dataKey="time" tickLine={false} axisLine={false} tickMargin={8} minTickGap={48} />
        <YAxis
          tickLine={false}
          axisLine={false}
          tickMargin={8}
          width={56}
          tickFormatter={(value: number) => `${value}ms`}
        />
        <ChartTooltip content={<ChartTooltipContent />} />
        <Area
          dataKey="ms"
          type="monotone"
          fill="var(--color-response)"
          fillOpacity={0.25}
          stroke="var(--color-response)"
        />
      </AreaChart>
    </ChartContainer>
  );
}
