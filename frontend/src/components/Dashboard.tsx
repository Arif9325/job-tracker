import { useEffect, useMemo, useState } from "react";
import { applicationsApi } from "../api";
import { useAuth } from "../context/AuthContext";
import { ALL_STATUSES, type ApplicationStats, type ApplicationStatus, type JobApplication } from "../types";
import { ApplicationForm, type ApplicationFormValues } from "./ApplicationForm";
import { ApplicationTable } from "./ApplicationTable";
import { Logo } from "./Logo";
import { StatsSummary } from "./StatsSummary";

type SortKey = "dateApplied" | "company" | "status";
type StatusFilter = ApplicationStatus | "All";

export function Dashboard() {
  const { email, logout } = useAuth();

  const [applications, setApplications] = useState<JobApplication[]>([]);
  const [stats, setStats] = useState<ApplicationStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [statusFilter, setStatusFilter] = useState<StatusFilter>("All");
  const [sortKey, setSortKey] = useState<SortKey>("dateApplied");

  const [showForm, setShowForm] = useState(false);
  const [editingApp, setEditingApp] = useState<JobApplication | undefined>(undefined);

  async function refresh() {
    setLoading(true);
    setError(null);
    try {
      const [apps, statsResult] = await Promise.all([
        applicationsApi.getAll(),
        applicationsApi.getStats(),
      ]);
      setApplications(apps);
      setStats(statsResult);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load applications");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    refresh();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const visibleApplications = useMemo(() => {
    let list = applications;
    if (statusFilter !== "All") {
      list = list.filter((a) => a.status === statusFilter);
    }

    // Copy before sorting — Array.sort mutates in place, and we never
    // want to mutate state directly in React.
    return [...list].sort((a, b) => {
      switch (sortKey) {
        case "company":
          return a.company.localeCompare(b.company);
        case "status":
          return a.status.localeCompare(b.status);
        case "dateApplied":
        default:
          return new Date(b.dateApplied).getTime() - new Date(a.dateApplied).getTime();
      }
    });
  }, [applications, statusFilter, sortKey]);

  function openCreateForm() {
    setEditingApp(undefined);
    setShowForm(true);
  }

  function openEditForm(app: JobApplication) {
    setEditingApp(app);
    setShowForm(true);
  }

  async function handleFormSubmit(values: ApplicationFormValues) {
    const payload = {
      company: values.company,
      role: values.role,
      status: values.status,
      dateApplied: new Date(values.dateApplied).toISOString(),
      postingUrl: values.postingUrl || null,
      notes: values.notes || null,
    };

    if (editingApp) {
      await applicationsApi.update(editingApp.id, payload);
    } else {
      await applicationsApi.create(payload);
    }

    setShowForm(false);
    await refresh();
  }

  async function handleDelete(app: JobApplication) {
    const confirmed = window.confirm(`Delete the application for ${app.role} at ${app.company}?`);
    if (!confirmed) return;

    await applicationsApi.delete(app.id);
    await refresh();
  }

  return (
    <div className="dashboard">
      <header className="dashboard-header">
        <div className="dashboard-brand">
          <Logo size={32} />
          <h1>JobTrail</h1>
        </div>
        <div className="header-right">
          <span className="user-email">{email}</span>
          <button className="secondary" onClick={logout}>
            Log out
          </button>
        </div>
      </header>

      {error && <p className="error">{error}</p>}

      {stats && <StatsSummary stats={stats} />}

      <div className="toolbar">
        <label>
          Status
          <select value={statusFilter} onChange={(e) => setStatusFilter(e.target.value as StatusFilter)}>
            <option value="All">All</option>
            {ALL_STATUSES.map((s) => (
              <option key={s} value={s}>
                {s}
              </option>
            ))}
          </select>
        </label>

        <label>
          Sort by
          <select value={sortKey} onChange={(e) => setSortKey(e.target.value as SortKey)}>
            <option value="dateApplied">Date applied</option>
            <option value="company">Company</option>
            <option value="status">Status</option>
          </select>
        </label>

        <button onClick={openCreateForm}>+ Add application</button>
      </div>

      {loading ? (
        <p>Loading...</p>
      ) : (
        <ApplicationTable
          applications={visibleApplications}
          onEdit={openEditForm}
          onDelete={handleDelete}
        />
      )}

      {showForm && (
        <div className="modal-backdrop" onClick={() => setShowForm(false)}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <ApplicationForm
              initial={editingApp}
              onSubmit={handleFormSubmit}
              onCancel={() => setShowForm(false)}
            />
          </div>
        </div>
      )}
    </div>
  );
}
