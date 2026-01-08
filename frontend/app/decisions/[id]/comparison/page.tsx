"use client"

import Link from "next/link"
import { ChevronLeft } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Card } from "@/components/ui/card"
import { OutcomeBadge } from "@/components/decision-replay/outcome-badge"
import { ConfidenceIndicator } from "@/components/decision-replay/confidence-indicator"
import { FactorBar } from "@/components/decision-replay/factor-bar"
import { useDecisionStore } from "@/lib/store"
import { useEffect, useState } from "react"

export default function ComparisonPage({ params }: { params: Promise<{ id: string }> }) {
  const [id, setId] = useState<string>("")
  const [isClient, setIsClient] = useState(false)
  const { currentDecision } = useDecisionStore()

  useEffect(() => {
    let mounted = true
    
    setIsClient(true)
    params.then((p) => {
      if (mounted) setId(p.id)
    })
    
    return () => {
      mounted = false
    }
  }, [params])

  if (!isClient || !currentDecision) {
    return <div className="min-h-screen bg-background" />
  }

  // Find AI reasoning event with validation
  const events = useDecisionStore.getState().events
  const aiEvent = events.find((e) => e.eventType === "AI_REASONING")
  const aiReasoning = aiEvent?.payload

  const aiConfidence = typeof aiReasoning?.confidence === 'number' ? aiReasoning.confidence : 0
  const aiOutcome = currentDecision.outcome || 'Unknown'
  const humanOverride = events.find((e) => e.eventType === "HUMAN_OVERRIDE")
  
  // Check if we have enough data to show comparison
  const hasComparisonData = aiEvent || humanOverride
  
  if (!hasComparisonData) {
    return (
      <main className="min-h-screen bg-background">
        <div className="bg-card border-b border-border">
          <div className="max-w-7xl mx-auto px-6 py-4 flex items-center gap-4">
            <Link href={`/decisions/${currentDecision.id}`}>
              <Button variant="ghost" size="sm">
                <ChevronLeft className="w-4 h-4 mr-2" />
                Back
              </Button>
            </Link>
            <h1 className="text-2xl font-bold text-foreground">AI vs Human Comparison</h1>
          </div>
        </div>
        <div className="max-w-7xl mx-auto px-6 py-6">
          <Card className="p-8 text-center">
            <h2 className="text-xl font-semibold mb-4">No Comparison Data Available</h2>
            <p className="text-muted-foreground mb-4">
              This decision doesn't have AI reasoning or human override data to compare.
            </p>
            <Link href={`/decisions/${currentDecision.id}/analysis`}>
              <Button>
                Generate Analysis
              </Button>
            </Link>
          </Card>
        </div>
      </main>
    )
  }

  return (
    <main className="min-h-screen bg-background">
      <div className="bg-card border-b border-border">
        <div className="max-w-7xl mx-auto px-6 py-4 flex items-center gap-4">
          <Link href={`/decisions/${currentDecision.id}`}>
            <Button variant="ghost" size="sm">
              <ChevronLeft className="w-4 h-4 mr-2" />
              Back
            </Button>
          </Link>
          <h1 className="text-2xl font-bold text-foreground">AI vs Human Comparison</h1>
        </div>
      </div>

      <div className="max-w-7xl mx-auto px-6 py-6 space-y-6">
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          {/* AI Recommendation */}
          <Card className="p-6 bg-card border border-border">
            <h2 className="text-lg font-bold text-foreground mb-4">AI Recommendation</h2>

            <div className="space-y-4">
              <div>
                <p className="text-xs font-semibold text-muted-foreground uppercase mb-2">Outcome</p>
                <OutcomeBadge outcome={aiOutcome} />
              </div>

              <div>
                <ConfidenceIndicator confidence={aiConfidence} label="AI Confidence" />
              </div>

              <div>
                <p className="text-xs font-semibold text-muted-foreground uppercase mb-2">Reasoning</p>
                <p className="text-sm text-foreground leading-relaxed">{aiReasoning?.reasoning}</p>
              </div>

              <div>
                <p className="text-xs font-semibold text-muted-foreground uppercase mb-2">Model</p>
                <p className="text-sm font-mono text-foreground">{aiReasoning?.source}</p>
              </div>

              {aiReasoning?.factors && (
                <div>
                  <p className="text-xs font-semibold text-muted-foreground uppercase mb-3">Factor Weights</p>
                  <div className="space-y-2">
                    {aiReasoning.factors.map((factor: any) => (
                      <FactorBar key={factor.name} name={factor.name} weight={factor.weight} impact={factor.impact} />
                    ))}
                  </div>
                </div>
              )}
            </div>
          </Card>

          {/* Human Decision */}
          <Card className="p-6 bg-card border border-border">
            <h2 className="text-lg font-bold text-foreground mb-4">
              {humanOverride ? "Human Override" : "Human Decision"}
            </h2>

            <div className="space-y-4">
              <div>
                <p className="text-xs font-semibold text-muted-foreground uppercase mb-2">Final Outcome</p>
                <OutcomeBadge outcome={currentDecision.outcome} />
              </div>

              {humanOverride ? (
                <>
                  <div className="p-3 bg-amber-50 border border-amber-200 rounded-md">
                    <p className="text-xs font-semibold text-amber-900 mb-2">Override Reason</p>
                    <p className="text-sm text-amber-800">{humanOverride.payload.reason}</p>
                  </div>

                  <div className="grid grid-cols-2 gap-4 text-xs">
                    <div>
                      <p className="text-muted-foreground font-semibold uppercase mb-1">Original AI Call</p>
                      <p className="text-foreground">{humanOverride.payload.originalAIRecommendation}</p>
                    </div>
                    <div>
                      <p className="text-muted-foreground font-semibold uppercase mb-1">Human Decision</p>
                      <p className="text-foreground font-medium">{currentDecision.outcome}</p>
                    </div>
                  </div>
                </>
              ) : (
                <div className="p-3 bg-green-50 border border-green-200 rounded-md">
                  <p className="text-xs font-semibold text-green-900">No override - AI decision accepted</p>
                </div>
              )}

              <div className="text-xs">
                <p className="text-muted-foreground font-semibold uppercase mb-1">Status</p>
                <p className="text-foreground">{currentDecision.status}</p>
              </div>
            </div>
          </Card>
        </div>

        {/* Disagreement Analysis */}
        {humanOverride && (
          <Card className="p-6 bg-slate-50 border border-border">
            <h2 className="text-lg font-bold text-foreground mb-4">Disagreement Analysis</h2>

            <div className="space-y-3">
              <div className="p-3 bg-white rounded-md border border-border">
                <p className="text-xs font-semibold text-muted-foreground uppercase mb-2">Original AI Recommendation</p>
                <OutcomeBadge outcome={humanOverride.payload.originalAIRecommendation} />
              </div>

              <div className="p-3 bg-white rounded-md border border-border">
                <p className="text-xs font-semibold text-muted-foreground uppercase mb-2">Final Human Decision</p>
                <OutcomeBadge outcome={currentDecision.outcome} />
              </div>

              <div className="p-3 bg-amber-50 rounded-md border border-amber-200">
                <p className="text-sm text-amber-900 leading-relaxed">{humanOverride.payload.reason}</p>
              </div>
            </div>
          </Card>
        )}
      </div>
    </main>
  )
}
