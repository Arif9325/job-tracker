import type { ApplicationStats } from "../types";

export function StatsSummary({ stats }: { stats: ApplicationStats }) {
  const cards: Array<{ label: string; value: number; className: string }> = [
    { label: "Total", value: stats.total, className: "stat-total" },
    { label: "Applied", value: stats.applied, className: "stat-applied" },
    { label: "Interviewing", value: stats.interviewing, className: "stat-interviewing" },
    { label: "Offers", value: stats.offer, className: "stat-offer" },
    { label: "Rejected", value: stats.rejected, className: "stat-rejected" },
  ];

  return (
    <div className="stats-grid">
      {cards.map((c) => (
        <div key={c.label} className={`stat-card ${c.className}`}>
          <span className="stat-value">{c.value}</span>
          <span className="stat-label">{c.label}</span>
        </div>
      ))}
    </div>
  );
}
