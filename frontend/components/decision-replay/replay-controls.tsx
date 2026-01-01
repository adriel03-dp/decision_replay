"use client"

import { Play, Pause, SkipBack, SkipForward } from "lucide-react"
import { Button } from "@/components/ui/button"

interface ReplayControlsProps {
  isPlaying: boolean
  onPlay: () => void
  onPause: () => void
  onPrevious: () => void
  onNext: () => void
  currentIndex: number
  totalEvents: number
}

export function ReplayControls({
  isPlaying,
  onPlay,
  onPause,
  onPrevious,
  onNext,
  currentIndex,
  totalEvents,
}: ReplayControlsProps) {
  return (
    <div className="flex items-center gap-2 bg-card p-4 rounded-lg border border-border">
      <Button variant="outline" size="sm" onClick={onPrevious} disabled={currentIndex === 0}>
        <SkipBack className="w-4 h-4" />
      </Button>

      <Button variant="default" size="sm" onClick={isPlaying ? onPause : onPlay}>
        {isPlaying ? <Pause className="w-4 h-4" /> : <Play className="w-4 h-4" />}
      </Button>

      <Button variant="outline" size="sm" onClick={onNext} disabled={currentIndex === totalEvents - 1}>
        <SkipForward className="w-4 h-4" />
      </Button>

      <div className="ml-auto text-sm text-muted-foreground">
        {currentIndex + 1} / {totalEvents}
      </div>
    </div>
  )
}
