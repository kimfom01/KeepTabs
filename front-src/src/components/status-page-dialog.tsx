import { useEffect, useState } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
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
  FieldLegend,
  FieldSet,
} from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { Spinner } from "@/components/ui/spinner";
import { Switch } from "@/components/ui/switch";
import {
  ApiError,
  monitorsApi,
  statusPagesApi,
  type Monitor,
  type SlugAvailability,
  type StatusPage,
} from "@/lib/api";

interface StatusPageDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  initial: StatusPage | null;
  onSaved: (page: StatusPage) => void;
}

export function StatusPageDialog({ open, onOpenChange, initial, onSaved }: StatusPageDialogProps) {
  const [name, setName] = useState("");
  const [slug, setSlug] = useState("");
  const [isPublic, setIsPublic] = useState(true);
  const [selected, setSelected] = useState<string[]>([]);
  const [monitors, setMonitors] = useState<Monitor[] | null>(null);
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const [checkingSlug, setCheckingSlug] = useState(false);
  const [slugFeedback, setSlugFeedback] = useState<SlugAvailability | null>(null);

  useEffect(() => {
    if (!open) return;
    setFormError(null);
    setName(initial?.name ?? "");
    setSlug(initial?.slug ?? "");
    setIsPublic(initial?.isPublic ?? true);
    setSelected(initial?.monitors.map((m) => m.monitorId) ?? []);
    setSlugFeedback(null);
    monitorsApi
      .list()
      .then(setMonitors)
      .catch((error: unknown) =>
        toast.error(error instanceof ApiError ? error.message : "Could not load monitors."),
      );
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, initial?.statusPageId]);

  useEffect(() => {
    if (!open || !slug.trim()) {
      setSlugFeedback(null);
      setCheckingSlug(false);
      return;
    }
    setCheckingSlug(true);
    const timer = setTimeout(() => {
      statusPagesApi
        .checkSlug(slug.trim(), name.trim() || undefined, initial?.statusPageId)
        .then((result) => {
          setSlugFeedback(result);
          setCheckingSlug(false);
        })
        .catch(() => setCheckingSlug(false));
    }, 400);

    return () => clearTimeout(timer);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [slug, name, open, initial?.statusPageId]);

  function toggleMonitor(monitorId: string, checked: boolean | "indeterminate") {
    setSelected((current) =>
      checked === true
        ? [...current, monitorId]
        : current.filter((id) => id !== monitorId),
    );
  }

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    setFormError(null);

    if (!name.trim()) {
      setFormError("Name is required.");
      return;
    }

    setSaving(true);
    try {
      const saved = initial
        ? await statusPagesApi.update(initial.statusPageId, {
            name: name.trim(),
            slug: slug.trim(),
            isPublic,
            monitorIds: selected,
          })
        : await statusPagesApi.create({
            name: name.trim(),
            slug: slug.trim(),
            isPublic,
            monitorIds: selected,
          });
      toast.success(initial ? "Status page updated." : "Status page created.");
      onSaved(saved);
      onOpenChange(false);
    } catch (error) {
      toast.error(error instanceof ApiError ? error.message : "Could not save the status page.");
    } finally {
      setSaving(false);
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{initial ? "Edit status page" : "New status page"}</DialogTitle>
          <DialogDescription>
            Pick the monitors to show. Public pages are visible to anyone with the link.
          </DialogDescription>
        </DialogHeader>
        <form onSubmit={handleSubmit}>
          <FieldGroup>
            <Field>
              <FieldLabel htmlFor="page-name">Name</FieldLabel>
              <Input
                id="page-name"
                value={name}
                onChange={(event) => setName(event.target.value)}
                placeholder="Acme status"
                autoComplete="off"
              />
            </Field>
            <Field>
              <FieldLabel htmlFor="page-slug">Slug</FieldLabel>
              <Input
                id="page-slug"
                value={slug}
                onChange={(event) => setSlug(event.target.value)}
                placeholder="Auto-generated from name"
                autoComplete="off"
                className="font-mono"
              />
              <FieldDescription>
                {slug.trim() === "" ? (
                  "Leave blank to generate from the name."
                ) : checkingSlug ? (
                  "Checking availability…"
                ) : slugFeedback ? (
                  slugFeedback.available ? (
                    <span className="text-emerald-700 dark:text-emerald-400">
                      “{slugFeedback.slug}” is available.
                    </span>
                  ) : (
                    <span>
                      <span className="text-rose-700 dark:text-rose-400">
                        “{slugFeedback.slug}” is taken.{" "}
                      </span>
                      <button
                        type="button"
                        className="font-medium underline underline-offset-4"
                        onClick={() => setSlug(slugFeedback.suggestion)}
                      >
                        Use “{slugFeedback.suggestion}”
                      </button>
                    </span>
                  )
                ) : (
                  "Lowercase letters, numbers, hyphens."
                )}
              </FieldDescription>
            </Field>
            <Field orientation="horizontal">
              <Switch id="page-public" checked={isPublic} onCheckedChange={setIsPublic} />
              <FieldLabel htmlFor="page-public">Public</FieldLabel>
            </Field>
            <FieldSet>
              <FieldLegend>Monitors</FieldLegend>
              <FieldDescription>Shown in the order selected.</FieldDescription>
              {monitors === null ? (
                <FieldDescription>Loading monitors…</FieldDescription>
              ) : monitors.length === 0 ? (
                <FieldDescription>
                  No monitors yet. Create one first, then add it here.
                </FieldDescription>
              ) : (
                <div className="flex max-h-48 flex-col gap-1 overflow-y-auto">
                  {monitors.map((monitor) => (
                    <Field key={monitor.monitorId} orientation="horizontal">
                      <Checkbox
                        id={`page-monitor-${monitor.monitorId}`}
                        checked={selected.includes(monitor.monitorId)}
                        onCheckedChange={(checked) => toggleMonitor(monitor.monitorId, checked)}
                      />
                      <FieldContent>
                        <FieldLabel htmlFor={`page-monitor-${monitor.monitorId}`}>
                          {monitor.name}
                        </FieldLabel>
                        <FieldDescription className="font-mono">{monitor.url}</FieldDescription>
                      </FieldContent>
                    </Field>
                  ))}
                </div>
              )}
            </FieldSet>
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
              {initial ? "Save changes" : "Create page"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
