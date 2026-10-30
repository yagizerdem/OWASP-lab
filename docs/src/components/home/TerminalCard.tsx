import { useEffect, useRef, useState } from "react";
import { Check, CircleCheck, Copy } from "lucide-react";
import { Button } from "@/components/ui/button";
import { CLONE_COMMAND, LABS, REPO_URL } from "./data";

const PREVIEW_LABS = [LABS[0], LABS[1], LABS[4]];

/** The page's one authored motion: the compose output prints in line by line. */
const reveal = (step: number) => ({
  className:
    "animate-in fade-in duration-500 fill-mode-backwards motion-reduce:animate-none",
  style: { animationDelay: `${300 + step * 160}ms` },
});

export function TerminalCard() {
  const [copied, setCopied] = useState(false);
  const timer = useRef<ReturnType<typeof setTimeout> | undefined>(undefined);

  useEffect(() => () => clearTimeout(timer.current), []);

  const copy = async () => {
    try {
      await navigator.clipboard.writeText(CLONE_COMMAND);
      setCopied(true);
      clearTimeout(timer.current);
      timer.current = setTimeout(() => setCopied(false), 2000);
    } catch {
      setCopied(false);
    }
  };

  const rows = [
    { name: "sqlserver", status: "healthy", ok: true },
    ...PREVIEW_LABS.map((lab) => ({
      name: lab.folder.toLowerCase(),
      status: `:${lab.port}`,
      ok: false,
    })),
  ];

  return (
    <div>
      <div className="overflow-hidden rounded-xl border border-surface-highest bg-surface-lowest shadow-[0_12px_32px_-12px_rgba(0,0,0,0.25)]">
        <div className="flex items-center justify-between border-b border-surface-highest bg-surface-low px-4 py-2">
          <span className="font-mono text-xs text-muted-foreground">
            bash ~ OWASP-lab
          </span>
          <Button
            variant="ghost"
            size="xs"
            onClick={copy}
            aria-label="Copy the clone and start commands"
            className="font-mono text-xs text-muted-foreground"
          >
            {copied ? <Check /> : <Copy />}
            {copied ? "Copied" : "Copy"}
          </Button>
        </div>

        <div className="space-y-2 overflow-x-auto p-6 font-mono text-[13px] text-foreground">
          <Line>git clone {REPO_URL}.git</Line>
          <Line>cd OWASP-lab</Line>
          <Line strong>docker compose up -d</Line>

          <div className="space-y-1.5 pt-2 text-xs">
            {rows.map((row, i) => (
              <Row key={row.name} {...row} anim={reveal(i)} />
            ))}
            <div
              className={`pl-6 text-xs text-muted-foreground ${reveal(rows.length).className}`}
              style={reveal(rows.length).style}
            >
              ... 6 more lab services
            </div>
          </div>

          <div
            className={`flex items-center gap-2 border-t border-surface-high pt-3 font-semibold text-primary ${reveal(rows.length + 1).className}`}
            style={reveal(rows.length + 1).style}
          >
            <CircleCheck className="size-4" />
            10 vulnerable targets running on localhost
          </div>
        </div>
      </div>
      <p className="mt-3 px-1 font-mono text-xs text-muted-foreground">
        Needs Docker Compose. Services run on .NET 10 with SQL Server.
      </p>
    </div>
  );
}

function Line({
  children,
  strong,
}: {
  children: React.ReactNode;
  strong?: boolean;
}) {
  return (
    <div
      className={`flex items-center gap-2 ${
        strong ? "text-foreground" : "text-muted-foreground"
      }`}
    >
      <span aria-hidden className="font-bold text-primary">
        $
      </span>
      <span className="whitespace-nowrap">{children}</span>
    </div>
  );
}

function Row({
  name,
  status,
  ok,
  anim,
}: {
  name: string;
  status: string;
  ok: boolean;
  anim: { className: string; style: React.CSSProperties };
}) {
  return (
    <div
      className={`flex items-center gap-2 ${anim.className}`}
      style={anim.style}
    >
      <Check className="size-3.5 shrink-0 text-primary" aria-hidden />
      <span className="text-muted-foreground">Container</span>
      <span className="whitespace-nowrap">{name}</span>
      <span
        className={`ml-auto tabular-nums ${ok ? "font-medium text-primary" : "text-tertiary"}`}
      >
        {status}
      </span>
    </div>
  );
}
