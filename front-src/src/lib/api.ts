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
}

export interface UpdateMonitorRequest {
  name?: string | null;
  url?: string | null;
  protocol?: ProtocolType | null;
  checkIntervalSeconds?: number | null;
  timeoutSeconds?: number | null;
  expectedStatusCode?: number | null;
  isPaused?: boolean | null;
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
};
