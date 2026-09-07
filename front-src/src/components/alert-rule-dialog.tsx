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
import {
  ApiError,
  alertsApi,
  type AlertRule,
  type AlertTriggerType,
  type AlertType,
} from "@/lib/api";

interface AlertRuleDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  monitorId: string;
  initial: AlertRule | null;
  onSaved: (rule: AlertRule) => void;
}

const triggers: { value: AlertTriggerType; label: string; hint: string }[] = [
  { value: "OnDown", label: "When it goes down", hint: "Fires on every up-to-down transition." },
  { value: "OnUp", label: "When it recovers", hint: "Fires on every down-to-up transition." },
  {
    value: "ConsecutiveFailures",
    label: "After repeated failures",
    hint: "Fires once the threshold of back-to-back failures is reached.",
  },
];

export function AlertRuleDialog({ open, onOpenChange, monitorId, initial, onSaved }: AlertRuleDialogProps) {
  const [type, setType] = useState<AlertType>("Email");
  const [trigger, setTrigger] = useState<AlertTriggerType>("OnDown");
  const [threshold, setThreshold] = useState("3");
  const [cooldown, setCooldown] = useState("30");
  const [target, setTarget] = useState("");
  const [enabled, setEnabled] = useState(true);
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  useEffect(() => {
    if (!open) return;
    setFormError(null);
    setType(initial?.type ?? "Email");
    setTrigger(initial?.triggerType ?? "OnDown");
    setThreshold(String(initial?.threshold ?? 3));
    setCooldown(String(initial?.coolDownMinutes ?? 30));
    setTarget(initial?.target ?? "");
    setEnabled(initial?.isEnabled ?? true);
  }, [open, initial]);

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    setFormError(null);

    if (!target.trim()) {
      setFormError(type === "Email" ? "An email address is required." : "A webhook URL is required.");
      return;
    }
    const thresholdValue = Number(threshold);
    const cooldownValue = Number(cooldown);
    if (!Number.isInteger(thresholdValue) || thresholdValue < 1 || thresholdValue > 100) {
      setFormError("Threshold must be a whole number between 1 and 100.");
      return;
    }
    if (!Number.isInteger(cooldownValue) || cooldownValue < 0 || cooldownValue > 1440) {
      setFormError("Cooldown must be a whole number of minutes between 0 and 1440.");
      return;
    }

    setSaving(true);
    try {
      const saved = initial
        ? await alertsApi.update(initial.alertRuleId, {
            type,
            triggerType: trigger,
            threshold: thresholdValue,
            coolDownMinutes: cooldownValue,
            target: target.trim(),
            isEnabled: enabled,
          })
        : await alertsApi.create({
            monitorId,
            type,
            triggerType: trigger,
            threshold: thresholdValue,
            coolDownMinutes: cooldownValue,
            target: target.trim(),
            isEnabled: enabled,
          });
      toast.success(initial ? "Alert rule updated." : "Alert rule created.");
      onSaved(saved);
      onOpenChange(false);
    } catch (error) {
      toast.error(error instanceof ApiError ? error.message : "Could not save the alert rule.");
    } finally {
      setSaving(false);
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{initial ? "Edit alert rule" : "New alert rule"}</DialogTitle>
          <DialogDescription>Get notified when this monitor changes state.</DialogDescription>
        </DialogHeader>
        <form onSubmit={handleSubmit}>
          <FieldGroup>
            <div className="flex gap-4">
              <Field className="flex-1">
                <FieldLabel htmlFor="alert-type">Channel</FieldLabel>
                <Select value={type} onValueChange={(value) => setType(value as AlertType)}>
                  <SelectTrigger id="alert-type">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectGroup>
                      <SelectItem value="Email">Email</SelectItem>
                      <SelectItem value="Webhook">Webhook</SelectItem>
                      <SelectItem value="Telegram">Telegram</SelectItem>
                    </SelectGroup>
                  </SelectContent>
                </Select>
              </Field>
              <Field className="flex-[2]">
                <FieldLabel htmlFor="alert-target">Destination</FieldLabel>
                <Input
                  id="alert-target"
                  value={target}
                  onChange={(event) => setTarget(event.target.value)}
                  placeholder={
                    type === "Email"
                      ? "ops@example.com"
                      : type === "Webhook"
                        ? "https://hooks.example.com/..."
                        : "-1001234567890"
                  }
                  autoComplete="off"
                  className="font-mono"
                />
              </Field>
            </div>
            <Field>
              <FieldLabel htmlFor="alert-trigger">Trigger</FieldLabel>
              <Select value={trigger} onValueChange={(value) => setTrigger(value as AlertTriggerType)}>
                <SelectTrigger id="alert-trigger">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectGroup>
                    {triggers.map((t) => (
                      <SelectItem key={t.value} value={t.value}>
                        {t.label}
                      </SelectItem>
                    ))}
                  </SelectGroup>
                </SelectContent>
              </Select>
              <FieldDescription>
                {triggers.find((t) => t.value === trigger)?.hint}
              </FieldDescription>
            </Field>
            <div className="flex gap-4">
              <Field>
                <FieldLabel htmlFor="alert-threshold">Failures to trigger</FieldLabel>
                <Input
                  id="alert-threshold"
                  inputMode="numeric"
                  value={threshold}
                  onChange={(event) => setThreshold(event.target.value)}
                />
              </Field>
              <Field>
                <FieldLabel htmlFor="alert-cooldown">Cooldown (min)</FieldLabel>
                <Input
                  id="alert-cooldown"
                  inputMode="numeric"
                  value={cooldown}
                  onChange={(event) => setCooldown(event.target.value)}
                />
              </Field>
            </div>
            {initial && (
              <Field orientation="horizontal">
                <Switch id="alert-enabled" checked={enabled} onCheckedChange={setEnabled} />
                <FieldLabel htmlFor="alert-enabled">Enabled</FieldLabel>
              </Field>
            )}
            {formError && (
              <Field data-invalid>
                <FieldError>{formError}</FieldError>
              </Field>
            )}
          </FieldGroup>
          <DialogFooter className="mt-4">
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
              Cancel
            </Button>
            <Button type="submit" disabled={saving}>
              {saving && <Spinner data-icon="inline-start" />}
              {initial ? "Save changes" : "Create rule"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
