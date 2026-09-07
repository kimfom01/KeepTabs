import { BrowserRouter, Navigate, Route, Routes } from "react-router";
import { Toaster } from "@/components/ui/sonner";
import { AuthProvider, useAuth } from "@/lib/auth";
import { ThemeProvider, useTheme } from "@/lib/theme";
import { AlertsPage } from "@/pages/alerts";
import { ApiKeysPage } from "@/pages/api-keys";
import { DocsPage } from "@/pages/docs";
import { LandingPage } from "@/pages/landing";
import { SettingsPage } from "@/pages/settings";
import { DashboardPage } from "@/pages/dashboard";
import { LoginPage } from "@/pages/login";
import { MonitorDetailPage } from "@/pages/monitor-detail";
import { NotFoundPage } from "@/pages/not-found";
import { RegisterPage } from "@/pages/register";
import { Spinner } from "@/components/ui/spinner";

function RequireAuth({ children }: { children: React.ReactNode }) {
  const { user, ready } = useAuth();

  if (!ready) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-background">
        <Spinner className="size-6" />
      </div>
    );
  }

  return user ? <>{children}</> : <Navigate to="/login" replace />;
}

function RedirectIfSignedIn({ children }: { children: React.ReactNode }) {
  const { user, ready } = useAuth();

  if (!ready) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-background">
        <Spinner className="size-6" />
      </div>
    );
  }

  return user ? <Navigate to="/dashboard" replace /> : <>{children}</>;
}

export default function App() {
  return (
    <ThemeProvider>
      <AuthProvider>
        <ThemedToaster />
        <BrowserRouter>
        <Routes>
          <Route
            path="/login"
            element={
              <RedirectIfSignedIn>
                <LoginPage />
              </RedirectIfSignedIn>
            }
          />
          <Route
            path="/register"
            element={
              <RedirectIfSignedIn>
                <RegisterPage />
              </RedirectIfSignedIn>
            }
          />
          <Route path="/" element={<LandingPage />} />
          <Route
            path="/dashboard"
            element={
              <RequireAuth>
                <DashboardPage />
              </RequireAuth>
            }
          />
          <Route
            path="/monitors/:monitorId"
            element={
              <RequireAuth>
                <MonitorDetailPage />
              </RequireAuth>
            }
          />
          <Route
            path="/alerts"
            element={
              <RequireAuth>
                <AlertsPage />
              </RequireAuth>
            }
          />
          <Route
            path="/api-keys"
            element={
              <RequireAuth>
                <ApiKeysPage />
              </RequireAuth>
            }
          />
          <Route
            path="/settings"
            element={
              <RequireAuth>
                <SettingsPage />
              </RequireAuth>
            }
          />
          <Route path="/docs" element={<DocsPage />} />
          <Route path="*" element={<NotFoundPage />} />
        </Routes>
      </BrowserRouter>
      </AuthProvider>
    </ThemeProvider>
  );
}

function ThemedToaster() {
  const { theme } = useTheme();

  return <Toaster position="top-center" theme={theme} />;
}
