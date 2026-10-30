const STACK = [
  "ASP.NET Core (.NET 10)",
  "Entity Framework Core",
  "Microsoft SQL Server",
  "Docker & Compose",
];

export function StackStrip() {
  return (
    <section
      aria-label="Technology"
      className="w-full border-y border-surface-highest bg-surface-lowest py-5 items-center flex"
    >
      <div className="mx-auto max-w-7xl px-4 md:px-10">
        <ul className="m-0! flex list-none flex-wrap p-0! items-center gap-x-8 gap-y-2 font-mono text-xs text-muted-foreground">
          {STACK.map((item) => (
            <li key={item}>{item}</li>
          ))}
        </ul>
      </div>
    </section>
  );
}
