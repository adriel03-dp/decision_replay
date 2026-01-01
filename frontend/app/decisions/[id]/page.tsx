"use client"

import { useEffect, useState } from "react"
import Link from "next/link"
import { ChevronLeft } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Card } from "@/components/ui/card"
import { OutcomeBadge } from "@/components/decision-replay/outcome-badge"
import { TimelineScrubber } from "@/components/decision-replay/timeline-scrubber"
import { ReplayControls } from "@/components/decision-replay/replay-controls"
import { DecisionStatePanel } from "@/components/decision-replay/decision-state-panel"
import { ReasoningPanel } from "@/components/decision-replay/reasoning-panel"
import { useDecisionStore } from "@/lib/store"
import { decisionApi } from "@/lib/api"
import type { DecisionExtended } from "@/lib/api-types"

export default function DecisionReplayPage({ params }: { params: Promise<{ id: string }> }) {
  const [id, setId] = useState<string>("")
  const [isClient, setIsClient] = useState(false)
  const [loading, setLoading] = useState(true)
  const { currentDecision, currentEventIndex, isPlaying, setCurrentDecision, setCurrentEventIndex, setIsPlaying } =
    useDecisionStore()

  const currentEvent = useDecisionStore.getState().getCurrentEvent()

  useEffect(() => {
    setIsClient(true)
    params.then((p) => setId(p.id))
  }, [params])

  useEffect(() => {
    if (!id) return

    async function loadDecisionWithEvents() {
      setLoading(true)
      try {
        const data = await decisionApi.getDecisionWithEvents(id)
        if (data) {
          setCurrentDecision(data, data.events || [])
        }
      } catch (error) {
        console.error('Failed to load decision:', error)
      } finally {
        setLoading(false)
      }
    }

    loadDecisionWithEvents()
  }, [id, setCurrentDecision])

  // Auto-play functionality
  useEffect(() => {
    if (!isPlaying) return

    const timer = setTimeout(() => {
      if (currentEventIndex < (currentDecision?.events?.length || 0) - 1) {
        setCurrentEventIndex(currentEventIndex + 1)
      } else {
        setIsPlaying(false)
      }
    }, 2000)

    return () => clearTimeout(timer)
  }, [isPlaying, currentEventIndex, currentDecision, setCurrentEventIndex, setIsPlaying])

  if (!isClient || loading) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary mx-auto mb-4"></div>
          <p className="text-muted-foreground">Loading decision...</p>
        </div>
      </div>
    )
  }

  if (!currentDecision) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <div className="text-center">
          <h2 className="text-2xl font-bold mb-2">Decision Not Found</h2>
          <p className="text-muted-foreground mb-4">The requested decision could not be found.</p>
          <Link href="/decisions">
            <Button>Back to Decisions</Button>
          </Link>
        </div>
      </div>
    )
  }

  return (
    <main className="min-h-screen bg-background">
      {/* Header Navigation */}
      <div className="bg-card border-b border-border">
        <div className="max-w-7xl mx-auto px-6 py-4 flex items-center gap-4">
          <Link href="/decisions">
            <Button variant="ghost" size="sm">
              <ChevronLeft className="w-4 h-4 mr-2" />
              Back
            </Button>
          </Link>
          <div className="flex-1">
            <h1 className="text-2xl font-bold text-foreground">{currentDecision.id}</h1>
          </div>
          <div className="flex items-center gap-2">
            <OutcomeBadge outcome={currentDecision.outcome || currentDecision.currentOutcome} />
          </div>
        </div>
      </div>

      <div className="max-w-7xl mx-auto px-6 py-6 space-y-6">
        {/* Decision Summary Strip */}
        <Card className="p-4 bg-card border border-border">
          <div className="grid grid-cols-2 md:grid-cols-5 gap-4">
            <div>
              <p className="text-xs font-semibold text-muted-foreground uppercase">ID</p>
              <p className="text-sm font-mono text-foreground mt-1">{currentDecision.id}</p>
            </div>
            <div>
              <p className="text-xs font-semibold text-muted-foreground uppercase">Type</p>
              <p className="text-sm font-medium text-foreground mt-1">{currentDecision.type}</p>
            </div>
            <div>
              <p className="text-xs font-semibold text-muted-foreground uppercase">Outcome</p>
              <div className="mt-1">
                <OutcomeBadge outcome={currentDecision.outcome || currentDecision.currentOutcome} size="sm" />
              </div>
            </div>
            <div>
              <p className="text-xs font-semibold text-muted-foreground uppercase">Risk Score</p>
              <p className="text-lg font-bold text-foreground mt-1">{currentDecision.riskScore ?? 0}</p>
            </div>
            <div>
              <p className="text-xs font-semibold text-muted-foreground uppercase">Finalized</p>
              <p className="text-sm text-foreground mt-1">
                {currentDecision.finalizedAt ? new Date(currentDecision.finalizedAt).toLocaleString() : "Pending"}
              </p>
            </div>
          </div>
        </Card>

        {/* 3-Column Layout */}
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* Left Panel - Decision State */}
          <Card className="lg:col-span-1 p-4 bg-card border border-border max-h-[500px]">
            <h2 className="text-sm font-bold text-foreground mb-4">Decision State</h2>
            <DecisionStatePanel event={currentEvent} timestamp={currentEvent?.timestamp || ""} />
          </Card>

          {/* Center Panel - Timeline */}
          <Card className="lg:col-span-1 p-4 bg-card border border-border">
            <h2 className="text-sm font-bold text-foreground mb-4">Timeline</h2>
            <div className="space-y-4">
              <TimelineScrubber
                events={useDecisionStore.getState().events}
                currentIndex={currentEventIndex}
                onIndexChange={setCurrentEventIndex}
              />
              <ReplayControls
                isPlaying={isPlaying}
                onPlay={() => setIsPlaying(true)}
                onPause={() => setIsPlaying(false)}
                onPrevious={() => setCurrentEventIndex(Math.max(0, currentEventIndex - 1))}
                onNext={() =>
                  setCurrentEventIndex(Math.min(useDecisionStore.getState().events.length - 1, currentEventIndex + 1))
                }
                currentIndex={currentEventIndex}
                totalEvents={useDecisionStore.getState().events.length}
              />
            </div>
          </Card>

          {/* Right Panel - Reasoning */}
          <Card className="lg:col-span-1 p-4 bg-card border border-border max-h-[500px]">
            <h2 className="text-sm font-bold text-foreground mb-4">Reasoning Snapshot</h2>
            <ReasoningPanel event={currentEvent} />
          </Card>
        </div>

        {/* Navigation Links */}
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <Link href={`/decisions/${currentDecision.id}/comparison`}>
            <Button variant="outline" className="w-full bg-transparent">
              Compare
            </Button>
          </Link>
          <Link href={`/decisions/${currentDecision.id}/factors`}>
            <Button variant="outline" className="w-full bg-transparent">
              Factors
            </Button>
          </Link>
          <Link href={`/decisions/${currentDecision.id}/diff`}>
            <Button variant="outline" className="w-full bg-transparent">
              Diff View
            </Button>
          </Link>
          <Link href={`/audit/decisions/${currentDecision.id}`}>
            <Button variant="outline" className="w-full bg-transparent">
              Audit Mode
            </Button>
          </Link>
        </div>
      </div>
    </main>
  )
}
