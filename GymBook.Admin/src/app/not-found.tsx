import Link from "next/link";

export default function NotFound() {
  return (
    <div className="page">
      <div className="card" style={{ display: "grid", gap: 12, justifyItems: "start" }}>
        <h1>Page not found</h1>
        <p className="subtle">That page doesn&apos;t exist.</p>
        <Link href="/" className="btn btn-primary">
          Back to dashboard
        </Link>
      </div>
    </div>
  );
}
