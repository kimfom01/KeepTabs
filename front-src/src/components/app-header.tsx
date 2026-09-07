import { Link, NavLink, useNavigate } from "react-router";
import { ActivityIcon, BellRingIcon, BookOpenIcon, KeyRoundIcon, LogOutIcon, MonitorIcon, MoonIcon, SettingsIcon, SunIcon } from "lucide-react";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { displayName, useAuth } from "@/lib/auth";
import { useTheme } from "@/lib/theme";
import { cn } from "@/lib/utils";

const links = [
  { to: "/dashboard", label: "Monitors", icon: MonitorIcon, end: true, newTab: false },
  { to: "/alerts", label: "Alerts", icon: BellRingIcon, end: false, newTab: false },
  { to: "/api-keys", label: "API keys", icon: KeyRoundIcon, end: false, newTab: false },
  { to: "/docs", label: "Docs", icon: BookOpenIcon, end: false, newTab: true },
  { to: "/settings", label: "Settings", icon: SettingsIcon, end: false, newTab: false },
];

export function AppHeader() {
  const { user, signOut } = useAuth();
  const { theme, toggleTheme } = useTheme();
  const navigate = useNavigate();

  if (!user) return null;

  const initials = displayName(user)
    .split(" ")
    .map((part) => part[0])
    .join("")
    .slice(0, 2)
    .toUpperCase();

  return (
    <header className="border-b bg-card">
      <div className="mx-auto flex h-14 max-w-6xl items-center gap-3 px-4 sm:gap-6 sm:px-6">
        <Link to={user ? "/dashboard" : "/"} className="flex items-center gap-2">
          <span className="flex size-7 items-center justify-center rounded-md bg-primary text-primary-foreground">
            <ActivityIcon data-icon="inline-start" className="size-4" />
          </span>
          <span className="hidden text-base font-semibold tracking-tight min-[400px]:inline">
            KeepTabs
          </span>
        </Link>
        <nav className="flex items-center gap-1">
          {links.map(({ to, label, icon: Icon, end, newTab }) =>
            newTab ? (
              <a
                key={to}
                href={to}
                target="_blank"
                rel="noreferrer"
                className="flex items-center gap-2 rounded-md px-2 py-1.5 text-sm font-medium text-muted-foreground transition-colors hover:text-foreground sm:px-3"
              >
                <Icon data-icon="inline-start" className="size-4" />
                <span className="hidden md:inline">{label}</span>
              </a>
            ) : (
              <NavLink key={to} to={to} end={end}>
                {({ isActive }) => (
                  <span
                    className={cn(
                      "flex items-center gap-2 rounded-md px-2 py-1.5 text-sm font-medium transition-colors sm:px-3",
                      isActive
                        ? "bg-secondary text-secondary-foreground"
                        : "text-muted-foreground hover:text-foreground",
                    )}
                  >
                    <Icon data-icon="inline-start" className="size-4" />
                    <span className="hidden md:inline">{label}</span>
                  </span>
                )}
              </NavLink>
            ),
          )}
        </nav>
        <div className="ml-auto flex items-center gap-1">
          <Button variant="ghost" size="icon" onClick={toggleTheme}>
            {theme === "light" ? (
              <MoonIcon data-icon="inline-start" />
            ) : (
              <SunIcon data-icon="inline-start" />
            )}
            <span className="sr-only">Toggle color theme</span>
          </Button>
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="icon" className="rounded-full">
                <Avatar className="size-8">
                  <AvatarFallback>{initials}</AvatarFallback>
                </Avatar>
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuLabel>
                <span className="block truncate font-medium">{displayName(user)}</span>
                <span className="block truncate text-xs font-normal text-muted-foreground">
                  {user.email}
                </span>
              </DropdownMenuLabel>
              <DropdownMenuSeparator />
              <DropdownMenuGroup>
                <DropdownMenuItem
                  onSelect={() => {
                    signOut();
                    navigate("/login", { replace: true });
                  }}
                >
                  <LogOutIcon data-icon="inline-start" />
                  Sign out
                </DropdownMenuItem>
              </DropdownMenuGroup>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      </div>
    </header>
  );
}
