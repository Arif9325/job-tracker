import { getToken } from "./storage";
import type {
  ApplicationStats,
  AuthResponse,
  CreateJobApplicationInput,
  JobApplication,
  UpdateJobApplicationInput,
} from "./types";

// In production (Vercel) this comes from the VITE_API_BASE_URL env var,
// set to the deployed Azure API's URL. Locally it falls back to the
// dev API running on your machine.
const API_BASE = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5203/api";

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
    const text = await response.text();
    throw new Error(text || `Request failed with status ${response.status}`);
  }

  // DELETE returns 204 No Content — nothing to parse.
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
  getAll: () => request<JobApplication[]>("/applications"),

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

  delete: (id: number) =>
    request<void>(`/applications/${id}`, { method: "DELETE" }),
};
