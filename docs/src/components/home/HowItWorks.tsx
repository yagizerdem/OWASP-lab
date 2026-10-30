import type { ReactNode } from "react";

type Step = {
  title: string;
  rule: string;
  body: ReactNode;
};

const STEPS: Step[] = [
  {
    title: "Run",
    rule: "border-primary",
    body: (
      <>
        Start every lab with one{" "}
        <code className="rounded bg-surface-container px-1.5 py-0.5 text-xs text-primary">
          docker compose up
        </code>{" "}
        command. Each service starts with its own database and seeded dummy
        data, entirely on your machine.
      </>
    ),
  },
  {
    title: "Exploit",
    rule: "border-tertiary",
    body: "Intercept requests, craft malicious payloads, and watch flawed logic hand over accounts, data, or full query control. curl or any REST client is enough.",
  },
  {
    title: "Fix",
    rule: "border-alert",
    body: "Find the root cause in the source, write the secure ASP.NET Core version, and send the same payload again to confirm it no longer works.",
  },
];

export function HowItWorks() {
  return (
    <section
      id="how-it-works"
      className="mx-auto w-full max-w-7xl scroll-mt-20 px-4 py-24 md:px-10"
    >
      <h2 className="mb-12 max-w-[24ch] text-balance font-display text-3xl font-semibold tracking-[-0.02em] text-foreground">
        Every lab follows the same three steps
      </h2>

      <ol className="m-0! grid list-none p-0! grid-cols-1 gap-10 md:grid-cols-3 md:gap-8">
        {STEPS.map((step, i) => (
          <li key={step.title} className={`border-t-2 pt-5 ${step.rule}`}>
            <p className="mb-3 flex items-baseline gap-3">
              <span className="font-mono text-sm font-semibold tabular-nums text-muted-foreground">
                {i + 1}
              </span>
              <span className="font-display text-2xl font-semibold text-foreground">
                {step.title}
              </span>
            </p>
            <p className="max-w-[44ch] font-mono text-[13px] leading-relaxed text-muted-foreground">
              {step.body}
            </p>
          </li>
        ))}
      </ol>
    </section>
  );
}
