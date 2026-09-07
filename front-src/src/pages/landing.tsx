import { useRef } from "react";
import { Link } from "react-router";
import { useGSAP } from "@gsap/react";
import gsap from "gsap";
import { ScrollTrigger } from "gsap/ScrollTrigger";
import { ArrowRightIcon, BellRingIcon, ChartLineIcon, GlobeIcon, ServerIcon } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { PublicFooter } from "@/components/public-footer";
import { PublicHeader } from "@/components/public-header";
import { useAuth } from "@/lib/auth";
import { cn } from "@/lib/utils";

const liveRows = [
  { name: "Marketing site", url: "https://example.com", state: "up" as const, detail: "200 · 123ms" },
  { name: "API", url: "https://api.example.com/health", state: "up" as const, detail: "200 · 87ms" },
  { name: "Database", url: "db:5432", state: "down" as const, detail: "refused · 0ms" },
];

const features = [
  {
    icon: GlobeIcon,
    title: "HTTP, TCP, and Ping",
    description: "Watch websites, ports, and hosts with per-target intervals, timeouts, and expected statuses.",
  },
  {
    icon: BellRingIcon,
    title: "Alerts that reach you",
    description: "Email, webhook, and Telegram rules on outages, recoveries, and repeated failures — with cooldowns.",
  },
  {
    icon: ChartLineIcon,
    title: "History that answers",
    description: "Response-time charts, uptime summaries, and filterable check history, including certificate expiry.",
  },
  {
    icon: ServerIcon,
    title: "Yours, entirely",
    description: "One container holds the API, worker, database, and UI. No accounts elsewhere, no per-monitor pricing.",
  },
];

const steps = [
  { title: "Add a target", description: "A URL, a host:port, or a hostname. Thirty seconds of setup." },
  { title: "Checks run themselves", description: "The worker probes on your schedule and records every result." },
  { title: "Get woken up properly", description: "Down, recovered, or flaky — the alert finds you with context." },
];

