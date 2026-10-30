import { BookOpen, Play, Star } from "lucide-react";
import { Button } from "@/components/ui/button";
import { REPO_URL, labUrl } from "./data";

export function FinalCta() {
  return (
    <section className="mx-auto w-full max-w-7xl px-4 pb-24 md:px-10">
      <div className="rounded-xl border border-surface-highest bg-surface-low p-8 shadow-xl md:p-14">
        <div className="max-w-3xl">
          <h2 className="mb-4 font-display text-3xl font-semibold tracking-[-0.02em] text-foreground">
            Ready to test your defensive instincts?
          </h2>
          <p className="mb-8 font-mono text-[15px] leading-relaxed text-muted-foreground">
            Clone the repository, start the ten services, and work through the
            attack and the fix for each category.
          </p>
          <div className="flex flex-wrap items-center gap-3">
            <Button
              asChild
              size="lg"
              className="h-11 px-6 font-mono font-semibold"
            >
              <a
                href={labUrl("BrokenAccessControl")}
                className="text-secondary!"
              >
                <Play />
                Start with A01
              </a>
            </Button>
            <Button
              asChild
              size="lg"
              variant="secondary"
              className="h-11 px-6 font-mono"
            >
              <a href={REPO_URL}>
                <Star />
                Star on GitHub
              </a>
            </Button>
            <Button
              asChild
              size="lg"
              variant="secondary"
              className="h-11 px-6 font-mono"
            >
              <a href={`${REPO_URL}/tree/main/docs`}>
                <BookOpen />
                Read the docs source
              </a>
            </Button>
          </div>
        </div>
      </div>
    </section>
  );
}
