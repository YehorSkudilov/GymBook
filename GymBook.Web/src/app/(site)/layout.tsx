import { Footer } from "@/components/Footer";
import { Header } from "@/components/Header";

// The public site's pages, with its header and footer. The web admin (/admin) has its own chrome.
export default function SiteLayout({ children }: LayoutProps<"/">) {
  return (
    <>
      <Header />
      <main>{children}</main>
      <Footer />
    </>
  );
}