export function LandingPage() {
  const { user } = useAuth();
  const root = useRef<HTMLDivElement>(null);

  useGSAP(
    () => {
      gsap.registerPlugin(ScrollTrigger);
      if (window.matchMedia("(prefers-reduced-motion: reduce)").matches) {
        return;
      }

      gsap.fromTo(
        "[data-hero-item]",
        { y: 18, opacity: 0 },
        { y: 0, opacity: 1, duration: 0.6, ease: "power2.out", stagger: 0.09 },
      );
      gsap.fromTo(
        "[data-hero-board]",
        { y: 24, opacity: 0 },
        { y: 0, opacity: 1, duration: 0.7, ease: "power2.out", delay: 0.25 },
      );
      gsap.utils.toArray<HTMLElement>("[data-reveal]").forEach((element) => {
        gsap.fromTo(
          element,
          { y: 22, opacity: 0 },
          {
            y: 0,
            opacity: 1,
            duration: 0.6,
            ease: "power2.out",
            scrollTrigger: { trigger: element, start: "top 88%" },
          },
        );
      });
    },
    { scope: root },
  );

  return (
    <div ref={root} className="min-h-screen bg-background">
      <PublicHeader />

      <main className="mx-auto max-w-6xl px-4 sm:px-6">
        <section className="flex flex-col items-center gap-8 py-16 sm:py-24 lg:flex-row lg:gap-12">
          <div className="flex max-w-xl flex-col items-start gap-5">
            <Badge variant="secondary" data-hero-item>Free · Self-hosted · Open source</Badge>
            <h1 data-hero-item className="text-4xl font-semibold tracking-tight text-balance sm:text-5xl">
              Know the second your site goes down.
            </h1>
            <p data-hero-item className="text-lg text-muted-foreground">
              KeepTabs watches your services around the clock and taps you on the
              shoulder — by email, webhook, or Telegram — before your users notice.
            </p>
            <div data-hero-item className="flex flex-wrap gap-3">
              {user ? (
                <Button size="lg" asChild>
                  <Link to="/dashboard">
                    Open dashboard
                    <ArrowRightIcon data-icon="inline-start" />
                  </Link>
                </Button>
              ) : (
                <>
                  <Button size="lg" asChild>
                    <Link to="/register">
                      Start monitoring
                      <ArrowRightIcon data-icon="inline-start" />
                    </Link>
                  </Button>
                  <Button size="lg" variant="outline" asChild>
                    <Link to="/login">Sign in</Link>
                  </Button>
                </>
              )}
            </div>
            <p data-hero-item className="font-mono text-xs text-muted-foreground">
              docker compose up -d <span className="mx-1">·</span> http://localhost:8080
            </p>
          </div>

          <Card data-hero-board className="w-full max-w-md lg:ml-auto">
            <CardHeader className="pb-2">
              <CardTitle className="text-sm font-medium text-muted-foreground">
                Live status board
              </CardTitle>
            </CardHeader>
            <CardContent className="flex flex-col gap-1 p-2 pt-0">
              {liveRows.map((row) => (
                <div
                  key={row.name}
                  className="flex items-center gap-3 rounded-md px-3 py-2.5 transition-colors hover:bg-muted"
                >
                  <span className="relative flex size-2.5 shrink-0">
                    {row.state === "down" && (
                      <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-rose-400 opacity-60" />
                    )}
                    <span
                      className={cn(
                        "relative inline-flex size-2.5 rounded-full",
                        row.state === "up" ? "bg-emerald-500" : "bg-rose-500",
                      )}
                    />
                  </span>
                  <div className="min-w-0">
                    <p className="truncate text-sm font-medium">{row.name}</p>
                    <p className="truncate font-mono text-xs text-muted-foreground">{row.url}</p>
                  </div>
                  <span className="ml-auto shrink-0 font-mono text-xs tabular-nums text-muted-foreground">
                    {row.detail}
                  </span>
                </div>
              ))}
              <div className="flex items-center justify-between px-3 pt-2">
                <span className="text-xs text-muted-foreground">30-day uptime</span>
                <span className="font-mono text-sm font-semibold tabular-nums">99.98%</span>
              </div>
            </CardContent>
          </Card>
        </section>

        <section data-reveal className="grid gap-4 py-8 sm:grid-cols-2 lg:grid-cols-4">
          {features.map(({ icon: Icon, title, description }) => (
            <Card key={title}>
              <CardHeader>
                <span className="flex size-9 items-center justify-center rounded-md bg-secondary text-secondary-foreground">
                  <Icon data-icon="inline-start" className="size-4" />
                </span>
                <CardTitle className="text-base">{title}</CardTitle>
                <CardDescription>{description}</CardDescription>
              </CardHeader>
            </Card>
          ))}
        </section>

        <section data-reveal className="py-8">
          <h2 className="text-2xl font-semibold tracking-tight">How it works</h2>
          <div className="mt-6 grid gap-4 md:grid-cols-3">
            {steps.map((step, index) => (
              <Card key={step.title}>
                <CardContent className="flex flex-col gap-2 pt-6">
                  <span className="font-mono text-xs text-muted-foreground">
                    0{index + 1}
                  </span>
                  <h3 className="font-semibold">{step.title}</h3>
                  <p className="text-sm text-muted-foreground">{step.description}</p>
                </CardContent>
              </Card>
            ))}
          </div>
        </section>

        <section data-reveal className="py-8">
          <Card>
            <CardContent className="flex flex-col items-start gap-4 p-6 sm:flex-row sm:items-center sm:p-8">
              <div>
                <h2 className="text-xl font-semibold tracking-tight">
                  Your uptime, on your hardware.
                </h2>
                <p className="mt-1 text-sm text-muted-foreground">
                  One compose file. Your data never leaves your server.
                </p>
              </div>
              <div className="flex gap-3 sm:ml-auto">
                {user ? (
                  <Button asChild>
                    <Link to="/dashboard">
                      Open dashboard
                      <ArrowRightIcon data-icon="inline-start" />
                    </Link>
                  </Button>
                ) : (
                  <Button asChild>
                    <Link to="/register">
                      Get started
                      <ArrowRightIcon data-icon="inline-start" />
                    </Link>
                  </Button>
                )}
              </div>
            </CardContent>
          </Card>
        </section>
      </main>

      <PublicFooter />
    </div>
  );
}
