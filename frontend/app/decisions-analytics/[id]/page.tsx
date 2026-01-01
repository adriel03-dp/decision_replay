"use client"

import { useState, useEffect } from "react"
import Link from "next/link"
import { Button } from "@/components/ui/button"
import { Card } from "@/components/ui/card"
import { ArrowLeft, Loader2, Sparkles } from "lucide-react"
import { AnimatedTimelineChart } from "@/components/decision-replay/animated-timeline-chart"
import { AnimatedPerformanceChart } from "@/components/decision-replay/animated-performance-chart"
import { AnimatedFactorHeatmap } from "@/components/decision-replay/animated-factor-heatmap"
import { mockDecisions, mockDecisionEvents } from "@/lib/mock-data"

export default function AnalyticsPage({ params }: { params: { id: string } }) {
  const decision = mockDecisions.find((d) => d.id === params.id)
  const events = mockDecisionEvents.filter((e) => e.decisionId === params.id)

  const [explanation, setExplanation] = useState<string>("")
  const [factorAnalysis, setFactorAnalysis] = useState<string>("")
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    const fetchAnalysis = async () => {
      try {
        // Fetch explanation from Gemini API
        const explainRes = await fetch(`/api/decisions/${params.id}/explain`, {
          method: "POST",
        })
        const explainData = await explainRes.json()
        if (explainData.explanation) {
          setExplanation(explainData.explanation)
        }

        // Fetch factor analysis from Gemini API
        const factorRes = await fetch(`/api/decisions/${params.id}/factors`, {
          method: "POST",
        })
        const factorData = await factorRes.json()
        if (factorData.analysis) {
          setFactorAnalysis(factorData.analysis)
        }
      } catch (error) {
        console.error("[v0] Failed to fetch analysis:", error)
        // Set fallback text if API fails
        setExplanation("Unable to generate explanation at this time. Please try again later.")
        setFactorAnalysis("Unable to generate factor analysis at this time. Please try again later.")
      } finally {
        setLoading(false)
      }
    }

    if (params.id) {
      fetchAnalysis()
    }
  }, [params.id])

  if (!decision) {
    return (
      <main className="min-h-screen bg-gradient-to-br from-white to-green-50 dark:from-slate-950 dark:to-slate-900">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
          <p className="text-center text-lg text-muted-foreground">Decision not found</p>
        </div>
      </main>
    )
  }

  // Feasibility timeline data - showing how analysis evolved
  const timelineData = events.map((event, index) => ({
    time: event.timestamp.split("T")[1]?.slice(0, 5) || event.timestamp,
    feasibilityScore: event.confidence || 65,  // Overall feasibility
    timelinePressure: Math.max(30, 100 - (event.confidence || 65) + (Math.random() * 20 - 10)),
    resourceAdequacy: Math.min(90, (event.confidence || 65) + (Math.random() * 15)),
    scopeComplexity: 40 + index * 5 + (Math.random() * 10),
  }))

  // Resource allocation data - what's allocated vs. what's needed
  const resourceData = [
    { resource: "Team Size", allocated: 3, required: 5, gap: -2 },
    { resource: "Budget ($K)", allocated: 50, required: 45, gap: 5 },
    { resource: "Timeline (weeks)", allocated: 12, required: 16, gap: -4 },
    { resource: "Tech Stack", allocated: 2, required: 3, gap: -1 },
  ]

  // Planning risk factors with status indicators
  const factorData = [
    {
      factor: "Timeline Pressure",
      impact: 85,
      status: "critical" as const,
      trend: "up" as const,
      description: "12-week timeline is aggressive for scope - recommend 16 weeks",
    },
    {
      factor: "Team Size Gap",
      impact: 70,
      status: "critical" as const,
      trend: "stable" as const,
      description: "Need 2 additional developers to meet timeline",
    },
    {
      factor: "Scope Complexity",
      impact: 60,
      status: "warning" as const,
      trend: "up" as const,
      description: "Feature set is complex for MVP - consider phase 2",
    },
    {
      factor: "Budget Adequacy",
      impact: 30,
      status: "good" as const,
      trend: "stable" as const,
      description: "Budget covers estimated costs with 10% buffer",
    },
    {
      factor: "Technical Dependencies",
      impact: 45,
      status: "warning" as const,
      trend: "down" as const,
      description: "3rd party API integrations add risk",
    },
  ]

  return (
    <main className="min-h-screen bg-gradient-to-br from-white to-green-50 dark:from-slate-950 dark:to-slate-900">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
        {/* Header */}
        <Link href="/decisions-dashboard">
          <Button variant="ghost" className="mb-8 group">
            <ArrowLeft className="w-4 h-4 mr-2 group-hover:-translate-x-1 smooth-transition" />
            Back to Dashboard
          </Button>
        </Link>

        <div className="mb-12">
          <h1 className="text-4xl font-bold mb-2">{decision.title}</h1>
          <p className="text-foreground/60">{decision.description}</p>
        </div>

        {/* Planning Decision Summary Cards */}
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-12">
          <Card className="p-6 border-green-100 dark:border-slate-700 bg-white/50 dark:bg-slate-900/50 backdrop-blur">
            <p className="text-sm text-muted-foreground mb-2">Feasibility</p>
            <p className="text-2xl font-bold text-green-600 dark:text-green-400">
              {decision.confidence || 65}%
            </p>
          </Card>
          <Card className="p-6 border-green-100 dark:border-slate-700 bg-white/50 dark:bg-slate-900/50 backdrop-blur">
            <p className="text-sm text-muted-foreground mb-2">Status</p>
            <p className="text-2xl font-bold">{decision.outcome || "Draft"}</p>
          </Card>
          <Card className="p-6 border-green-100 dark:border-slate-700 bg-white/50 dark:bg-slate-900/50 backdrop-blur">
            <p className="text-sm text-muted-foreground mb-2">Risk Factors</p>
            <p className="text-2xl font-bold text-amber-600 dark:text-amber-400">5</p>
          </Card>
          <Card className="p-6 border-green-100 dark:border-slate-700 bg-white/50 dark:bg-slate-900/50 backdrop-blur">
            <p className="text-sm text-muted-foreground mb-2">Analysis Steps</p>
            <p className="text-2xl font-bold">{events.length}</p>
          </Card>
        </div>

        {/* Charts Section */}
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-12">
          <AnimatedTimelineChart data={timelineData} />
          <AnimatedPerformanceChart data={resourceData} />
        </div>

        {/* Factor Heatmap */}
        <div className="mb-12">
          <AnimatedFactorHeatmap data={factorData} />
        </div>

        {/* AI Analysis Section */}
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          {/* Feasibility Analysis */}
          <Card className="p-8 border-green-100 dark:border-slate-700 bg-white/50 dark:bg-slate-900/50 backdrop-blur">
            <div className="flex items-center gap-2 mb-4">
              <Sparkles className="w-5 h-5 text-green-600 dark:text-green-400" />
              <h3 className="text-xl font-semibold">Feasibility Analysis</h3>
            </div>
            {loading ? (
              <div className="flex items-center justify-center py-8">
                <Loader2 className="w-6 h-6 animate-spin text-green-600 dark:text-green-400" />
              </div>
            ) : (
              <div className="prose dark:prose-invert max-w-none text-sm space-y-3">
                {explanation.split("\n").map((line, i) => {
                  if (!line.trim()) return null
                  if (line.startsWith("#")) {
                    return (
                      <h4 key={i} className="font-semibold text-base mt-4">
                        {line.replace(/^#+\s*/, "")}
                      </h4>
                    )
                  }
                  return (
                    <p key={i} className="text-foreground/70">
                      {line}
                    </p>
                  )
                })}
              </div>
            )}
          </Card>

          {/* Risk & Bottleneck Analysis */}
          <Card className="p-8 border-green-100 dark:border-slate-700 bg-white/50 dark:bg-slate-900/50 backdrop-blur">
            <div className="flex items-center gap-2 mb-4">
              <Sparkles className="w-5 h-5 text-green-600 dark:text-green-400" />
              <h3 className="text-xl font-semibold">Risk & Bottlenecks</h3>
            </div>
            {loading ? (
              <div className="flex items-center justify-center py-8">
                <Loader2 className="w-6 h-6 animate-spin text-green-600 dark:text-green-400" />
              </div>
            ) : (
              <div className="prose dark:prose-invert max-w-none text-sm space-y-3">
                {factorAnalysis.split("\n").map((line, i) => {
                  if (!line.trim()) return null
                  if (line.startsWith("#")) {
                    return (
                      <h4 key={i} className="font-semibold text-base mt-4">
                        {line.replace(/^#+\s*/, "")}
                      </h4>
                    )
                  }
                  return (
                    <p key={i} className="text-foreground/70">
                      {line}
                    </p>
                  )
                })}
              </div>
            )}
          </Card>
        </div>
      </div>
    </main>
  )
}
