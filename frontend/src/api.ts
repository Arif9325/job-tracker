import { getToken } from "./storage";
import type {
  ApplicationStats,
  AuthResponse,
  CreateJobApplicationInput,
  JobApplication,
  PagedResult,
  UpdateJobApplicationInput,
} from "./types";

// In production (Vercel) this comes from the VITE_API_BASE_URL env var,
// set to the deployed Azure API's URL. Locally it falls back to the
// dev API running on your machine.
const API_BASE = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5203/api";

// Tries to turn a non-2xx response body into a readable message. The
// API sometimes returns a plain JSON string (e.g. the lockout message),
// sometimes an ASP.NET "ProblemDetails" validation error object, and
// sometimes plain text — this covers all three rather than showing the
// user a raw, quoted JSON blob.
async function extractErrorMessage(response: Response): Promise<string> {
  const text = await response.text();
  if (!text) return `Request failed with status ${response.status}`;

  try {
    const parsed = JSON.parse(text);
    if (typeof parsed === "string") return parsed;
    if (parsed?.title) return parsed.title;
    if (parsed?.errors) {
      const firstError = Object.values(parsed.errors).flat()[0];
      if (typeof firstError === "string") return firstError;
    }
  } catch {
    // Not JSON — fall through and use the raw text below.
  }
  return text;
}

// A small wrapper around fetch that: builds the full URL, attaches the
// JWT (if we have one) as an Authorization header, and throws a real
// Error with a useful message on non-2xx responses instead of silently
// returning bad data.
async function request<T>(
  path: string,
  options: RequestInit = {}
): Promise<T> {
  const token = getToken();

  const response = await fetch(`${API_BASE}${path}`, {
    ...options,
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...options.headers,
    },
  });

  if (!response.ok) {
    throw new Error(await extractErrorMessage(response));
  }

  // DELETE / archive / restore return 204 No Content — nothing to parse.
  if (response.status === 204) {
    return undefined as T;
  }

  return response.json() as Promise<T>;
}

export const authApi = {
  register: (email: string, password: string) =>
    request<AuthResponse>("/auth/register", {
      method: "POST",
      body: JSON.stringify({ email, password }),
    }),

  login: (email: string, password: string, rememberMe: boolean) =>
    request<AuthResponse>("/auth/login", {
      method: "POST",
      body: JSON.stringify({ email, password, rememberMe }),
    }),
};

export const applicationsApi = {
  getAll: (page: number, pageSize: number, includeArchived: boolean) =>
    request<PagedResult<JobApplication>>(
      `/applications?page=${page}&pageSize=${pageSize}&includeArchived=${includeArchived}`
    ),

  getStats: () => request<ApplicationStats>("/applications/stats"),

  create: (input: CreateJobApplicationInput) =>
    request<JobApplication>("/applications", {
      method: "POST",
      body: JSON.stringify(input),
    }),

  update: (id: number, input: UpdateJobApplicationInput) =>
    request<JobApplication>(`/applications/${id}`, {
      method: "PUT",
      body: JSON.stringify(input),
    }),

  archive: (id: number) =>
    request<void>(`/applications/${id}/archive`, { method: "POST" }),

  restore: (id: number) =>
    request<void>(`/applications/${id}/restore`, { method: "POST" }),

  // Permanent delete — only succeeds server-side on an already-archived
  // application.
  delete: (id: number) =>
    request<void>(`/applications/${id}`, { method: "DELETE" }),
};
