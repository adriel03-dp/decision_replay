"use client"

import Link from "next/link"
import { ChevronLeft, Download } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Card } from "@/components/ui/card"
import { OutcomeBadge } from "@/components/decision-replay/outcome-badge"
import { ConfidenceIndicator } from "@/components/decision-replay/confidence-indicator"
import { FactorBar } from "@/components/decision-replay/factor-bar"
import { useDecisionStore } from "@/lib/store"
import { useEffect, useState } from "react"

export default function AuditModePage({ params }: { params: Promise<{ id: string }> }) {
  const [id, setId] = useState<string>("")
  const [isClient, setIsClient] = useState(false)
  const { currentDecision } = useDecisionStore()

  useEffect(() => {
    setIsClient(true)
    params.then((p) => setId(p.id))
  }, [params])

  const handleExportJSON = () => {
    const data = {
      decision: currentDecision,
      events: useDecisionStore.getState().events,
      exportedAt: new Date().toISOString(),
    }
    const blob = new Blob([JSON.stringify(data, null, 2)], { type: "application/json" })
    const url = URL.createObjectURL(blob)
    const a = document.createElement("a")
    a.href = url
    a.download = `decision-${currentDecision?.id}-audit.json`
    a.click()
    URL.revokeObjectURL(url)
  }

  const handleExportPDF = () => {
    alert("PDF export would be implemented with a PDF library like jsPDF or pdfkit")
  }

  if (!isClient || !currentDecision) {
    return <div className="min-h-screen bg-background" />
  }

  const events = useDecisionStore.getState().events

  return (
    <main className="min-h-screen bg-background">
      {/* Header */}
      <div className="bg-card border-b border-border">
        <div className="max-w-7xl mx-auto px-6 py-4 flex items-center justify-between">
          <div className="flex items-center gap-4">
            <Link href={`/decisions/${currentDecision.id}`}>
              <Button variant="ghost" size="sm">
                <ChevronLeft className="w-4 h-4 mr-2" />
                Back
              </Button>
            </Link>
            <h1 className="text-2xl font-bold text-foreground">Audit Mode - Read-Only</h1>
          </div>
          <div className="flex gap-2">
            <Button onClick={handleExportJSON} variant="outline" size="sm">
              <Download className="w-4 h-4 mr-2" />
              Export JSON
            </Button>
            <Button onClick={handleExportPDF} variant="outline" size="sm">
              <Download className="w-4 h-4 mr-2" />
              Export PDF
            </Button>
          </div>
        </div>
      </div>

      <div className="max-w-7xl mx-auto px-6 py-6 space-y-6">
        {/* Decision Summary */}
        <Card className="p-6 bg-card border border-border">
          <div className="grid grid-cols-2 md:grid-cols-5 gap-6">
            <div>
              <p className="text-xs font-semibold text-muted-foreground uppercase">ID</p>
              <p className="text-sm font-mono text-foreground mt-2">{currentDecision.id}</p>
            </div>
            <div>
              <p className="text-xs font-semibold text-muted-foreground uppercase">Type</p>
              <p className="text-sm font-medium text-foreground mt-2">{currentDecision.type}</p>
            </div>
            <div>
              <p className="text-xs font-semibold text-muted-foreground uppercase">Status</p>
              <p className="text-sm text-foreground mt-2">
                <span className="inline-block px-2 py-1 text-xs font-medium rounded bg-slate-100 text-slate-800">
                  {currentDecision.status}
                </span>
              </p>
            </div>
            <div>
              <p className="text-xs font-semibold text-muted-foreground uppercase">Outcome</p>
              <div className="mt-2">
                <OutcomeBadge outcome={currentDecision.outcome} size="sm" />
              </div>
            </div>
            <div>
              <p className="text-xs font-semibold text-muted-foreground uppercase">Risk Score</p>
              <p className="text-lg font-bold text-foreground mt-2">{currentDecision.riskScore}</p>
            </div>
          </div>
        </Card>

        {/* Audit Trail */}
        <Card className="p-6 bg-card border border-border">
          <h2 className="text-lg font-bold text-foreground mb-4">Complete Audit Trail</h2>

          <div className="space-y-4">
            {events.length === 0 ? (
              <p className="text-muted-foreground">No events recorded</p>
            ) : (
              events.map((event, idx) => (
                <div key={event.id} className="p-4 bg-slate-50 rounded-md border border-border">
                  <div className="flex items-start justify-between mb-3">
                    <div>
                      <p className="text-sm font-bold text-foreground">
                        {idx + 1}. {event.eventType}
                      </p>
                      <p className="text-xs text-muted-foreground font-mono mt-1">ID: {event.id}</p>
                    </div>
                    <p className="text-xs text-muted-foreground">{new Date(event.timestamp).toLocaleString()}</p>
                  </div>

                  {/* Event Details */}
                  <div className="space-y-3 mt-3 pt-3 border-t border-border">
                    {event.payload.inputs && (
                      <div>
                        <p className="text-xs font-semibold text-foreground mb-2">Inputs</p>
                        <div className="bg-white p-2 rounded text-xs space-y-1">
                          {Object.entries(event.payload.inputs).map(([key, value]) => (
                            <div key={key} className="flex justify-between">
                              <span className="text-muted-foreground">{key}:</span>
                              <span className="font-mono text-foreground">{String(value)}</span>
                            </div>
                          ))}
                        </div>
                      </div>
                    )}

                    {event.payload.rules && (
                      <div>
                        <p className="text-xs font-semibold text-foreground mb-2">Rules</p>
                        <div className="space-y-1">
                          {event.payload.rules.map((rule: any, ruleIdx: number) => (
                            <div key={ruleIdx} className="flex items-center gap-2 text-xs">
                              <span className={rule.passed ? "text-green-600" : "text-red-600"}>
                                {rule.passed ? "✓" : "✕"}
                              </span>
                              <span className="text-foreground">{rule.name}</span>
                            </div>
                          ))}
                        </div>
                      </div>
                    )}

                    {event.payload.reasoning && (
                      <div>
                        <p className="text-xs font-semibold text-foreground mb-2">AI Reasoning</p>
                        <p className="text-xs text-foreground leading-relaxed bg-white p-2 rounded">
                          {event.payload.reasoning}
                        </p>
                      </div>
                    )}

                    {event.payload.confidence !== undefined && (
                      <div>
                        <ConfidenceIndicator confidence={event.payload.confidence} label="Confidence" />
                      </div>
                    )}

                    {event.payload.factors && (
                      <div>
                        <p className="text-xs font-semibold text-foreground mb-2">Factors</p>
                        <div className="space-y-2">
                          {event.payload.factors.map((factor: any) => (
                            <FactorBar
                              key={factor.name}
                              name={factor.name}
                              weight={factor.weight}
                              impact={factor.impact}
                            />
                          ))}
                        </div>
                      </div>
                    )}

                    {event.payload.overriddenBy && (
                      <div className="p-2 bg-amber-50 rounded border border-amber-200">
                        <p className="text-xs font-semibold text-amber-900 mb-1">Human Override</p>
                        <p className="text-xs text-amber-800">{event.payload.reason}</p>
                      </div>
                    )}
                  </div>
                </div>
              ))
            )}
          </div>
        </Card>

        {/* Immutable Notice */}
        <Card className="p-4 bg-slate-50 border border-border">
          <p className="text-xs text-muted-foreground">
            This audit view is immutable and read-only. All timestamps and events are locked for compliance purposes.
          </p>
        </Card>
      </div>
    </main>
  )
}
