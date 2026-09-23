import { useEffect, useMemo, useState } from "react";
import { applicationsApi } from "../api";
import { useAuth } from "../context/AuthContext";
import { ALL_STATUSES, type ApplicationStats, type ApplicationStatus, type JobApplication } from "../types";
import { ApplicationForm, type ApplicationFormValues } from "./ApplicationForm";
import { ApplicationTable } from "./ApplicationTable";
import { Logo } from "./Logo";
import { StatsSummary } from "./StatsSummary";
import { ThemeToggle } from "./ThemeToggle";

type SortKey = "dateApplied" | "company" | "status";
type StatusFilter = ApplicationStatus | "All";
const PAGE_SIZE = 10;

export function Dashboard() {
  const { email, logout } = useAuth();

  const [applications, setApplications] = useState<JobApplication[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [stats, setStats] = useState<ApplicationStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [page, setPage] = useState(1);
  const [includeArchived, setIncludeArchived] = useState(false);
  const [statusFilter, setStatusFilter] = useState<StatusFilter>("All");
  const [sortKey, setSortKey] = useState<SortKey>("dateApplied");

  const [showForm, setShowForm] = useState(false);
  const [editingApp, setEditingApp] = useState<JobApplication | undefined>(undefined);

  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));

  async function refresh() {
    setLoading(true);
    setError(null);
    try {
      const [pageResult, statsResult] = await Promise.all([
        applicationsApi.getAll(page, PAGE_SIZE, includeArchived),
        applicationsApi.getStats(),
      ]);

      // If an archive/delete just emptied the current page (and it's not
      // page 1), step back a page rather than showing an empty page with
      // valid pages still behind it.
      if (pageResult.items.length === 0 && page > 1 && pageResult.totalCount > 0) {
        setPage((p) => p - 1);
        return;
      }

      setApplications(pageResult.items);
      setTotalCount(pageResult.totalCount);
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
  }, [page, includeArchived]);

  const visibleApplications = useMemo(() => {
    // Status filter and sort apply only within the current page, since
    // pagination happens server-side — sorting/filtering the full
    // dataset would need those to move server-side too. Fine at the
    // 10-per-page scale this app targets; worth knowing as a limitation
    // if this ever needs to scale further.
    let list = applications;
    if (statusFilter !== "All") {
      list = list.filter((a) => a.status === statusFilter);
    }

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
      nextFollowUpDate: values.nextFollowUpDate ? new Date(values.nextFollowUpDate).toISOString() : null,
    };

    if (editingApp) {
      await applicationsApi.update(editingApp.id, payload);
    } else {
      await applicationsApi.create(payload);
    }

    setShowForm(false);
    await refresh();
  }

  async function handleArchive(app: JobApplication) {
    await applicationsApi.archive(app.id);
    await refresh();
  }

  async function handleRestore(app: JobApplication) {
    await applicationsApi.restore(app.id);
    await refresh();
  }

  async function handlePermanentDelete(app: JobApplication) {
    const confirmed = window.confirm(
      `Permanently delete the application for ${app.role} at ${app.company}? This can't be undone.`
    );
    if (!confirmed) return;

    await applicationsApi.delete(app.id);
    await refresh();
  }

  function toggleArchivedView() {
    setIncludeArchived((v) => !v);
    setPage(1);
  }

  return (
    <div className="dashboard">
      <header className="dashboard-header">
        <div className="dashboard-brand">
          <Logo size={32} />
          <h1>JobTrail</h1>
        </div>
        <div className="header-right">
          <ThemeToggle />
          <span className="user-email">{email}</span>
          <button className="secondary" onClick={logout}>
            Log out
          </button>
        </div>
      </header>

      {error && <p className="error">{error}</p>}

      {stats && <StatsSummary stats={stats} />}

      <div className="toolbar">
        {!includeArchived && (
          <>
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
          </>
        )}

        <button type="button" className="secondary" onClick={toggleArchivedView}>
          {includeArchived ? "← Back to active" : "View archived"}
        </button>

        {!includeArchived && <button onClick={openCreateForm}>+ Add application</button>}
      </div>

      {loading ? (
        <p>Loading...</p>
      ) : (
        <>
          <ApplicationTable
            applications={visibleApplications}
            isArchivedView={includeArchived}
            onEdit={openEditForm}
            onArchive={handleArchive}
            onRestore={handleRestore}
            onPermanentDelete={handlePermanentDelete}
          />

          {totalPages > 1 && (
            <div className="pagination">
              <button
                className="secondary"
                disabled={page <= 1}
                onClick={() => setPage((p) => p - 1)}
              >
                ← Previous
              </button>
              <span className="pagination-status">
                Page {page} of {totalPages} ({totalCount} total)
              </span>
              <button
                className="secondary"
                disabled={page >= totalPages}
                onClick={() => setPage((p) => p + 1)}
              >
                Next →
              </button>
            </div>
          )}
        </>
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
