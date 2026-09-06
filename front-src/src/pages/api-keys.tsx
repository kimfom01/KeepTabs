import { useState } from "react";
import { CheckIcon, CopyIcon, KeyRoundIcon, RefreshCwIcon } from "lucide-react";
import { toast } from "sonner";
import { AppHeader } from "@/components/app-header";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Field, FieldDescription } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { Spinner } from "@/components/ui/spinner";
import { ApiError, authApi } from "@/lib/api";

export function ApiKeysPage() {
  const [freshKey, setFreshKey] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [copied, setCopied] = useState(false);

  async function handleRegenerate() {
    setBusy(true);
    try {
      const response = await authApi.regenerateApiKey();
      setFreshKey(response.apiKey);
      setCopied(false);
      toast.success("New API key created.");
    } catch (error) {
      toast.error(error instanceof ApiError ? error.message : "Could not create an API key.");
    } finally {
      setBusy(false);
    }
  }

  async function handleCopy() {
    if (!freshKey) return;
    try {
      await navigator.clipboard.writeText(freshKey);
      setCopied(true);
    } catch {
      toast.error("Could not copy to the clipboard.");
    }
  }

  return (
    <div className="min-h-screen bg-background">
      <AppHeader />
      <main className="mx-auto max-w-6xl px-4 py-8 sm:px-6">
        <h1 className="text-2xl font-semibold tracking-tight">API keys</h1>
        <p className="text-sm text-muted-foreground">
          Use a key in the <span className="font-mono">X-Api-Key</span> header to call the API
          without a browser session.
        </p>

        <Card className="mt-6 max-w-2xl">
          <CardHeader>
            <CardTitle>Personal key</CardTitle>
            <CardDescription>
              Regenerating replaces your current key immediately. Only the hash is stored, so a
              lost key cannot be recovered.
            </CardDescription>
          </CardHeader>
          <CardContent>
            {freshKey ? (
              <Alert>
                <KeyRoundIcon />
                <AlertTitle>Copy this key now — it will not be shown again.</AlertTitle>
                <AlertDescription className="mt-2">
                  <Field orientation="horizontal">
                    <Input readOnly value={freshKey} className="font-mono" />
                    <Button type="button" variant="outline" onClick={handleCopy}>
                      {copied ? (
                        <>
                          <CheckIcon data-icon="inline-start" />
                          Copied
                        </>
                      ) : (
                        <>
                          <CopyIcon data-icon="inline-start" />
                          Copy
                        </>
                      )}
                    </Button>
                  </Field>
                </AlertDescription>
              </Alert>
            ) : (
              <Field>
                <FieldDescription>
                  No key is currently visible. Generate one to get started.
                </FieldDescription>
                <div>
                  <Button onClick={handleRegenerate} disabled={busy}>
                    {busy ? (
                      <Spinner data-icon="inline-start" />
                    ) : (
                      <RefreshCwIcon data-icon="inline-start" />
                    )}
                    Generate new key
                  </Button>
                </div>
              </Field>
            )}
            {freshKey && (
              <div className="mt-4">
                <Button variant="outline" onClick={handleRegenerate} disabled={busy}>
                  {busy ? (
                    <Spinner data-icon="inline-start" />
                  ) : (
                    <RefreshCwIcon data-icon="inline-start" />
                  )}
                  Regenerate again
                </Button>
              </div>
            )}
          </CardContent>
        </Card>
      </main>
    </div>
  );
}
