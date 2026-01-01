import type { DecisionEvent } from "@/lib/types"

interface DecisionStatePanelProps {
  event: DecisionEvent | null
  timestamp: string
}

export function DecisionStatePanel({ event, timestamp }: DecisionStatePanelProps) {
  if (!event) {
    return (
      <div className="h-full flex items-center justify-center">
        <p className="text-muted-foreground">No event selected</p>
      </div>
    )
  }

  return (
    <div className="space-y-4 overflow-y-auto max-h-full pr-4">
      <div>
        <h3 className="text-xs font-semibold text-muted-foreground uppercase mb-2">Event Type</h3>
        <p className="text-sm font-medium text-foreground">{event.eventType.replace(/_/g, " ")}</p>
      </div>

      <div>
        <h3 className="text-xs font-semibold text-muted-foreground uppercase mb-2">Timestamp</h3>
        <p className="text-xs font-mono text-foreground">{new Date(event.timestamp).toLocaleString()}</p>
      </div>

      {event.payload.inputs && (
        <div>
          <h3 className="text-xs font-semibold text-muted-foreground uppercase mb-2">Inputs</h3>
          <div className="bg-slate-50 rounded-md p-3 space-y-2 text-xs">
            {Object.entries(event.payload.inputs).map(([key, value]) => (
              <div key={key} className="flex justify-between items-center">
                <span className="text-muted-foreground">{key}</span>
                <span className="font-mono text-foreground">{String(value)}</span>
              </div>
            ))}
          </div>
        </div>
      )}

      {event.payload.rules && (
        <div>
          <h3 className="text-xs font-semibold text-muted-foreground uppercase mb-2">Rules Evaluated</h3>
          <div className="space-y-2">
            {event.payload.rules.map((rule: any, idx: number) => (
              <div key={idx} className="flex items-center gap-2 p-2 bg-slate-50 rounded-md">
                <span className={rule.passed ? "text-green-600" : "text-red-600"}>{rule.passed ? "✓" : "✕"}</span>
                <span className="text-xs font-medium text-foreground flex-1">{rule.name}</span>
                {rule.value !== undefined && <span className="text-xs text-muted-foreground">{rule.value}</span>}
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  )
}
