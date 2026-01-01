interface ConfidenceIndicatorProps {
  confidence: number
  label?: string
}

export function ConfidenceIndicator({ confidence, label = "Confidence" }: ConfidenceIndicatorProps) {
  const percentage = Math.round(confidence * 100)
  const isHigh = confidence >= 0.8
  const isMedium = confidence >= 0.6 && confidence < 0.8
  const isLow = confidence < 0.6

  return (
    <div className="flex flex-col gap-2">
      <div className="flex items-center justify-between">
        <span className="text-sm font-medium text-foreground">{label}</span>
        <span className="text-sm font-semibold text-foreground">{percentage}%</span>
      </div>
      <div className="h-2 bg-slate-200 rounded-full overflow-hidden">
        <div
          className={cn(
            "h-full transition-all",
            isHigh && "bg-green-600",
            isMedium && "bg-amber-600",
            isLow && "bg-red-600",
          )}
          style={{ width: `${percentage}%` }}
        />
      </div>
    </div>
  )
}

function cn(...classes: any[]) {
  return classes.filter(Boolean).join(" ")
}
