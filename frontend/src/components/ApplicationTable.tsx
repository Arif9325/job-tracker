import type { JobApplication } from "../types";
import { StatusBadge } from "./StatusBadge";

interface Props {
  applications: JobApplication[];
  onEdit: (app: JobApplication) => void;
  onDelete: (app: JobApplication) => void;
}

function daysSince(dateIso: string): number {
  const applied = new Date(dateIso).getTime();
  const now = Date.now();
  return Math.floor((now - applied) / (1000 * 60 * 60 * 24));
}

export function ApplicationTable({ applications, onEdit, onDelete }: Props) {
  if (applications.length === 0) {
    return <p className="empty-state">No applications match the current filter.</p>;
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
          <th></th>
        </tr>
      </thead>
      <tbody>
        {applications.map((app) => (
          <tr key={app.id}>
            <td data-label="Company">{app.company}</td>
            <td data-label="Role">{app.role}</td>
            <td data-label="Status">
              <StatusBadge status={app.status} />
            </td>
            <td data-label="Applied">{new Date(app.dateApplied).toLocaleDateString()}</td>
            <td data-label="Days since">{daysSince(app.dateApplied)}</td>
            <td className="row-actions">
              <button className="link-button" onClick={() => onEdit(app)}>
                Edit
              </button>
              <button className="link-button danger" onClick={() => onDelete(app)}>
                Delete
              </button>
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
