import type { NextConfig } from "next";

// A Node server (it holds the admin's session and talks to the API on their behalf), built as a self-contained
// .next/standalone folder for the Docker image.
const nextConfig: NextConfig = {
  output: "standalone",
  poweredByHeader: false,
};

export default nextConfig;
