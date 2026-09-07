import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router";
import { ArrowUpRightIcon, PencilIcon, PlusIcon, Trash2Icon } from "lucide-react";
import { toast } from "sonner";
import { AppHeader } from "@/components/app-header";
import { StatusPageDialog } from "@/components/status-page-dialog";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import {
  Empty,
  EmptyContent,
  EmptyDescription,
  EmptyHeader,
  EmptyMedia,
  EmptyTitle,
} from "@/components/ui/empty";
import { Skeleton } from "@/components/ui/skeleton";
import { Switch } from "@/components/ui/switch";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { ApiError, statusPagesApi, type StatusPage } from "@/lib/api";

export function StatusPagesPage() {
  const [pages, setPages] = useState<StatusPage[] | null>(null);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editing, setEditing] = useState<StatusPage | null>(null);
  const [deleting, setDeleting] = useState<StatusPage | null>(null);

  const load = useCallback(async () => {
    try {
      setPages(await statusPagesApi.list());
    } catch (error) {
      toast.error(error instanceof ApiError ? error.message : "Could not load status pages.");
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  async function togglePublic(page: StatusPage) {
    try {
      const updated = await statusPagesApi.update(page.statusPageId, {
        isPublic: !page.isPublic,
      });
      setPages((current) =>
        current?.map((p) => (p.statusPageId === updated.statusPageId ? updated : p)) ?? null,
      );
      toast.success(updated.isPublic ? "Page is now public." : "Page is now private.");
    } catch (error) {
      toast.error(error instanceof ApiError ? error.message : "Could not update the page.");
    }
  }

  async function confirmDelete() {
    if (!deleting) return;
    try {
      await statusPagesApi.remove(deleting.statusPageId);
      setPages((current) => current?.filter((p) => p.statusPageId !== deleting.statusPageId) ?? null);
      toast.success("Status page deleted.");
    } catch (error) {
      toast.error(error instanceof ApiError ? error.message : "Could not delete the page.");
    } finally {
      setDeleting(null);
    }
  }

  return (
    <div className="min-h-screen bg-background">
      <AppHeader />
      <main className="mx-auto max-w-6xl px-4 py-8 sm:px-6">
        <div className="flex items-center justify-between gap-4">
          <div>
            <h1 className="text-2xl font-semibold tracking-tight">Status pages</h1>
            <p className="text-sm text-muted-foreground">
              Shareable uptime pages for your monitors. Public pages need no sign-in.
            </p>
          </div>
          <Button
            onClick={() => {
              setEditing(null);
              setDialogOpen(true);
            }}
          >
            <PlusIcon data-icon="inline-start" />
            New page
          </Button>
        </div>

        <Card className="mt-6">
          <CardHeader>
            <CardTitle>Pages</CardTitle>
          </CardHeader>
          <CardContent className="p-0">
            {pages === null ? (
              <div className="flex flex-col gap-3 p-6">
                <Skeleton className="h-10 w-full" />
                <Skeleton className="h-10 w-full" />
              </div>
            ) : pages.length === 0 ? (
              <Empty className="py-12">
                <EmptyHeader>
                  <EmptyMedia variant="icon">
                    <ArrowUpRightIcon />
                  </EmptyMedia>
                  <EmptyTitle>No status pages yet</EmptyTitle>
                  <EmptyDescription>
                    Bundle monitors into a public page and share the link.
                  </EmptyDescription>
                </EmptyHeader>
                <EmptyContent>
                  <Button
                    onClick={() => {
                      setEditing(null);
                      setDialogOpen(true);
                    }}
                  >
                    <PlusIcon data-icon="inline-start" />
                    New page
                  </Button>
                </EmptyContent>
              </Empty>
            ) : (
              <div className="overflow-x-auto">
                <Table className="min-w-[640px]">
                  <TableHeader>
                    <TableRow>
                      <TableHead>Page</TableHead>
                      <TableHead>Link</TableHead>
                      <TableHead>Monitors</TableHead>
                      <TableHead>Public</TableHead>
                      <TableHead className="w-24">
                        <span className="sr-only">Actions</span>
                      </TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {pages.map((page) => (
                      <TableRow key={page.statusPageId}>
                        <TableCell className="font-medium">{page.name}</TableCell>
                        <TableCell className="font-mono text-xs">
                          <Link
                            to={`/status/${page.slug}`}
                            target="_blank"
                            rel="noreferrer"
                            className="underline-offset-4 hover:underline"
                          >
                            /status/{page.slug}
                          </Link>
                        </TableCell>
                        <TableCell className="font-mono tabular-nums">
                          {page.monitors.length}
                        </TableCell>
                        <TableCell>
                          <Switch
                            checked={page.isPublic}
                            onCheckedChange={() => togglePublic(page)}
                            aria-label={`Publish ${page.name}`}
                          />
                        </TableCell>
                        <TableCell>
                          <div className="flex gap-1">
                            <Button
                              variant="ghost"
                              size="icon"
                              onClick={() => {
                                setEditing(page);
                                setDialogOpen(true);
                              }}
                            >
                              <PencilIcon data-icon="inline-start" />
                              <span className="sr-only">Edit page</span>
                            </Button>
                            <Button variant="ghost" size="icon" onClick={() => setDeleting(page)}>
                              <Trash2Icon data-icon="inline-start" />
                              <span className="sr-only">Delete page</span>
                            </Button>
                          </div>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>
            )}
          </CardContent>
        </Card>
        {pages !== null && pages.length > 0 && (
          <p className="mt-3 flex items-center gap-2 text-sm text-muted-foreground">
            <Badge variant="secondary">Tip</Badge>
            Private pages are only visible here. Flip a page public to share its link.
          </p>
        )}
      </main>

      <StatusPageDialog
        open={dialogOpen}
        onOpenChange={setDialogOpen}
        initial={editing}
        onSaved={(saved) => {
          setPages((current) => {
            if (!current) return [saved];
            return current.some((p) => p.statusPageId === saved.statusPageId)
              ? current.map((p) => (p.statusPageId === saved.statusPageId ? saved : p))
              : [...current, saved];
          });
        }}
      />

      <AlertDialog open={deleting !== null} onOpenChange={(open) => !open && setDeleting(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete this status page?</AlertDialogTitle>
            <AlertDialogDescription>
              {deleting
                ? `“${deleting.name}” will no longer be reachable. Monitors are unaffected.`
                : "This page will no longer be reachable. Monitors are unaffected."}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction onClick={confirmDelete}>Delete</AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
