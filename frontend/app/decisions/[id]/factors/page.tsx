"use client"

import Link from "next/link"
import { ChevronLeft } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Card } from "@/components/ui/card"
import { FactorBar } from "@/components/decision-replay/factor-bar"
import { useDecisionStore } from "@/lib/store"
import { useEffect, useState } from "react"

export default function FactorsPage({ params }: { params: Promise<{ id: string }> }) {
  const [id, setId] = useState<string>("")
  const [isClient, setIsClient] = useState(false)
  const { currentDecision } = useDecisionStore()

  useEffect(() => {
    setIsClient(true)
    params.then((p) => setId(p.id))
  }, [params])

  if (!isClient || !currentDecision) {
    return <div className="min-h-screen bg-background" />
  }

  // Aggregate factors from all AI events
  const allFactors = new Map<string, { weights: number[]; impacts: string[] }>()

  useDecisionStore.getState().events.forEach((event) => {
    if (event.eventType === "AI_REASONING" && event.payload.factors) {
      event.payload.factors.forEach((factor: any) => {
        if (!allFactors.has(factor.name)) {
          allFactors.set(factor.name, { weights: [], impacts: [] })
        }
        const data = allFactors.get(factor.name)!
        data.weights.push(factor.weight)
        data.impacts.push(factor.impact)
      })
    }
  })

  const aggregatedFactors = Array.from(allFactors.entries()).map(([name, data]) => ({
    name,
    avgWeight: data.weights.reduce((a, b) => a + b, 0) / data.weights.length,
    impact: data.impacts[0],
  }))

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
          <h1 className="text-2xl font-bold text-foreground">Factor Influence Heatmap</h1>
        </div>
      </div>

      <div className="max-w-7xl mx-auto px-6 py-6">
        <Card className="p-6 bg-card border border-border">
          <div className="space-y-6">
            <div>
              <h2 className="text-sm font-bold text-foreground mb-4">Factor Weights Over Time</h2>
              <p className="text-xs text-muted-foreground mb-4">
                Influence of each factor in the decision model, aggregated across all AI reasoning events
              </p>
            </div>

            {aggregatedFactors.length > 0 ? (
              <div className="space-y-4">
                {aggregatedFactors.map((factor) => (
                  <FactorBar
                    key={factor.name}
                    name={factor.name}
                    weight={factor.avgWeight}
                    impact={factor.impact as any}
                  />
                ))}
              </div>
            ) : (
              <div className="text-center py-8">
                <p className="text-muted-foreground">No factor data available</p>
              </div>
            )}
          </div>
        </Card>

        {/* Timeline Table */}
        <Card className="p-6 bg-card border border-border mt-6">
          <h2 className="text-sm font-bold text-foreground mb-4">Factor Timeline</h2>

          <div className="overflow-x-auto">
            <table className="w-full text-xs">
              <thead>
                <tr className="border-b border-border">
                  <th className="text-left py-2 px-2 font-semibold text-muted-foreground">Timestamp</th>
                  <th className="text-left py-2 px-2 font-semibold text-muted-foreground">Event</th>
                  <th className="text-left py-2 px-2 font-semibold text-muted-foreground">Factors</th>
                </tr>
              </thead>
              <tbody>
                {useDecisionStore.getState().events.map((event) => (
                  <tr key={event.id} className="border-b border-border hover:bg-slate-50">
                    <td className="py-3 px-2 font-mono text-foreground">
                      {new Date(event.timestamp).toLocaleTimeString()}
                    </td>
                    <td className="py-3 px-2 text-foreground">{event.eventType.replace(/_/g, " ")}</td>
                    <td className="py-3 px-2">
                      {event.eventType === "AI_REASONING" && event.payload.factors ? (
                        <div className="flex flex-wrap gap-1">
                          {event.payload.factors.map((f: any) => (
                            <span
                              key={f.name}
                              className={`px-2 py-1 rounded text-xs font-medium ${
                                f.impact === "positive"
                                  ? "bg-green-100 text-green-800"
                                  : f.impact === "negative"
                                    ? "bg-red-100 text-red-800"
                                    : "bg-slate-100 text-slate-800"
                              }`}
                            >
                              {f.name}
                            </span>
                          ))}
                        </div>
                      ) : (
                        <span className="text-muted-foreground">—</span>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </Card>
      </div>
    </main>
  )
}
