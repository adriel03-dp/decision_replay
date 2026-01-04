"use client"

import { useState } from "react"
import type { DecisionEvent } from "@/lib/api-types"
import { DecisionEventMarker } from "./decision-event-marker"

interface TimelineScrubberProps {
  events: DecisionEvent[]
  currentIndex: number
  onIndexChange: (index: number) => void
}

export function TimelineScrubber({ events, currentIndex, onIndexChange }: TimelineScrubberProps) {
  const [isDragging, setIsDragging] = useState(false)

  if (events.length === 0) {
    return <div className="text-sm text-muted-foreground">No events</div>
  }

  const firstTime = new Date(events[0].timestamp).getTime()
  const lastTime = new Date(events[events.length - 1].timestamp).getTime()
  const totalDuration = lastTime - firstTime || 1

  return (
    <div className="space-y-4">
      <div className="relative pt-8 pb-4 px-2">
        <div className="flex gap-2 overflow-x-auto pb-2">
          {events.map((event, idx) => (
            <DecisionEventMarker
              key={event.id}
              eventType={event.eventType}
              timestamp={event.timestamp}
              isActive={idx === currentIndex}
              onClick={() => onIndexChange(idx)}
            />
          ))}
        </div>
      </div>

      <div className="space-y-2">
        <input
          type="range"
          min="0"
          max={events.length - 1}
          value={currentIndex}
          onChange={(e) => onIndexChange(Number.parseInt(e.target.value))}
          onMouseDown={() => setIsDragging(true)}
          onMouseUp={() => setIsDragging(false)}
          onTouchStart={() => setIsDragging(true)}
          onTouchEnd={() => setIsDragging(false)}
          className="w-full cursor-pointer"
        />
        <div className="flex items-center justify-between text-xs text-muted-foreground">
          <span>{events[0] ? new Date(events[0].timestamp).toLocaleString() : ""}</span>
          <span className="font-medium">
            Event {currentIndex + 1} / {events.length}
          </span>
          <span>{events[events.length - 1] ? new Date(events[events.length - 1].timestamp).toLocaleString() : ""}</span>
        </div>
      </div>
    </div>
  )
}
