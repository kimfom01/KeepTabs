import { ArrowUpRightIcon } from "lucide-react";
import { PublicFooter } from "@/components/public-footer";
import { PublicHeader } from "@/components/public-header";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";

function CodeBlock({ code }: { code: string }) {
  return (
    <pre className="overflow-x-auto rounded-md bg-muted p-4 font-mono text-xs leading-relaxed">
      {code}
    </pre>
  );
}

const webhookPayload = `POST https://your-server.example.com/keeptabs
Content-Type: application/json

{
  "monitorId": "01a0788a-9fa7-7686-a14d-e3a7e08079e8",
  "monitorName": "Marketing site",
  "monitorUrl": "https://example.com",
  "isUp": false,
  "message": "Monitor 'Marketing site' is DOWN (https://example.com).",
  "firedAt": "2026-09-07T12:00:00+00:00"
}`;

const curlApiKey = `curl https://keeptabs.example.com/api/monitors \\
  -H "X-Api-Key: YOUR_API_KEY_HERE"`;

const curlCreate = `curl -X POST https://keeptabs.example.com/api/monitors \\
  -H "X-Api-Key: YOUR_API_KEY_HERE" \\
  -H "Content-Type: application/json" \\
  -d '{
    "name": "Marketing site",
    "url": "https://example.com",
    "protocol": "Http",
    "checkIntervalSeconds": 60,
    "timeoutSeconds": 10,
    "expectedStatusCode": 200
  }'`;

const payloadFields: [string, string][] = [
  ["monitorId", "ID of the monitor that changed state."],
  ["monitorName", "Display name of the monitor."],
  ["monitorUrl", "Target being watched."],
  ["isUp", "New state: true after recovery, false when down."],
  ["message", "Human-readable summary, e.g. why it fired."],
  ["firedAt", "UTC timestamp of the delivery."],
];

export function DocsPage() {
  return (
    <div className="min-h-screen bg-background">
      <PublicHeader />
      <main className="mx-auto max-w-6xl px-4 py-8 sm:px-6">
        <h1 className="text-2xl font-semibold tracking-tight">Docs</h1>
        <p className="text-sm text-muted-foreground">
          Integrate webhooks, call the API, and find the interactive reference.
        </p>

        <Card className="mt-6 max-w-3xl">
          <CardHeader>
            <CardTitle>Receiving webhooks</CardTitle>
            <CardDescription>
              Create an alert rule of type Webhook and KeepTabs POSTs a JSON document to
              your URL every time the rule fires.
            </CardDescription>
          </CardHeader>
          <CardContent className="flex flex-col gap-4">
            <div>
              <h3 className="mb-2 text-sm font-semibold">Request format</h3>
              <p className="mb-2 text-sm text-muted-foreground">
                Deliveries time out after 15 seconds. Any 2xx response counts as delivered;
                anything else — connection errors, timeouts, non-2xx statuses — is recorded
                as a failed delivery with the reason.
              </p>
              <CodeBlock code={webhookPayload} />
            </div>
            <div>
              <h3 className="mb-2 text-sm font-semibold">Payload fields</h3>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Field</TableHead>
                    <TableHead>Meaning</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {payloadFields.map(([field, meaning]) => (
                    <TableRow key={field}>
                      <TableCell className="font-mono text-xs">{field}</TableCell>
                      <TableCell className="text-muted-foreground">{meaning}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
            <div>
              <h3 className="mb-2 text-sm font-semibold">Testing and troubleshooting</h3>
              <ul className="flex list-disc flex-col gap-1 pl-5 text-sm text-muted-foreground">
                <li>Use the test action on any rule to send a delivery on demand.</li>
                <li>Every attempt — test or real — appears on the Alerts page with its outcome.</li>
                <li>Respond 2xx quickly; slow endpoints trip the 15-second timeout.</li>
                <li>Your endpoint must be reachable from wherever the worker runs.</li>
              </ul>
            </div>
          </CardContent>
        </Card>

        <Card className="mt-6 max-w-3xl">
          <CardHeader>
            <CardTitle>Calling the API with an API key</CardTitle>
            <CardDescription>
              API keys carry the same access as a signed-in session, for scripts and
              integrations.
            </CardDescription>
          </CardHeader>
          <CardContent className="flex flex-col gap-4">
            <ol className="flex list-decimal flex-col gap-1 pl-5 text-sm text-muted-foreground">
              <li>Create a key on the API keys page (or POST /api/auth/api-key/regenerate).</li>
              <li>Copy it immediately — only a hash is stored, it cannot be shown again.</li>
              <li>Send it in the <span className="font-mono">X-Api-Key</span> header.</li>
            </ol>
            <div>
              <h3 className="mb-2 text-sm font-semibold">List monitors</h3>
              <CodeBlock code={curlApiKey} />
            </div>
            <div>
              <h3 className="mb-2 text-sm font-semibold">Create a monitor</h3>
              <CodeBlock code={curlCreate} />
            </div>
            <p className="text-sm text-muted-foreground">
              Regenerating replaces the current key immediately. Prefer short-lived JWT
              sessions (`Authorization: Bearer`) for browser use.
            </p>
          </CardContent>
        </Card>

        <Card className="mt-6 max-w-3xl">
          <CardHeader>
            <CardTitle>Interactive API reference</CardTitle>
            <CardDescription>
              Full endpoint list with schemas, generated from the running API.
            </CardDescription>
          </CardHeader>
          <CardContent className="flex flex-wrap items-center gap-3">
            <Button asChild>
              <a href="swagger" target="_blank" rel="noreferrer">
                Open Swagger UI
                <ArrowUpRightIcon data-icon="inline-start" />
              </a>
            </Button>
            <Button variant="outline" asChild>
              <a href="openapi/v1.json" target="_blank" rel="noreferrer">
                OpenAPI JSON
                <ArrowUpRightIcon data-icon="inline-start" />
              </a>
            </Button>
          </CardContent>
        </Card>
      </main>
      <PublicFooter />
    </div>
  );
}
