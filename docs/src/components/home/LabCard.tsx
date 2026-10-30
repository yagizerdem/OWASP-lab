import { ArrowRight } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardFooter } from "@/components/ui/card";
import { labUrl, type Lab, type Level } from "./data";

export const LEVEL_TONE: Record<
  Level,
  { badge: string; title: string; footer: string }
> = {
  Beginner: {
    badge: "bg-primary/10 text-primary",
    title: "group-hover:text-primary",
    footer: "group-hover:text-primary",
  },
  Intermediate: {
    badge: "bg-tertiary/10 text-tertiary",
    title: "group-hover:text-tertiary",
    footer: "group-hover:text-tertiary",
  },
  Advanced: {
    badge: "bg-alert/10 text-alert",
    title: "group-hover:text-alert",
    footer: "group-hover:text-alert",
  },
};

export function LabCard({ lab }: { lab: Lab }) {
  const tone = LEVEL_TONE[lab.level];
  return (
    <a
      href={labUrl(lab.folder)}
      className="group flex rounded-xl outline-none focus-visible:ring-[3px] focus-visible:ring-ring/50"
    >
      <Card className="w-full justify-between gap-0 rounded-xl border-0 bg-surface-container py-0 shadow-sm transition-colors group-hover:bg-surface-high">
        <CardContent className="p-6">
          <div className="mb-4 flex items-center justify-between">
            <Badge
              className={`rounded font-mono text-[11px] font-semibold ${tone.badge}`}
            >
              {lab.id}
            </Badge>
            <Badge className={`rounded font-mono text-[11px] ${tone.badge}`}>
              {lab.level}
            </Badge>
          </div>
          <h3
            className={`mb-2 font-display text-lg font-semibold leading-snug text-foreground transition-colors ${tone.title}`}
          >
            {lab.title}
          </h3>
          <p className="font-mono text-xs leading-relaxed text-muted-foreground">
            {lab.description}
          </p>
        </CardContent>
        <CardFooter
          className={`mx-6 mb-0 justify-between border-t border-surface-highest py-4 font-mono text-[11px] font-semibold text-outline transition-colors ${tone.footer}`}
        >
          <span>Open lab on :{lab.port}</span>
          <ArrowRight className="size-4 transition-transform group-hover:translate-x-1 motion-reduce:transition-none" />
        </CardFooter>
      </Card>
    </a>
  );
}
