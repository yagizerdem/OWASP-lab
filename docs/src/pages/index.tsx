import type { ReactNode } from "react";
import Layout from "@theme/Layout";
import {
  CodeProof,
  FinalCta,
  Hero,
  HowItWorks,
  LabsSection,
  StackStrip,
} from "@/components/home";

export default function Home(): ReactNode {
  return (
    <Layout
      title="Hands-on web app security training"
      description="Exploit and fix intentionally vulnerable ASP.NET Core labs covering the OWASP Top 10 (2025)."
    >
      <main>
        <Hero />
        <StackStrip />
        <LabsSection />
        <CodeProof />
        <HowItWorks />
        <FinalCta />
      </main>
    </Layout>
  );
}
