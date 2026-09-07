const API_BASE = "/api";
const TOKEN_KEY = "keeptabs.token";

export type ProtocolType = "Http" | "Tcp" | "Ping";

export interface Monitor {
  monitorId: string;
  userId: string;
  name: string;
  url: string;
  protocol: ProtocolType;
  checkIntervalSeconds: number;
  timeoutSeconds: number;
  expectedStatusCode: number | null;
  useHeadRequest: boolean;
  isPaused: boolean;
  lastCheckedAt: string | null;
  lastStatusUp: boolean | null;
}

export interface CreateMonitorRequest {
  name: string;
  url: string;
  protocol: ProtocolType;
  checkIntervalSeconds: number;
  timeoutSeconds: number;
  expectedStatusCode: number | null;
  useHeadRequest: boolean;
}

export interface UpdateMonitorRequest {
  name?: string | null;
  url?: string | null;
  protocol?: ProtocolType | null;
  checkIntervalSeconds?: number | null;
  timeoutSeconds?: number | null;
  expectedStatusCode?: number | null;
  isPaused?: boolean | null;
  useHeadRequest?: boolean | null;
}

export interface MonitorSummary {
  monitorId: string;
  name: string;
  url: string;
  totalChecks: number;
  upCount: number;
  downCount: number;
  uptimePercentage: number;
  averageResponseTimeMs: number;
  lastCheckedAt: string | null;
  lastStatusUp: boolean | null;
}

export interface MonitorCheck {
  checkId: string;
  timestamp: string;
  isUp: boolean;
  statusCode: number | null;
  responseTimeMs: number;
  errorMessage: string | null;
  sslDaysRemaining: number | null;
}

export interface DailyUptime {
  date: string;
  totalChecks: number;
  upCount: number;
  uptimePercentage: number;
  averageResponseTimeMs: number;
}

export interface AuthResponse {
  token: string;
  userId: string;
  email: string;
  firstName: string | null;
  lastName: string | null;
  roles: string[];
}

export interface UserProfile {
  userId: string;
  email: string;
  firstName: string | null;
  lastName: string | null;
}

export interface ProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
  errors?: Record<string, string[]>;
}

export type AlertType = "Email" | "Webhook" | "Telegram";
export type AlertTriggerType = "OnDown" | "OnUp" | "ConsecutiveFailures";

export interface AlertRule {
  alertRuleId: string;
  monitorId: string;
  monitorName: string;
  type: AlertType;
  triggerType: AlertTriggerType;
  threshold: number;
  coolDownMinutes: number;
  isEnabled: boolean;
  target: string;
  lastFiredAt: string | null;
}

export interface CreateAlertRuleRequest {
  monitorId: string;
  type: AlertType;
  triggerType: AlertTriggerType;
  threshold: number;
  coolDownMinutes: number;
  target: string;
  isEnabled: boolean;
}

export interface UpdateAlertRuleRequest {
  type?: AlertType | null;
  triggerType?: AlertTriggerType | null;
  threshold?: number | null;
  coolDownMinutes?: number | null;
  target?: string | null;
  isEnabled?: boolean | null;
}

export interface AlertLog {
  alertLogId: string;
  alertRuleId: string;
  monitorId: string;
  monitorName: string;
  firedAt: string;
  message: string;
  success: boolean;
  error: string | null;
}

export interface TestAlertResult {
  success: boolean;
  error: string | null;
  message: string;
}

export interface SmtpSettings {
  enabled: boolean;
  host: string;
  port: number;
  username: string;
  passwordSet: boolean;
  from: string;
  enableSsl: boolean;
}

export interface UpdateSmtpSettingsRequest {
  enabled: boolean;
  host: string;
  port: number;
  username: string;
  password: string;
  from: string;
  enableSsl: boolean;
}

export interface TelegramSettings {
  enabled: boolean;
  tokenSet: boolean;
}

export interface UpdateTelegramSettingsRequest {
  enabled: boolean;
  botToken: string;
}

export class ApiError extends Error {
  status: number;
  problem: ProblemDetails | null;

  constructor(status: number, problem: ProblemDetails | null) {
    super(ApiError.describe(status, problem));
    this.name = "ApiError";
    this.status = status;
    this.problem = problem;
  }

  private static describe(status: number, problem: ProblemDetails | null): string {
    if (problem?.errors) {
      const first = Object.values(problem.errors).flat()[0];
      if (first) return first;
    }
    if (problem?.detail) return problem.detail;
    if (problem?.title) return problem.title;
    if (status === 401) return "Your session has expired. Sign in again.";
    if (status === 404) return "The requested item was not found.";
    return `Request failed (HTTP ${status}).`;
  }
}

export function getToken(): string | null {
  return localStorage.getItem(TOKEN_KEY);
}

export function setToken(token: string | null): void {
  if (token) {
    localStorage.setItem(TOKEN_KEY, token);
  } else {
    localStorage.removeItem(TOKEN_KEY);
  }
}

interface RequestOptions {
  method?: string;
  body?: unknown;
}

export async function request<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const token = getToken();
  const response = await fetch(`${API_BASE}${path}`, {
    method: options.method ?? "GET",
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    body: options.body === undefined ? undefined : JSON.stringify(options.body),
  });

  if (response.status === 204) {
    return undefined as T;
  }

  if (!response.ok) {
    let problem: ProblemDetails | null = null;
    try {
      problem = (await response.json()) as ProblemDetails;
    } catch {
      problem = null;
    }
    throw new ApiError(response.status, problem);
  }

  return (await response.json()) as T;
}

