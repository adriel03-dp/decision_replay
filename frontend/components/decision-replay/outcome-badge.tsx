import type { DecisionOutcome } from "@/lib/types"
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

  const outcomeStyles = {
    APPROVED: "bg-green-100 text-green-800",
    REJECTED: "bg-red-100 text-red-800",
    PENDING: "bg-slate-100 text-slate-700",
  }

  return (
    <span className={cn(baseStyles, sizeStyles[size], outcomeStyles[outcome])}>
      {outcome === "APPROVED" && "✓ Approved"}
      {outcome === "REJECTED" && "✕ Rejected"}
      {outcome === "PENDING" && "⊙ Pending"}
    </span>
  )
}
