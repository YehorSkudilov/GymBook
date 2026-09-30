import type { NextConfig } from "next";

// A plain static site (the `out` folder), served by nginx - see Dockerfile.
const nextConfig: NextConfig = {
  output: "export",
  // /privacy is emitted as /privacy/index.html, so any static server finds it without rewrites.
  trailingSlash: true,
};

export default nextConfig;
