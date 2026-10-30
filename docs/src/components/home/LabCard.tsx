import { ArrowRight } from "lucide-react";
import { labUrl, type Lab, type Level } from "./data";

const LEVEL_TEXT: Record<Level, string> = {
  Beginner: "text-primary",
  Intermediate: "text-tertiary",
  Advanced: "text-alert",
};

/** One lab as a catalog row: id, what it teaches, difficulty, port. */
export function LabRow({ lab }: { lab: Lab }) {
  return (
    <a
      href={labUrl(lab.folder)}
      data-row-link
      className="group grid grid-cols-[3rem_1fr_auto] items-baseline gap-x-4 gap-y-1 rounded-sm px-2 py-5 outline-none transition-colors hover:bg-surface-low focus-visible:ring-[3px] focus-visible:ring-ring/50 md:grid-cols-[4rem_minmax(0,1fr)_7.5rem_5rem_1.5rem] md:gap-x-6 md:px-4"
    >
      <span className="font-mono text-sm font-semibold tabular-nums text-muted-foreground">
        {lab.id}
      </span>

      <span className="min-w-0">
        <span className="block font-display text-lg font-semibold leading-snug text-foreground">
          {lab.title}
        </span>
        <span className="mt-1 block max-w-[68ch] font-mono text-xs leading-relaxed text-muted-foreground">
          {lab.description}
        </span>
      </span>

      <span
        className={`col-start-2 font-mono text-xs font-semibold md:col-start-auto ${LEVEL_TEXT[lab.level]}`}
      >
        {lab.level}
      </span>

      <span className="col-start-3 row-start-1 text-right font-mono text-xs tabular-nums text-muted-foreground md:col-start-auto md:row-start-auto">
        :{lab.port}
      </span>

      <ArrowRight
        aria-hidden
        className="hidden size-4 self-center text-muted-foreground transition-transform group-hover:translate-x-1 group-hover:text-foreground motion-reduce:transition-none md:block"
      />
    </a>
  );
}
