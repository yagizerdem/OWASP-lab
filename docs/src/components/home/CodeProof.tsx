import { Check } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";

const POINTS = [
  "Full source code and git history for every lab",
  "Plain HTTP endpoints you can hit with curl or any REST client",
  "Seeded dummy data so every exploit has something to find",
];

const kw = "text-primary";
const str = "text-tertiary";
const note = "text-outline";

export function CodeProof() {
  return (
    <section className="w-full border-y border-surface-highest bg-surface-lowest py-20">
      <div className="mx-auto grid max-w-7xl grid-cols-1 items-center gap-8 px-4 md:px-10 lg:grid-cols-12">
        <div className="lg:col-span-5">
          <h2 className="mb-4 font-display text-3xl font-semibold tracking-[-0.02em] text-foreground">
            Real code, real vulnerabilities
          </h2>
          <p className="mb-6 font-mono text-[13px] leading-relaxed text-muted-foreground">
            No quizzes and no mock data. Each lab is a normal ASP.NET Core
            service running against a live SQL Server, so what you exploit is
            what you then fix.
          </p>
          <ul className="space-y-3 font-mono text-xs text-foreground">
            {POINTS.map((point) => (
              <li key={point} className="flex items-center gap-3">
                <span className="flex size-5 shrink-0 items-center justify-center rounded bg-primary/10 text-primary">
                  <Check className="size-3.5" />
                </span>
                {point}
              </li>
            ))}
          </ul>
        </div>

        <Card className="gap-0 rounded-xl border border-surface-high bg-surface-container p-6 shadow-xl lg:col-span-7">
          <div className="mb-3 flex flex-wrap items-center justify-between gap-2 border-b border-surface-highest pb-3">
            <span className="font-mono text-[11px] text-muted-foreground">
              Injection / BlogController.cs
            </span>
            <Badge className="rounded bg-alert/10 font-mono text-[11px] font-semibold text-alert">
              CWE-89: SQL injection
            </Badge>
          </div>
          <pre
            tabIndex={0}
            aria-label="Vulnerable and fixed C# search endpoint"
            className="overflow-x-auto font-mono text-[13px] leading-6 text-foreground"
          >
            <code>
              <span className={note}>
                {"// Vulnerable: the search term is concatenated into the query\n"}
              </span>
              <span className={kw}>var</span>
              {" query = "}
              <span className={str}>
                {"$\"SELECT ... FROM Blogs b WHERE b.Title LIKE '%{title}%'\""}
              </span>
              {";\n"}
              <span className={kw}>var</span>
              {" data = _context.Database\n    .SqlQueryRaw<BlogAuthorDTO>(query).ToList();\n\n"}
              <span className={note}>
                {"// Fixed: EF Core turns the value into a SQL parameter\n"}
              </span>
              <span className={kw}>var</span>
              {" data = _context.Blogs\n    .Where(b => b.Title.Contains(title))\n    .ToList();"}
            </code>
          </pre>
        </Card>
      </div>
    </section>
  );
}
