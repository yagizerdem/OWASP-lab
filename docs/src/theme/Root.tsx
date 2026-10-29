import type { ReactNode } from "react";
import { TooltipProvider } from "@/components/ui/tooltip";

export default function Root({ children }: { children: ReactNode }): ReactNode {
  return <TooltipProvider>{children}</TooltipProvider>;
}
