import { useState, type FormEvent } from "react";
import { ALL_STATUSES, type ApplicationStatus, type JobApplication } from "../types";

export interface ApplicationFormValues {
  company: string;
  role: string;
  status: ApplicationStatus;
  dateApplied: string; // yyyy-mm-dd, matching <input type="date">
  postingUrl: string;
  notes: string;
}

interface Props {
  initial?: JobApplication;
  onSubmit: (values: ApplicationFormValues) => Promise<void>;
  onCancel: () => void;
}

function toDateInputValue(iso: string): string {
  return iso.slice(0, 10);
}

export function ApplicationForm({ initial, onSubmit, onCancel }: Props) {
  const [values, setValues] = useState<ApplicationFormValues>({
    company: initial?.company ?? "",
    role: initial?.role ?? "",
    status: initial?.status ?? "Applied",
    dateApplied: initial ? toDateInputValue(initial.dateApplied) : toDateInputValue(new Date().toISOString()),
    postingUrl: initial?.postingUrl ?? "",
    notes: initial?.notes ?? "",
  });
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  function update<K extends keyof ApplicationFormValues>(key: K, value: ApplicationFormValues[K]) {
    setValues((v) => ({ ...v, [key]: value }));
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      await onSubmit(values);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Something went wrong");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <form className="application-form" onSubmit={handleSubmit}>
      <h2>{initial ? "Edit application" : "Add application"}</h2>
      {error && <p className="error">{error}</p>}

      <label>
        Company
        <input
          value={values.company}
          onChange={(e) => update("company", e.target.value)}
          required
        />
      </label>

      <label>
        Role
        <input
          value={values.role}
          onChange={(e) => update("role", e.target.value)}
          required
        />
      </label>

      <label>
        Status
        <select
          value={values.status}
          onChange={(e) => update("status", e.target.value as ApplicationStatus)}
        >
          {ALL_STATUSES.map((s) => (
            <option key={s} value={s}>
              {s}
            </option>
          ))}
        </select>
      </label>

      <label>
        Date applied
        <input
          type="date"
          value={values.dateApplied}
          onChange={(e) => update("dateApplied", e.target.value)}
          required
        />
      </label>

      <label>
        Posting URL
        <input
          type="url"
          placeholder="https://..."
          value={values.postingUrl}
          onChange={(e) => update("postingUrl", e.target.value)}
        />
      </label>

      <label>
        Notes
        <textarea
          value={values.notes}
          onChange={(e) => update("notes", e.target.value)}
          rows={3}
        />
      </label>

      <div className="form-actions">
        <button type="button" onClick={onCancel} className="secondary">
          Cancel
        </button>
        <button type="submit" disabled={submitting}>
          {submitting ? "Saving..." : "Save"}
        </button>
      </div>
    </form>
  );
}
