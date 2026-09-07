import { Link } from "react-router";
import { ActivityIcon, ArrowRightIcon, MoonIcon, SunIcon } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useAuth } from "@/lib/auth";
import { useTheme } from "@/lib/theme";

export function PublicHeader() {
  const { user } = useAuth();
  const { theme, toggleTheme } = useTheme();

  return (
    <header className="mx-auto flex h-14 max-w-6xl items-center gap-2 px-4 sm:px-6">
      <Link to="/" className="flex items-center gap-2">
        <span className="flex size-7 items-center justify-center rounded-md bg-primary text-primary-foreground">
          <ActivityIcon data-icon="inline-start" className="size-4" />
        </span>
        <span className="text-base font-semibold tracking-tight">KeepTabs</span>
      </Link>
      <div className="ml-auto flex items-center gap-2">
        <Button variant="ghost" size="icon" onClick={toggleTheme}>
          {theme === "light" ? (
            <MoonIcon data-icon="inline-start" />
          ) : (
            <SunIcon data-icon="inline-start" />
          )}
          <span className="sr-only">Toggle color theme</span>
        </Button>
        {user ? (
          <Button asChild size="sm">
            <Link to="/dashboard">
              Open dashboard
              <ArrowRightIcon data-icon="inline-start" />
            </Link>
          </Button>
        ) : (
          <>
            <Button variant="ghost" size="sm" asChild>
              <Link to="/login">Sign in</Link>
            </Button>
            <Button size="sm" asChild>
              <Link to="/register">Get started</Link>
            </Button>
          </>
        )}
      </div>
    </header>
  );
}