export const settingsApi = {
  getSmtp: () => request<SmtpSettings>("/settings/smtp"),
  updateSmtp: (payload: UpdateSmtpSettingsRequest) =>
    request<SmtpSettings>("/settings/smtp", { method: "PUT", body: payload }),
  getTelegram: () => request<TelegramSettings>("/settings/telegram"),
  updateTelegram: (payload: UpdateTelegramSettingsRequest) =>
    request<TelegramSettings>("/settings/telegram", { method: "PUT", body: payload }),
};

export interface StatusPageMonitorRef {
  monitorId: string;
  monitorName: string;
}

export interface StatusPage {
  statusPageId: string;
  name: string;
  slug: string;
  isPublic: boolean;
  monitors: StatusPageMonitorRef[];
}

export interface CreateStatusPageRequest {
  name: string;
  slug: string;
  isPublic: boolean;
  monitorIds: string[];
}

export interface UpdateStatusPageRequest {
  name?: string | null;
  slug?: string | null;
  isPublic?: boolean | null;
  monitorIds?: string[] | null;
}

export interface SlugAvailability {
  slug: string;
  available: boolean;
  suggestion: string;
}

export interface PublicStatusMonitor {
  monitorId: string;
  name: string;
  url: string;
  protocol: ProtocolType;
  lastStatusUp: boolean | null;
  lastCheckedAt: string | null;
  uptimePercentage: number | null;
  hourly: HourlyUptime[];
  daily: DailyUptime[];
}

export interface HourlyUptime {
  hour: string;
  totalChecks: number;
  upCount: number;
  uptimePercentage: number;
  averageResponseTimeMs: number;
}

export interface PublicStatusPage {
  name: string;
  slug: string;
  monitors: PublicStatusMonitor[];
}

export const statusPagesApi = {
  list: () => request<StatusPage[]>("/status-pages"),
  get: (statusPageId: string) => request<StatusPage>(`/status-pages/${statusPageId}`),
  create: (payload: CreateStatusPageRequest) =>
    request<StatusPage>("/status-pages", { method: "POST", body: payload }),
  update: (statusPageId: string, payload: UpdateStatusPageRequest) =>
    request<StatusPage>(`/status-pages/${statusPageId}`, { method: "PUT", body: payload }),
  remove: (statusPageId: string) =>
    request<void>(`/status-pages/${statusPageId}`, { method: "DELETE" }),
  public: (slug: string) => request<PublicStatusPage>(`/status/${slug}`),
  checkSlug: (slug: string, name?: string, excludeId?: string) =>
    request<SlugAvailability>(
      `/status-pages/check-slug?slug=${encodeURIComponent(slug)}${name ? `&name=${encodeURIComponent(name)}` : ""}${excludeId ? `&excludeId=${excludeId}` : ""}`,
    ),
};

export const authApi = {
  login: (email: string, password: string) =>
    request<AuthResponse>("/auth/login", { method: "POST", body: { email, password } }),
  register: (email: string, password: string, firstName?: string, lastName?: string) =>
    request<AuthResponse>("/auth/register", {
      method: "POST",
      body: { email, password, firstName: firstName || null, lastName: lastName || null },
    }),
  me: () => request<UserProfile>("/auth/me"),
  regenerateApiKey: () => request<{ apiKey: string }>("/auth/api-key/regenerate", { method: "POST" }),
};

export const alertsApi = {
  list: (monitorId?: string) =>
    request<AlertRule[]>(`/alerts${monitorId ? `?monitorId=${monitorId}` : ""}`),
  get: (alertRuleId: string) => request<AlertRule>(`/alerts/${alertRuleId}`),
  create: (payload: CreateAlertRuleRequest) =>
    request<AlertRule>("/alerts", { method: "POST", body: payload }),
  update: (alertRuleId: string, payload: UpdateAlertRuleRequest) =>
    request<AlertRule>(`/alerts/${alertRuleId}`, { method: "PUT", body: payload }),
  remove: (alertRuleId: string) => request<void>(`/alerts/${alertRuleId}`, { method: "DELETE" }),
  test: (alertRuleId: string) =>
    request<TestAlertResult>(`/alerts/${alertRuleId}/test`, { method: "POST" }),
  logs: (monitorId?: string) =>
    request<AlertLog[]>(`/alerts/logs${monitorId ? `?monitorId=${monitorId}` : ""}`),
};
export const monitorsApi = {
  list: () => request<Monitor[]>("/monitors"),
  get: (monitorId: string) => request<Monitor>(`/monitors/${monitorId}`),
  create: (payload: CreateMonitorRequest) =>
    request<Monitor>("/monitors", { method: "POST", body: payload }),
  update: (monitorId: string, payload: UpdateMonitorRequest) =>
    request<Monitor>(`/monitors/${monitorId}`, { method: "PUT", body: payload }),
  remove: (monitorId: string) =>
    request<void>(`/monitors/${monitorId}`, { method: "DELETE" }),
  pause: (monitorId: string) => request<Monitor>(`/monitors/${monitorId}/pause`, { method: "PATCH" }),
  resume: (monitorId: string) => request<Monitor>(`/monitors/${monitorId}/resume`, { method: "PATCH" }),
  summary: (monitorId: string) => request<MonitorSummary>(`/monitors/${monitorId}/summary`),
  history: (monitorId: string, days = 7) =>
    request<MonitorCheck[]>(`/monitors/${monitorId}/history?days=${days}`),
  daily: (monitorId: string, days = 30) =>
    request<DailyUptime[]>(`/monitors/${monitorId}/daily?days=${days}`),
};
