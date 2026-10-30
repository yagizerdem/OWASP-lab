import { ArrowRight, BookOpen, TriangleAlert } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { TerminalCard } from "./TerminalCard";

const STATS = [
  { value: "10/10", label: "OWASP vectors", accent: false },
  { value: "10", label: "Isolated labs", accent: false },
  { value: "Local", label: "Docker sandbox", accent: true },
];

export function Hero() {
  return (
    <section className="mx-auto w-full max-w-7xl px-4 pb-20 pt-10 md:px-10">
      <div className="grid grid-cols-1 items-center gap-8 lg:grid-cols-12">
        <div className="flex flex-col items-start lg:col-span-7">
          <Badge
            variant="outline"
            className="mb-6 h-auto gap-2 whitespace-normal rounded-full border-alert/30 bg-alert/10 px-3 py-1.5 font-mono text-[11px] text-alert"
          >
            <TriangleAlert />
            For educational use only. Run locally, never expose to the internet.
          </Badge>

          <h1 className="mb-4 font-display text-4xl font-semibold leading-[1.1] tracking-[-0.03em] text-foreground md:text-5xl">
            Break it. Understand it.{" "}
            <span className="text-primary">Fix it.</span>
          </h1>

          <p className="mb-8 max-w-2xl font-mono text-[15px] leading-relaxed text-muted-foreground">
            A hands-on training lab covering the OWASP Top 10 (2025). Exploit
            intentionally vulnerable ASP.NET Core services locally, inspect the
            root cause, and ship the hardened fix.
          </p>

          <div className="flex flex-wrap items-center gap-3">
            <Button
              size="lg"
              asChild
              className="h-11 px-6 font-mono font-semibold "
            >
              <a href="#labs" className="text-secondary!">
                Get started
                <ArrowRight />
              </a>
            </Button>
            <Button
              asChild
              size="lg"
              variant="secondary"
              className="h-11 px-6 font-mono"
            >
              <a href="#how-it-works">
                <BookOpen />
                How it works
              </a>
            </Button>
            <span className="ml-0 flex items-center gap-2 font-mono text-[11px] text-outline sm:ml-3">
              <span className="inline-block size-2 animate-pulse rounded-full bg-primary motion-reduce:animate-none" />
              Docker Compose ready
            </span>
          </div>

          <dl className="mt-10 grid w-full max-w-lg grid-cols-3 gap-6 border-t border-surface-highest pt-8">
            {STATS.map((s) => (
              <div key={s.label} className="flex flex-col-reverse">
                <dt className="font-mono text-[11px] text-outline">
                  {s.label}
                </dt>
                <dd
                  className={`font-display text-2xl font-semibold ${
                    s.accent ? "text-primary" : "text-foreground"
                  }`}
                >
                  {s.value}
                </dd>
              </div>
            ))}
          </dl>
        </div>

        <div className="w-full lg:col-span-5">
          <TerminalCard />
        </div>
      </div>
    </section>
  );
}
