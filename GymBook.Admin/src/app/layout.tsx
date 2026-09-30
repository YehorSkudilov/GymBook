import type { Metadata, Viewport } from "next";
import localFont from "next/font/local";
import { Shell } from "@/components/Shell";
import "./globals.css";

// The same Open Sans files the app ships (GymBook/Resources/Fonts).
const openSans = localFont({
  src: [
    { path: "../fonts/OpenSans-Regular.ttf", weight: "400", style: "normal" },
    { path: "../fonts/OpenSans-Semibold.ttf", weight: "600", style: "normal" },
  ],
  variable: "--font-open-sans",
  display: "swap",
});

export const metadata: Metadata = {
  title: { default: "Gym Book Admin", template: "%s | Gym Book Admin" },
  robots: { index: false, follow: false },
};

export const viewport: Viewport = {
  themeColor: "#0B0D12",
};

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html lang="en" className={openSans.variable}>
      <body>
        <Shell>{children}</Shell>
      </body>
    </html>
  );
}
