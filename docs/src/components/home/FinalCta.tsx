import { BookOpen, Play, Star } from "lucide-react";
import { Button } from "@/components/ui/button";
import { REPO_URL, labUrl } from "./data";

export function FinalCta() {
  return (
    <section className="mx-auto w-full max-w-7xl px-4 pb-24 md:px-10">
      <div className="grid grid-cols-1 items-end gap-8 border-t border-surface-highest pt-12 lg:grid-cols-12">
        <div className="lg:col-span-7">
          <h2 className="mb-3 max-w-[22ch] text-balance font-display text-3xl font-semibold tracking-[-0.02em] text-foreground">
            Start with the first category
          </h2>
          <p className="max-w-[56ch] font-mono text-[13px] leading-relaxed text-muted-foreground">
            Clone the repository, bring up the ten services, and work through
            the attack and the fix for each category at your own pace.
          </p>
        </div>

        <div className="flex flex-wrap items-center gap-3 lg:col-span-5 lg:justify-end">
          <Button asChild size="lg" className="h-11 px-6 font-mono bg-primary">
            <a href={REPO_URL} className="text-secondary-foreground! ">
              <Star />
              Star on GitHub
            </a>
          </Button>
          <Button asChild size="lg" className="h-11 px-6 font-mono bg-primary">
            <a
              href={`${REPO_URL}/tree/main/docs`}
              className="text-secondary-foreground!"
            >
              <BookOpen />
              Docs
            </a>
          </Button>
        </div>
      </div>
    </section>
  );
}
