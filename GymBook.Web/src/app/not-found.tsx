import Link from "next/link";

export default function NotFound() {
  return (
    <article className="prose" style={{ textAlign: "center" }}>
      <h1>Page not found</h1>
      <p className="updated">That page doesn&apos;t exist - maybe it skipped leg day.</p>
      <Link href="/" className="btn btn-primary">
        Back to home
      </Link>
    </article>
  );
}
