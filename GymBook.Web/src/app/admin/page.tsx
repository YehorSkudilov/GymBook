import type { Metadata } from "next";
import { AdminApp } from "./AdminApp";

// The web admin: the admin app's screens in the browser, for admin accounts only (the API checks every call).
export const metadata: Metadata = {
  title: "Admin",
  robots: { index: false, follow: false },
};

export default function AdminPage() {
  return <AdminApp />;
}
