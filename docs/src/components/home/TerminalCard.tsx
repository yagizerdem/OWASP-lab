import { useEffect, useRef, useState } from "react";
import { Check, CircleCheck, Copy } from "lucide-react";
import { Button } from "@/components/ui/button";
import { CLONE_COMMAND, LABS, REPO_URL } from "./data";

const PREVIEW_LABS = [LABS[0], LABS[1], LABS[4]];

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

  return (
    <div>
      <div className="overflow-hidden rounded-xl border border-surface-highest bg-surface-lowest shadow-xl">
        <div className="flex items-center justify-between border-b border-surface-highest bg-surface-low px-4 py-2.5">
          <div className="flex items-center gap-2">
            <span className="size-3 rounded-full bg-alert" />
            <span className="size-3 rounded-full bg-surface-highest" />
            <span className="size-3 rounded-full bg-primary" />
            <span className="ml-2 font-mono text-[11px] text-outline">
              bash ~ OWASP-lab
            </span>
          </div>
          <Button
            variant="ghost"
            size="xs"
            onClick={copy}
            aria-label="Copy the clone and start commands"
            className="font-mono text-[11px] text-muted-foreground"
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
            <Row name="sqlserver" status="Healthy" ok />
            {PREVIEW_LABS.map((lab) => (
              <Row
                key={lab.id}
                name={lab.folder.toLowerCase()}
                status={`:${lab.port}`}
              />
            ))}
            <div className="pl-4 text-[11px] text-outline">
              ... 6 more lab services
            </div>
          </div>

          <div className="flex items-center gap-2 border-t border-surface-high pt-3 font-semibold text-primary">
            <CircleCheck className="size-4" />
            10 vulnerable targets running on localhost
          </div>
        </div>
      </div>
      <div className="mt-3 flex items-center justify-between px-1 font-mono text-[11px] text-outline">
        <span>Engine: Docker Compose</span>
        <span>Runtime: .NET 10</span>
      </div>
    </div>
  );
}

function Line({ children, strong }: { children: React.ReactNode; strong?: boolean }) {
  return (
    <div
      className={`flex items-center gap-2 ${
        strong ? "text-foreground" : "text-muted-foreground"
      }`}
    >
      <span className="font-bold text-primary">$</span>
      <span className="whitespace-nowrap">{children}</span>
    </div>
  );
}

function Row({
  name,
  status,
  ok,
}: {
  name: string;
  status: string;
  ok?: boolean;
}) {
  return (
    <div className="flex items-center gap-2">
      <span className="font-bold text-primary">✔</span>
      <span className="text-outline">Container</span>
      <span className="whitespace-nowrap">{name}</span>
      <span
        className={`ml-auto ${ok ? "font-medium text-primary" : "text-tertiary"}`}
      >
        {ok ? `[${status}]` : status}
      </span>
    </div>
  );
}
