import { Check } from "lucide-react";

const POINTS = [
  "Full source code and git history for every lab",
  "Plain HTTP endpoints you can hit with curl or any REST client",
  "Seeded dummy data so every exploit has something to find",
];

const kw = "text-primary";
const str = "text-tertiary";
const note = "text-muted-foreground italic";

export function CodeProof() {
  return (
    <section className="w-full border-y border-surface-highest bg-surface-lowest py-20">
      <div className="mx-auto grid max-w-7xl grid-cols-1 items-center gap-10 px-4 md:px-10 lg:grid-cols-12 lg:gap-12">
        <div className="min-w-0 lg:col-span-5">
          <h2 className="mb-4 text-balance font-display text-3xl font-semibold tracking-[-0.02em] text-foreground">
            Real code, real vulnerabilities
          </h2>
          <p className="mb-6 max-w-[62ch] font-mono text-[13px] leading-relaxed text-muted-foreground">
            No quizzes and no mock data. Each lab is a normal ASP.NET Core
            service running against a live SQL Server, so what you exploit is
            what you then fix.
          </p>
          <ul className="m-0! list-none space-y-3 p-0! font-mono text-xs text-foreground">
            {POINTS.map((point) => (
              <li key={point} className="flex items-start gap-3">
                <Check
                  aria-hidden
                  className="mt-0.5 size-4 shrink-0 text-primary"
                />
                {point}
              </li>
            ))}
          </ul>
        </div>

        <figure className="overflow-hidden rounded-xl border border-surface-high bg-surface-container shadow-[0_12px_32px_-12px_rgba(0,0,0,0.25)] min-w-0 lg:col-span-7">
          <figcaption className="flex flex-wrap items-center justify-between gap-2 border-b border-surface-highest px-5 py-3 font-mono text-xs">
            <span className="text-muted-foreground">
              Injection / BlogController.cs
            </span>
            <span className="font-semibold text-alert">
              CWE-89: SQL injection
            </span>
          </figcaption>
          <pre
            tabIndex={0}
            aria-label="Vulnerable and fixed C# search endpoint"
            className="overflow-x-auto p-5 font-mono text-[13px] leading-6 text-foreground"
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
        </figure>
      </div>
    </section>
  );
}
