"use client"

import Link from "next/link"
import { ChevronLeft } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Card } from "@/components/ui/card"
import { useDecisionStore } from "@/lib/store"
import { useEffect, useState } from "react"

export default function DiffViewPage({ params }: { params: Promise<{ id: string }> }) {
  const [id, setId] = useState<string>("")
  const [isClient, setIsClient] = useState(false)
  const [version1, setVersion1] = useState(0)
  const [version2, setVersion2] = useState(-1)
  const { currentDecision } = useDecisionStore()
  const events = useDecisionStore.getState().events

  useEffect(() => {
    let mounted = true
    
    setIsClient(true)
    params.then((p) => {
      if (mounted) setId(p.id)
    })
    
    // Set version2 to last event if not set
    if (version2 === -1 && events.length > 0) {
      setVersion2(events.length - 1)
    }
    
    return () => {
      mounted = false
    }
  }, [params, events.length, version2])

  if (!isClient || !currentDecision) {
    return <div className="min-h-screen bg-background" />
  }

  const event1 = events[version1]
  const event2 = events[version2]

  const getDifferences = () => {
    if (!event1 || !event2 || !event1.payload || !event2.payload) {
      return []
    }

    const differences: Array<{ field: string; before: any; after: any }> = []

    try {
      // Compare inputs
      if (event1.payload.inputs && event2.payload.inputs) {
        const allKeys = new Set([
          ...Object.keys(event1.payload.inputs || {}),
          ...Object.keys(event2.payload.inputs || {})
        ])
        
        allKeys.forEach((key) => {
          const before = event1.payload.inputs[key]
          const after = event2.payload.inputs[key]
          
          if (before !== after) {
            differences.push({
              field: `input.${key}`,
              before: before ?? 'Not Set',
              after: after ?? 'Not Set',
            })
          }
        })
      }

    // Compare rules
    if (event1.payload.rules && event2.payload.rules) {
      event2.payload.rules.forEach((rule2: any, idx: number) => {
        const rule1 = event1.payload.rules[idx]
        if (rule1 && rule1.passed !== rule2.passed) {
          differences.push({
            field: `rule.${rule2.name}`,
            before: rule1.passed ? "PASS" : "FAIL",
            after: rule2.passed ? "PASS" : "FAIL",
          })
        }
      })
    }

    // Compare reasoning
    if (event1.payload.confidence !== event2.payload.confidence) {
      differences.push({
        field: "confidence",
        before: event1.payload.confidence,
        after: event2.payload.confidence,
      })
    }

    return differences
  }

  const differences = getDifferences()\n  \n  // Add validation for events\n  if (!event1 || !event2) {\n    return (\n      <main className=\"min-h-screen bg-background\">\n        <div className=\"bg-card border-b border-border\">\n          <div className=\"max-w-7xl mx-auto px-6 py-4 flex items-center gap-4\">\n            <Link href={`/decisions/${currentDecision.id}`}>\n              <Button variant=\"ghost\" size=\"sm\">\n                <ChevronLeft className=\"w-4 h-4 mr-2\" />\n                Back\n              </Button>\n            </Link>\n            <h1 className=\"text-2xl font-bold text-foreground\">Diff View</h1>\n          </div>\n        </div>\n        <div className=\"max-w-7xl mx-auto px-6 py-6\">\n          <Card className=\"p-8 text-center\">\n            <h2 className=\"text-xl font-semibold mb-4\">Insufficient Data</h2>\n            <p className=\"text-muted-foreground\">\n              Not enough event data to perform comparison. Please ensure at least 2 events exist.\n            </p>\n          </Card>\n        </div>\n      </main>\n    )\n  }"

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
          <h1 className="text-2xl font-bold text-foreground">Diff View</h1>
        </div>
      </div>

      <div className="max-w-7xl mx-auto px-6 py-6 space-y-6">
        {/* Version Selectors */}
        <Card className="p-4 bg-card border border-border">
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="text-xs font-semibold text-muted-foreground uppercase block mb-2">Version 1</label>
              <select
                value={version1}
                onChange={(e) => setVersion1(Number(e.target.value))}
                className="w-full px-3 py-2 text-sm bg-background border border-border rounded-md text-foreground"
              >
                {events.map((event, idx) => (
                  <option key={event.id} value={idx}>
                    {idx} - {event.eventType} ({new Date(event.timestamp).toLocaleTimeString()})
                  </option>
                ))}
              </select>
            </div>

            <div>
              <label className="text-xs font-semibold text-muted-foreground uppercase block mb-2">Version 2</label>
              <select
                value={version2}
                onChange={(e) => setVersion2(Number(e.target.value))}
                className="w-full px-3 py-2 text-sm bg-background border border-border rounded-md text-foreground"
              >
                {events.map((event, idx) => (
                  <option key={event.id} value={idx}>
                    {idx} - {event.eventType} ({new Date(event.timestamp).toLocaleTimeString()})
                  </option>
                ))}
              </select>
            </div>
          </div>
        </Card>

        {/* Side-by-side Comparison */}
        {event1 && event2 && (
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            {/* Version 1 */}
            <Card className="p-6 bg-card border border-border">
              <h2 className="text-lg font-bold text-foreground mb-4">Version 1 (Event {version1})</h2>
              <div className="space-y-4 text-sm">
                <div>
                  <p className="text-xs font-semibold text-muted-foreground uppercase mb-1">Type</p>
                  <p className="text-foreground">{event1.eventType}</p>
                </div>
                <div>
                  <p className="text-xs font-semibold text-muted-foreground uppercase mb-1">Timestamp</p>
                  <p className="text-foreground font-mono">{new Date(event1.timestamp).toLocaleString()}</p>
                </div>

                {event1.payload.inputs && (
                  <div>
                    <p className="text-xs font-semibold text-muted-foreground uppercase mb-2">Inputs</p>
                    <div className="bg-slate-50 p-3 rounded-md space-y-1 text-xs">
                      {Object.entries(event1.payload.inputs).map(([key, value]) => (
                        <div key={key} className="flex justify-between">
                          <span className="text-muted-foreground">{key}</span>
                          <span className="font-mono text-foreground">{String(value)}</span>
                        </div>
                      ))}
                    </div>
                  </div>
                )}

                {event1.payload.reasoning && (
                  <div>
                    <p className="text-xs font-semibold text-muted-foreground uppercase mb-1">Reasoning</p>
                    <p className="text-foreground leading-relaxed">{event1.payload.reasoning}</p>
                  </div>
                )}
              </div>
            </Card>

            {/* Version 2 */}
            <Card className="p-6 bg-card border border-border">
              <h2 className="text-lg font-bold text-foreground mb-4">Version 2 (Event {version2})</h2>
              <div className="space-y-4 text-sm">
                <div>
                  <p className="text-xs font-semibold text-muted-foreground uppercase mb-1">Type</p>
                  <p className="text-foreground">{event2.eventType}</p>
                </div>
                <div>
                  <p className="text-xs font-semibold text-muted-foreground uppercase mb-1">Timestamp</p>
                  <p className="text-foreground font-mono">{new Date(event2.timestamp).toLocaleString()}</p>
                </div>

                {event2.payload.inputs && (
                  <div>
                    <p className="text-xs font-semibold text-muted-foreground uppercase mb-2">Inputs</p>
                    <div className="bg-slate-50 p-3 rounded-md space-y-1 text-xs">
                      {Object.entries(event2.payload.inputs).map(([key, value]) => (
                        <div key={key} className="flex justify-between">
                          <span className="text-muted-foreground">{key}</span>
                          <span className="font-mono text-foreground">{String(value)}</span>
                        </div>
                      ))}
                    </div>
                  </div>
                )}

                {event2.payload.reasoning && (
                  <div>
                    <p className="text-xs font-semibold text-muted-foreground uppercase mb-1">Reasoning</p>
                    <p className="text-foreground leading-relaxed">{event2.payload.reasoning}</p>
                  </div>
                )}
              </div>
            </Card>
          </div>
        )}

        {/* Differences Highlighted */}
        {differences.length > 0 && (
          <Card className="p-6 bg-slate-50 border border-border">
            <h2 className="text-lg font-bold text-foreground mb-4">Changes Detected</h2>
            <div className="space-y-3">
              {differences.map((diff, idx) => (
                <div key={idx} className="p-3 bg-white rounded-md border border-border">
                  <p className="text-xs font-semibold text-muted-foreground uppercase mb-2">{diff.field}</p>
                  <div className="grid grid-cols-2 gap-4 text-sm">
                    <div>
                      <p className="text-xs text-muted-foreground mb-1">Before</p>
                      <p className="text-foreground font-mono bg-red-50 p-2 rounded">{String(diff.before)}</p>
                    </div>
                    <div>
                      <p className="text-xs text-muted-foreground mb-1">After</p>
                      <p className="text-foreground font-mono bg-green-50 p-2 rounded">{String(diff.after)}</p>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </Card>
        )}
      </div>
    </main>
  )
}
