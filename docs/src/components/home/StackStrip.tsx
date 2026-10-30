import { Boxes, Database, Library, Server, SquareCode } from "lucide-react";
import type { LucideIcon } from "lucide-react";
import { Badge } from "@/components/ui/badge";

const STACK: { label: string; icon: LucideIcon; tone: string }[] = [
  { label: "ASP.NET Core (.NET 10)", icon: SquareCode, tone: "text-primary" },
  { label: "Entity Framework Core", icon: Database, tone: "text-tertiary" },
  { label: "Microsoft SQL Server", icon: Server, tone: "text-outline" },
  { label: "Docker & Compose", icon: Boxes, tone: "text-primary" },
  { label: "Docusaurus guides", icon: Library, tone: "text-tertiary" },
];

export function StackStrip() {
  return (
    <section className="w-full border-y border-surface-highest bg-surface-lowest py-6">
      <div className="mx-auto flex max-w-7xl flex-col gap-4 px-4 md:flex-row md:items-center md:justify-between md:px-10">
        <p className="font-mono text-[11px] text-outline">Built with</p>
        <ul className="flex flex-wrap items-center gap-3">
          {STACK.map(({ label, icon: Icon, tone }) => (
            <li key={label}>
              <Badge
                variant="secondary"
                className="h-auto gap-2 rounded-lg bg-surface-container px-3 py-1.5 font-mono text-[11px] font-medium text-foreground"
              >
                <Icon className={`${tone} !size-4`} />
                {label}
              </Badge>
            </li>
          ))}
        </ul>
      </div>
    </section>
  );
}
