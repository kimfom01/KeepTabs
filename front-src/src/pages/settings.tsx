import { useCallback, useEffect, useState } from "react";
import { toast } from "sonner";
import { AppHeader } from "@/components/app-header";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Field, FieldDescription, FieldError, FieldGroup, FieldLabel } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { Spinner } from "@/components/ui/spinner";
import { Switch } from "@/components/ui/switch";
import { ApiError, settingsApi } from "@/lib/api";

export function SettingsPage() {
  const [smtpEnabled, setSmtpEnabled] = useState(false);
  const [host, setHost] = useState("");
  const [port, setPort] = useState("587");
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [passwordSet, setPasswordSet] = useState(false);
  const [from, setFrom] = useState("");
  const [enableSsl, setEnableSsl] = useState(true);
  const [telegramEnabled, setTelegramEnabled] = useState(false);
  const [botToken, setBotToken] = useState("");
  const [tokenSet, setTokenSet] = useState(false);
  const [loading, setLoading] = useState(true);
  const [savingSmtp, setSavingSmtp] = useState(false);
  const [savingTelegram, setSavingTelegram] = useState(false);
  const [smtpError, setSmtpError] = useState<string | null>(null);
  const [telegramError, setTelegramError] = useState<string | null>(null);

  const load = useCallback(async () => {
    try {
      const [smtp, telegram] = await Promise.all([
        settingsApi.getSmtp(),
        settingsApi.getTelegram(),
      ]);
      setSmtpEnabled(smtp.enabled);
      setHost(smtp.host);
      setPort(String(smtp.port));
      setUsername(smtp.username);
      setPassword("");
      setPasswordSet(smtp.passwordSet);
      setFrom(smtp.from);
      setEnableSsl(smtp.enableSsl);
      setTelegramEnabled(telegram.enabled);
      setBotToken("");
      setTokenSet(telegram.tokenSet);
    } catch (error) {
      toast.error(error instanceof ApiError ? error.message : "Could not load settings.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  async function handleSmtpSubmit(event: React.FormEvent) {
    event.preventDefault();
    setSmtpError(null);

    const portValue = Number(port);
    if (smtpEnabled && !host.trim()) {
      setSmtpError("SMTP host is required.");
      return;
    }
    if (!Number.isInteger(portValue) || portValue < 1 || portValue > 65535) {
      setSmtpError("Port must be a whole number between 1 and 65535.");
      return;
    }

    setSavingSmtp(true);
    try {
      const updated = await settingsApi.updateSmtp({
        enabled: smtpEnabled,
        host: host.trim(),
        port: portValue,
        username: username.trim(),
        password,
        from: from.trim(),
        enableSsl,
      });
      setPassword("");
      setPasswordSet(updated.passwordSet);
      toast.success("SMTP settings saved.");
    } catch (error) {
      const message = error instanceof ApiError ? error.message : "Could not save settings.";
      setSmtpError(message);
      toast.error(message);
    } finally {
      setSavingSmtp(false);
    }
  }

  async function handleTelegramSubmit(event: React.FormEvent) {
    event.preventDefault();
    setTelegramError(null);

    setSavingTelegram(true);
    try {
      const updated = await settingsApi.updateTelegram({
        enabled: telegramEnabled,
        botToken: botToken.trim(),
      });
      setBotToken("");
      setTokenSet(updated.tokenSet);
      toast.success("Telegram settings saved.");
    } catch (error) {
      const message = error instanceof ApiError ? error.message : "Could not save settings.";
      setTelegramError(message);
      toast.error(message);
    } finally {
      setSavingTelegram(false);
    }
  }

  return (
    <div className="min-h-screen bg-background">
      <AppHeader />
      <main className="mx-auto max-w-6xl px-4 py-8 sm:px-6">
        <h1 className="text-2xl font-semibold tracking-tight">Settings</h1>
        <p className="text-sm text-muted-foreground">
          Delivery channels and notification configuration.
        </p>

        <Card className="mt-6 max-w-2xl">
          <CardHeader>
            <CardTitle>Email (SMTP)</CardTitle>
            <CardDescription>
              Used for email alert rules. Disabled until you turn it on and save a host.
            </CardDescription>
          </CardHeader>
          <CardContent>
            {loading ? (
              <div className="flex flex-col gap-3">
                <Skeleton className="h-10 w-full" />
                <Skeleton className="h-10 w-full" />
                <Skeleton className="h-10 w-full" />
              </div>
            ) : (
              <form onSubmit={handleSmtpSubmit}>
                <FieldGroup>
                  <Field orientation="horizontal">
                    <Switch id="smtp-enabled" checked={smtpEnabled} onCheckedChange={setSmtpEnabled} />
                    <FieldLabel htmlFor="smtp-enabled">Enabled</FieldLabel>
                  </Field>
                  <div className="flex gap-4">
                    <Field className="flex-[2]">
                      <FieldLabel htmlFor="smtp-host">Host</FieldLabel>
                      <Input
                        id="smtp-host"
                        value={host}
                        onChange={(event) => setHost(event.target.value)}
                        placeholder="smtp.example.com"
                        autoComplete="off"
                        className="font-mono"
                      />
                    </Field>
                    <Field className="flex-1">
                      <FieldLabel htmlFor="smtp-port">Port</FieldLabel>
                      <Input
                        id="smtp-port"
                        inputMode="numeric"
                        value={port}
                        onChange={(event) => setPort(event.target.value)}
                      />
                    </Field>
                  </div>
                  <div className="flex gap-4">
                    <Field>
                      <FieldLabel htmlFor="smtp-username">Username</FieldLabel>
                      <Input
                        id="smtp-username"
                        value={username}
                        onChange={(event) => setUsername(event.target.value)}
                        autoComplete="off"
                      />
                    </Field>
                    <Field>
                      <FieldLabel htmlFor="smtp-password">Password</FieldLabel>
                      <Input
                        id="smtp-password"
                        type="password"
                        value={password}
                        onChange={(event) => setPassword(event.target.value)}
                        placeholder={passwordSet ? "Saved — leave blank to keep" : "Not set"}
                        autoComplete="new-password"
                      />
                    </Field>
                  </div>
                  <Field>
                    <FieldLabel htmlFor="smtp-from">Sender address</FieldLabel>
                    <Input
                      id="smtp-from"
                      type="email"
                      value={from}
                      onChange={(event) => setFrom(event.target.value)}
                      placeholder="alerts@example.com"
                      autoComplete="off"
                    />
                  </Field>
                  <Field orientation="horizontal">
                    <Switch id="smtp-ssl" checked={enableSsl} onCheckedChange={setEnableSsl} />
                    <FieldLabel htmlFor="smtp-ssl">Use TLS</FieldLabel>
                  </Field>
                  {smtpError && (
                    <Field data-invalid>
                      <FieldError>{smtpError}</FieldError>
                    </Field>
                  )}
                  <Field>
                    <div>
                      <Button type="submit" disabled={savingSmtp}>
                        {savingSmtp && <Spinner data-icon="inline-start" />}
                        Save changes
                      </Button>
                    </div>
                    <FieldDescription>
                      Test delivery from any alert rule to verify these settings.
                    </FieldDescription>
                  </Field>
                </FieldGroup>
              </form>
            )}
          </CardContent>
        </Card>

        <Card className="mt-6 max-w-2xl">
          <CardHeader>
            <CardTitle>Webhooks</CardTitle>
            <CardDescription>No global setup needed.</CardDescription>
          </CardHeader>
          <CardContent>
            <p className="text-sm text-muted-foreground">
              Webhook destinations are configured per alert rule. Each delivery POSTs check
              details as JSON to the rule's URL.
            </p>
          </CardContent>
        </Card>

        <Card className="mt-6 max-w-2xl">
          <CardHeader>
            <CardTitle>Telegram</CardTitle>
            <CardDescription>
              Create a bot with @BotFather, add it to your notifications group, then paste
              the token here. Each alert rule targets a group chat ID (e.g. -1001234567890).
            </CardDescription>
          </CardHeader>
          <CardContent>
            {loading ? (
              <div className="flex flex-col gap-3">
                <Skeleton className="h-10 w-full" />
                <Skeleton className="h-10 w-full" />
              </div>
            ) : (
              <form onSubmit={handleTelegramSubmit}>
                <FieldGroup>
                  <Field orientation="horizontal">
                    <Switch
                      id="telegram-enabled"
                      checked={telegramEnabled}
                      onCheckedChange={setTelegramEnabled}
                    />
                    <FieldLabel htmlFor="telegram-enabled">Enabled</FieldLabel>
                  </Field>
                  <Field>
                    <FieldLabel htmlFor="telegram-token">Bot token</FieldLabel>
                    <Input
                      id="telegram-token"
                      type="password"
                      value={botToken}
                      onChange={(event) => setBotToken(event.target.value)}
                      placeholder={tokenSet ? "Saved — leave blank to keep" : "123456:ABC-DEF..."}
                      autoComplete="off"
                      className="font-mono"
                    />
                    <FieldDescription>
                      Find your group chat ID by adding @getmyid_bot to the group.
                    </FieldDescription>
                  </Field>
                  {telegramError && (
                    <Field data-invalid>
                      <FieldError>{telegramError}</FieldError>
                    </Field>
                  )}
                  <Field>
                    <div>
                      <Button type="submit" disabled={savingTelegram}>
                        {savingTelegram && <Spinner data-icon="inline-start" />}
                        Save changes
                      </Button>
                    </div>
                    <FieldDescription>
                      Send a test from any Telegram alert rule to verify delivery.
                    </FieldDescription>
                  </Field>
                </FieldGroup>
              </form>
            )}
          </CardContent>
        </Card>
      </main>
    </div>
  );
}
