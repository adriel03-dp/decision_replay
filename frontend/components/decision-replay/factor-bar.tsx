interface FactorBarProps {
  name: string
  weight: number
  impact: "positive" | "negative" | "neutral"
}

export function FactorBar({ name, weight, impact }: FactorBarProps) {
  const percentage = Math.round(weight * 100)
  const impactColors = {
    positive: "bg-green-600",
    negative: "bg-red-600",
    neutral: "bg-slate-400",
  }

  return (
    <div className="flex flex-col gap-1.5">
      <div className="flex items-center justify-between">
        <span className="text-sm font-medium text-foreground">{name}</span>
        <span className="text-xs text-muted-foreground">{percentage}%</span>
      </div>
      <div className="h-1.5 bg-slate-200 rounded-full overflow-hidden">
        <div className={`h-full transition-all ${impactColors[impact]}`} style={{ width: `${percentage}%` }} />
      </div>
    </div>
  )
}
