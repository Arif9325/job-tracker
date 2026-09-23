// Mirrors the enum in backend/JobTracker.Api/Models/ApplicationStatus.cs
export type ApplicationStatus = "Applied" | "Interviewing" | "Offer" | "Rejected";

export const ALL_STATUSES: ApplicationStatus[] = [
  "Applied",
  "Interviewing",
  "Offer",
  "Rejected",
];

// Mirrors JobApplicationDto in the backend.
export interface JobApplication {
  id: number;
  company: string;
  role: string;
  status: ApplicationStatus;
  dateApplied: string; // ISO date string, as JSON has no native date type
  postingUrl: string | null;
  notes: string | null;
  nextFollowUpDate: string | null;
  isArchived: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateJobApplicationInput {
  company: string;
  role: string;
  status: ApplicationStatus;
  dateApplied: string;
  postingUrl: string | null;
  notes: string | null;
  nextFollowUpDate: string | null;
}

export type UpdateJobApplicationInput = CreateJobApplicationInput;

export interface ApplicationStats {
  total: number;
  applied: number;
  interviewing: number;
  offer: number;
  rejected: number;
  overdueFollowUps: number;
}

// Mirrors PagedResultDto<T> in the backend.
export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface AuthResponse {
  token: string;
  email: string;
}
