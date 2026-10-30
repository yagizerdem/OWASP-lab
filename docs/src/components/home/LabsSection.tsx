import { LABS, type Level } from "./data";
import { LEVEL_TONE, LabCard } from "./LabCard";

const LEVELS: Level[] = ["Beginner", "Intermediate", "Advanced"];

export function LabsSection() {
  return (
    <section
      id="labs"
      className="mx-auto w-full max-w-7xl scroll-mt-20 px-4 py-24 md:px-10"
    >
      <div className="mb-8 flex flex-col justify-between gap-4 md:flex-row md:items-end">
        <div>
          <h2 className="font-display text-3xl font-semibold tracking-[-0.02em] text-foreground">
            The labs
          </h2>
          <p className="mt-2 max-w-2xl font-mono text-[13px] leading-relaxed text-muted-foreground">
            All 10 OWASP Top 10 (2025) categories as isolated ASP.NET Core
            services you can attack and then fix.
          </p>
        </div>
        <ul
          aria-label="Difficulty levels"
          className="flex items-center gap-2 self-start rounded-xl bg-surface-low p-1.5 md:self-auto"
        >
          {LEVELS.map((level) => (
            <li
              key={level}
              className={`rounded px-2.5 py-1 font-mono text-[11px] font-semibold ${LEVEL_TONE[level].badge}`}
            >
              {level} ({LABS.filter((l) => l.level === level).length})
            </li>
          ))}
        </ul>
      </div>

      <div className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-5">
        {LABS.map((lab) => (
          <LabCard key={lab.id} lab={lab} />
        ))}
      </div>
    </section>
  );
}
