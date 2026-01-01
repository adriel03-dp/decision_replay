"use client"

import type { EventType } from "@/lib/types"

interface DecisionEventMarkerProps {
  eventType: EventType
  timestamp: string
  isActive?: boolean
  onClick?: () => void
}

export function DecisionEventMarker({ eventType, timestamp, isActive = false, onClick }: DecisionEventMarkerProps) {
  const eventIcons = {
    INPUT_CAPTURED: "📥",
    RULE_EVALUATED: "✓",
    AI_REASONING: "🤖",
    HUMAN_OVERRIDE: "👤",
    DECISION_FINALIZED: "✔",
  }

  const eventLabels = {
    INPUT_CAPTURED: "Input",
    RULE_EVALUATED: "Rules",
    AI_REASONING: "AI",
    HUMAN_OVERRIDE: "Override",
    DECISION_FINALIZED: "Finalized",
  }

  return (
    <button
      onClick={onClick}
      className={`flex flex-col items-center gap-1 cursor-pointer transition-all ${
        isActive ? "opacity-100" : "opacity-60 hover:opacity-80"
      }`}
      title={`${eventLabels[eventType]} - ${new Date(timestamp).toLocaleTimeString()}`}
    >
      <div
        className={`w-3 h-3 rounded-full transition-all ${
          isActive ? "bg-primary ring-2 ring-primary ring-offset-2" : "bg-slate-300"
        }`}
      />
      <span className="text-xs font-medium text-muted-foreground">{eventIcons[eventType]}</span>
      <span className="text-xs text-muted-foreground">
        {new Date(timestamp).toLocaleTimeString([], {
          hour: "2-digit",
          minute: "2-digit",
        })}
      </span>
    </button>
  )
}
