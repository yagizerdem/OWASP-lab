import { ArrowRight, BookOpen, TriangleAlert } from "lucide-react";
import { Button } from "@/components/ui/button";
import { TerminalCard } from "./TerminalCard";

export function Hero() {
  return (
    <section className="mx-auto w-full max-w-7xl px-4 pb-20 pt-12 md:px-10 md:pt-16">
      <div className="grid grid-cols-1 items-center gap-10 lg:grid-cols-12 lg:gap-12">
        <div className="flex min-w-0 flex-col items-start lg:col-span-7">
          <h1 className="mb-5 max-w-[16ch] text-balance font-display text-5xl font-semibold leading-[1.05] tracking-[-0.03em] text-foreground md:text-6xl">
            Break it. Understand it. Fix it.
          </h1>

          <p className="mb-8 max-w-[62ch] font-mono text-[15px] leading-relaxed text-muted-foreground">
            Ten intentionally vulnerable ASP.NET Core services, one for each
            category of the OWASP Top 10 (2025). Start them with Docker,
            exploit them from your terminal, then read the source and ship the
            fix.
          </p>

          <div className="flex flex-wrap items-center gap-3">
            <Button
              asChild
              size="lg"
              className="h-11 px-6 font-mono font-semibold"
            >
              {/* "!" keeps the label color on hover: Infima's unlayered a:hover rule would win otherwise. */}
              <a href="#labs" className="text-primary-foreground!">
                Browse the labs
                <ArrowRight />
              </a>
            </Button>
            <Button
              asChild
              size="lg"
              variant="secondary"
              className="h-11 px-6 font-mono"
            >
              <a href="#how-it-works" className="text-secondary-foreground!">
                <BookOpen />
                How it works
              </a>
            </Button>
          </div>

          <p className="mt-8 flex max-w-[62ch] items-start gap-2 font-mono text-xs leading-relaxed text-alert">
            <TriangleAlert className="mt-0.5 size-4 shrink-0" />
            The vulnerabilities are real. Run the labs locally and never expose
            them to the internet.
          </p>
        </div>

        <div className="w-full min-w-0 lg:col-span-5">
          <TerminalCard />
        </div>
      </div>
    </section>
  );
}
