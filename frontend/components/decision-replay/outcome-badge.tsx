import type { DecisionOutcome } from "@/lib/api-types"
import { cn } from "@/lib/utils"

interface OutcomeBadgeProps {
  outcome: DecisionOutcome
  size?: "sm" | "md"
}

export function OutcomeBadge({ outcome, size = "md" }: OutcomeBadgeProps) {
  const baseStyles = "inline-flex items-center justify-center font-medium rounded-md"
  const sizeStyles = {
    sm: "px-2 py-1 text-xs",
    md: "px-3 py-1.5 text-sm",
  }

  const outcomeStyles: Record<DecisionOutcome, string> = {
    Draft: "bg-slate-100 text-slate-700",
    Feasible: "bg-green-100 text-green-800",
    RiskyButPossible: "bg-yellow-100 text-yellow-800",
    NeedsAdjustment: "bg-orange-100 text-orange-800",
    Committed: "bg-blue-100 text-blue-800",
  }

  const outcomeLabels: Record<DecisionOutcome, string> = {
    Draft: "⊙ Draft",
    Feasible: "✓ Feasible",
    RiskyButPossible: "⚠ Risky",
    NeedsAdjustment: "↻ Needs Adjustment",
    Committed: "✓ Committed",
  }

  return (
    <span className={cn(baseStyles, sizeStyles[size], outcomeStyles[outcome])}>
      {outcomeLabels[outcome]}
    </span>
  )
}
