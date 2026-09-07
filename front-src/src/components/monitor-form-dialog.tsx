import { useEffect, useState } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  Field,
  FieldContent,
  FieldDescription,
  FieldError,
  FieldGroup,
  FieldLabel,
} from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectGroup,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Spinner } from "@/components/ui/spinner";
import { Switch } from "@/components/ui/switch";
import { ApiError, monitorsApi, type Monitor, type ProtocolType } from "@/lib/api";

interface MonitorFormDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  initial: Monitor | null;
  onSaved: (monitor: Monitor) => void;
}

const protocols: { value: ProtocolType; label: string; hint: string }[] = [
  { value: "Http", label: "HTTP", hint: "https://example.com" },
  { value: "Tcp", label: "TCP", hint: "example.com:443" },
  { value: "Ping", label: "Ping", hint: "example.com" },
];

export function MonitorFormDialog({ open, onOpenChange, initial, onSaved }: MonitorFormDialogProps) {
  const [name, setName] = useState("");
  const [url, setUrl] = useState("");
  const [protocol, setProtocol] = useState<ProtocolType>("Http");
  const [interval, setInterval] = useState("60");
  const [timeout, setTimeout] = useState("10");
  const [expectedStatus, setExpectedStatus] = useState("200");
  const [useHead, setUseHead] = useState(false);
  const [paused, setPaused] = useState(false);
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  useEffect(() => {
    if (!open) return;
    setFormError(null);
    setName(initial?.name ?? "");
    setUrl(initial?.url ?? "");
    setProtocol(initial?.protocol ?? "Http");
    setInterval(String(initial?.checkIntervalSeconds ?? 60));
    setTimeout(String(initial?.timeoutSeconds ?? 10));
    setExpectedStatus(String(initial?.expectedStatusCode ?? 200));
    setUseHead(initial?.useHeadRequest ?? false);
    setPaused(initial?.isPaused ?? false);
  }, [open, initial]);

  const protocolHint = protocols.find((p) => p.value === protocol)?.hint;

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    setFormError(null);

    const intervalSeconds = Number(interval);
    const timeoutSeconds = Number(timeout);
    if (!name.trim() || !url.trim()) {
      setFormError("Name and target are required.");
      return;
    }
    if (!Number.isFinite(intervalSeconds) || intervalSeconds <= 0) {
      setFormError("Check interval must be a positive number of seconds.");
      return;
    }
    if (!Number.isFinite(timeoutSeconds) || timeoutSeconds <= 0) {
      setFormError("Timeout must be a positive number of seconds.");
      return;
    }
    if (timeoutSeconds >= intervalSeconds) {
      setFormError("Timeout must be shorter than the check interval.");
      return;
    }

    setSaving(true);
    try {
      const saved = initial
        ? await monitorsApi.update(initial.monitorId, {
            name: name.trim(),
            url: url.trim(),
            protocol,
            checkIntervalSeconds: intervalSeconds,
            timeoutSeconds,
            expectedStatusCode: protocol === "Http" ? Number(expectedStatus) || null : null,
            isPaused: paused,
            useHeadRequest: protocol === "Http" ? useHead : false,
          })
        : await monitorsApi.create({
            name: name.trim(),
            url: url.trim(),
            protocol,
            checkIntervalSeconds: intervalSeconds,
            timeoutSeconds,
            expectedStatusCode: protocol === "Http" ? Number(expectedStatus) || null : null,
            useHeadRequest: protocol === "Http" ? useHead : false,
          });
      toast.success(initial ? "Monitor updated." : "Monitor created.");
      onSaved(saved);
      onOpenChange(false);
    } catch (error) {
      toast.error(error instanceof ApiError ? error.message : "Could not save the monitor.");
    } finally {
      setSaving(false);
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{initial ? "Edit monitor" : "New monitor"}</DialogTitle>
          <DialogDescription>
            {initial
              ? "Update how this target is watched."
              : "Add a target to watch. Checks start automatically."}
          </DialogDescription>
        </DialogHeader>
        <form onSubmit={handleSubmit}>
          <FieldGroup>
            <Field>
              <FieldLabel htmlFor="monitor-name">Name</FieldLabel>
              <Input
                id="monitor-name"
                value={name}
                onChange={(event) => setName(event.target.value)}
                placeholder="Marketing site"
                autoComplete="off"
              />
            </Field>
            <div className="flex flex-col gap-4 sm:flex-row">
              <Field className="flex-1">
                <FieldLabel htmlFor="monitor-protocol">Protocol</FieldLabel>
                <Select value={protocol} onValueChange={(value) => setProtocol(value as ProtocolType)}>
                  <SelectTrigger id="monitor-protocol">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectGroup>
                      {protocols.map((p) => (
                        <SelectItem key={p.value} value={p.value}>
                          {p.label}
                        </SelectItem>
                      ))}
                    </SelectGroup>
                  </SelectContent>
                </Select>
              </Field>
              <Field className="flex-[2]">
                <FieldLabel htmlFor="monitor-url">Target</FieldLabel>
                <Input
                  id="monitor-url"
                  value={url}
                  onChange={(event) => setUrl(event.target.value)}
                  placeholder={protocolHint}
                  autoComplete="off"
                  className="font-mono"
                />
              </Field>
            </div>
            <div className="flex flex-col gap-4 sm:flex-row">
              <Field>
                <FieldLabel htmlFor="monitor-interval">Interval (s)</FieldLabel>
                <Input
                  id="monitor-interval"
                  inputMode="numeric"
                  value={interval}
                  onChange={(event) => setInterval(event.target.value)}
                />
              </Field>
              <Field>
                <FieldLabel htmlFor="monitor-timeout">Timeout (s)</FieldLabel>
                <Input
                  id="monitor-timeout"
                  inputMode="numeric"
                  value={timeout}
                  onChange={(event) => setTimeout(event.target.value)}
                />
              </Field>
              {protocol === "Http" && (
                <Field>
                  <FieldLabel htmlFor="monitor-status">Expected status</FieldLabel>
                  <Input
                    id="monitor-status"
                    inputMode="numeric"
                    value={expectedStatus}
                    onChange={(event) => setExpectedStatus(event.target.value)}
                  />
                </Field>
              )}
            </div>
            {protocol === "Http" && (
              <Field orientation="horizontal">
                <Switch id="monitor-head" checked={useHead} onCheckedChange={setUseHead} />
                <FieldContent>
                  <FieldLabel htmlFor="monitor-head">Use HEAD requests</FieldLabel>
                  <FieldDescription>Lighter checks that skip the response body.</FieldDescription>
                </FieldContent>
              </Field>
            )}
            {initial && (
              <Field orientation="horizontal">
                <Switch id="monitor-paused" checked={paused} onCheckedChange={setPaused} />
                <FieldLabel htmlFor="monitor-paused">Paused</FieldLabel>
              </Field>
            )}
            {formError && (
              <Field data-invalid>
                <FieldError>{formError}</FieldError>
              </Field>
            )}
            <FieldDescription>
              Checks run every interval. A check fails when the target is unreachable,
              too slow, or returns an unexpected status.
            </FieldDescription>
          </FieldGroup>
          <DialogFooter className="mt-4">
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
              Cancel
            </Button>
            <Button type="submit" disabled={saving}>
              {saving && <Spinner data-icon="inline-start" />}
              {initial ? "Save changes" : "Create monitor"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
