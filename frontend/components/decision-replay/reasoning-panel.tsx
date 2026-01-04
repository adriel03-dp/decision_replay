import type { DecisionEvent } from "@/lib/api-types"
import { ConfidenceIndicator } from "./confidence-indicator"
import { FactorBar } from "./factor-bar"

interface ReasoningPanelProps {
  event: DecisionEvent | null
}

export function ReasoningPanel({ event }: ReasoningPanelProps) {
  if (!event || !event.payload.reasoning) {
    return (
      <div className="h-full flex items-center justify-center">
        <p className="text-muted-foreground">No reasoning available</p>
      </div>
    )
  }

  const { reasoning, confidence, source, generatedAt, factors } = event.payload

  return (
    <div className="space-y-4 overflow-y-auto max-h-full pr-4">
      <div>
        <h3 className="text-xs font-semibold text-muted-foreground uppercase mb-2">Reasoning</h3>
        <p className="text-sm text-foreground leading-relaxed">{reasoning}</p>
      </div>

      {confidence !== undefined && (
        <div>
          <ConfidenceIndicator confidence={confidence} label="Model Confidence" />
        </div>
      )}

      <div className="grid grid-cols-2 gap-4 text-xs">
        <div>
          <p className="text-muted-foreground font-semibold uppercase mb-1">Source</p>
          <p className="text-foreground font-mono">{source}</p>
        </div>
        <div>
          <p className="text-muted-foreground font-semibold uppercase mb-1">Generated</p>
          <p className="text-foreground">{new Date(generatedAt).toLocaleString()}</p>
        </div>
      </div>

      {factors && factors.length > 0 && (
        <div>
          <h3 className="text-xs font-semibold text-muted-foreground uppercase mb-3">Factor Influence</h3>
          <div className="space-y-2.5">
            {factors.map((factor: any) => (
              <FactorBar key={factor.name} name={factor.name} weight={factor.weight} impact={factor.impact} />
            ))}
          </div>
        </div>
      )}

      {event.payload.overriddenBy && (
        <div className="p-3 bg-amber-50 border border-amber-200 rounded-md">
          <p className="text-xs font-semibold text-amber-900 mb-2">Human Override</p>
          <p className="text-xs text-amber-800">{event.payload.reason}</p>
          <p className="text-xs text-amber-600 mt-2">
            Overridden by {event.payload.overriddenBy} at {new Date(event.payload.overriddenAt).toLocaleString()}
          </p>
        </div>
      )}
    </div>
  )
}
