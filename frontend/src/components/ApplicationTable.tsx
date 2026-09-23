import type { JobApplication } from "../types";
import { StatusBadge } from "./StatusBadge";

interface Props {
  applications: JobApplication[];
  isArchivedView: boolean;
  onEdit: (app: JobApplication) => void;
  onArchive: (app: JobApplication) => void;
  onRestore: (app: JobApplication) => void;
  onPermanentDelete: (app: JobApplication) => void;
}

function daysSince(dateIso: string): number {
  const applied = new Date(dateIso).getTime();
  const now = Date.now();
  return Math.floor((now - applied) / (1000 * 60 * 60 * 24));
}

function isOverdue(app: JobApplication): boolean {
  if (!app.nextFollowUpDate) return false;
  if (app.status === "Rejected" || app.status === "Offer") return false;
  const followUp = new Date(app.nextFollowUpDate);
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  return followUp < today;
}

export function ApplicationTable({
  applications,
  isArchivedView,
  onEdit,
  onArchive,
  onRestore,
  onPermanentDelete,
}: Props) {
  if (applications.length === 0) {
    return (
      <p className="empty-state">
        {isArchivedView ? "No archived applications." : "No applications match the current filter."}
      </p>
    );
  }

  return (
    <table className="application-table">
      <thead>
        <tr>
          <th>Company</th>
          <th>Role</th>
          <th>Status</th>
          <th>Applied</th>
          <th>Days since</th>
          <th>Follow-up</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        {applications.map((app) => {
          const overdue = isOverdue(app);
          return (
            <tr key={app.id}>
              <td data-label="Company">{app.company}</td>
              <td data-label="Role">{app.role}</td>
              <td data-label="Status">
                <StatusBadge status={app.status} />
              </td>
              <td data-label="Applied">{new Date(app.dateApplied).toLocaleDateString()}</td>
              <td data-label="Days since">{daysSince(app.dateApplied)}</td>
              <td data-label="Follow-up" className={overdue ? "follow-up-overdue" : undefined}>
                {app.nextFollowUpDate
                  ? `${new Date(app.nextFollowUpDate).toLocaleDateString()}${overdue ? " (overdue)" : ""}`
                  : "—"}
              </td>
              <td className="row-actions">
                {isArchivedView ? (
                  <>
                    <button className="link-button" onClick={() => onRestore(app)}>
                      Restore
                    </button>
                    <button className="link-button danger" onClick={() => onPermanentDelete(app)}>
                      Delete permanently
                    </button>
                  </>
                ) : (
                  <>
                    <button className="link-button" onClick={() => onEdit(app)}>
                      Edit
                    </button>
                    <button className="link-button danger" onClick={() => onArchive(app)}>
                      Archive
                    </button>
                  </>
                )}
              </td>
            </tr>
          );
        })}
      </tbody>
    </table>
  );
}
