import { Terminal, Crosshair, ShieldCheck, CircleCheck } from "lucide-react";
import type { LucideIcon } from "lucide-react";
import { Card, CardContent, CardFooter } from "@/components/ui/card";

type Step = {
  title: string;
  icon: LucideIcon;
  tone: string;
  body: React.ReactNode;
  note: string;
};

const STEPS: Step[] = [
  {
    title: "Run",
    icon: Terminal,
    tone: "bg-primary/10 text-primary",
    body: (
      <>
        Start every lab with one{" "}
        <code className="rounded bg-surface-lowest px-1.5 py-0.5 text-xs text-primary">
          docker compose up
        </code>{" "}
        command. Each service starts with its own database and seeded dummy
        data.
      </>
    ),
    note: "Runs entirely on your machine",
  },
  {
    title: "Exploit",
    icon: Crosshair,
    tone: "bg-tertiary/10 text-tertiary",
    body: "Intercept requests, craft malicious payloads, and watch flawed logic hand over accounts, data, or full query control.",
    note: "Works with curl or any REST client",
  },
  {
    title: "Fix",
    icon: ShieldCheck,
    tone: "bg-alert/10 text-alert",
    body: "Find the root cause in the source, write the secure ASP.NET Core version, and re-run the same payload to confirm it no longer works.",
    note: "Same payload, different result",
  },
];

export function HowItWorks() {
  return (
    <section
      id="how-it-works"
      className="mx-auto w-full max-w-7xl scroll-mt-20 px-4 py-24 md:px-10"
    >
      <div className="mx-auto mb-12 max-w-3xl text-center">
        <h2 className="font-display text-3xl font-semibold tracking-[-0.02em] text-foreground">
          How it works
        </h2>
        <p className="mt-2 font-mono text-[13px] text-muted-foreground">
          Three steps, from starting a target to patching it.
        </p>
      </div>

      <ol className="grid grid-cols-1 gap-6 md:grid-cols-3">
        {STEPS.map((step, i) => (
          <li key={step.title} className="flex">
            <Card className="w-full justify-between gap-0 rounded-xl border-0 bg-surface-container py-0 shadow-md">
              <CardContent className="p-8">
                <div className="mb-6 flex items-center justify-between">
                  <span
                    className={`flex size-12 items-center justify-center rounded-xl ${step.tone}`}
                  >
                    <step.icon className="size-6" />
                  </span>
                  <span className="font-display text-2xl font-semibold text-outline">
                    {String(i + 1).padStart(2, "0")}
                  </span>
                </div>
                <h3 className="mb-3 font-display text-2xl font-medium text-foreground">
                  {step.title}
                </h3>
                <p className="font-mono text-[13px] leading-relaxed text-muted-foreground">
                  {step.body}
                </p>
              </CardContent>
              <CardFooter className="mx-8 justify-start gap-2 border-t border-surface-highest py-4 font-mono text-[11px] text-outline">
                <CircleCheck className="size-4 text-primary" />
                {step.note}
              </CardFooter>
            </Card>
          </li>
        ))}
      </ol>
    </section>
  );
}
