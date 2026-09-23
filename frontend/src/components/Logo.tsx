// A small inline SVG mark — a checklist/briefcase motif — instead of an
// image file, so there's nothing to host or load: it's just code.
export function Logo({ size = 40 }: { size?: number }) {
  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 40 40"
      fill="none"
      xmlns="http://www.w3.org/2000/svg"
      aria-hidden="true"
    >
      <rect width="40" height="40" rx="10" fill="#3b82f6" />
      <rect x="10" y="14" width="20" height="15" rx="2.5" stroke="white" strokeWidth="2" />
      <path d="M15 14V12a3 3 0 0 1 3-3h4a3 3 0 0 1 3 3v2" stroke="white" strokeWidth="2" strokeLinecap="round" />
      <path d="M14 20.5l3.5 3.5L27 15" stroke="white" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}
