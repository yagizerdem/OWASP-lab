import { LABS, type Level } from "./data";
import { LabRow } from "./LabCard";

const LEVELS: Level[] = ["Beginner", "Intermediate", "Advanced"];

export function LabsSection() {
  return (
    <section
      id="labs"
      className="mx-auto w-full max-w-7xl scroll-mt-20 px-4 py-24 md:px-10"
    >
      <div className="mb-10 max-w-[62ch]">
        <h2 className="text-balance font-display text-3xl font-semibold tracking-[-0.02em] text-foreground">
          Ten labs, one per OWASP category
        </h2>
        <p className="mt-3 font-mono text-[13px] leading-relaxed text-muted-foreground">
          Each lab is its own ASP.NET Core service with a seeded database. Pick
          a row to open its source on GitHub; the port is where it listens once
          Docker Compose is up.
        </p>
      </div>

      <ul className="m-0! list-none border-t border-surface-highest p-0!">
        {LABS.map((lab) => (
          <li key={lab.id} className="border-b border-surface-highest">
            <LabRow lab={lab} />
          </li>
        ))}
      </ul>

      <p className="mt-4 font-mono text-xs text-muted-foreground">
        {LEVELS.map(
          (level) =>
            `${level}: ${LABS.filter((l) => l.level === level).length}`,
        ).join("   ")}
      </p>
    </section>
  );
}
