"use client"

import { useState, useEffect } from "react"
import { useParams, useRouter } from "next/navigation"
import Link from "next/link"
import { ChevronLeft, FileText, TrendingUp, Clock, DollarSign, AlertTriangle } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Card } from "@/components/ui/card"
import { LineChart, Line, BarChart, Bar, PieChart, Pie, Cell, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from 'recharts'

export default function DecisionAnalyticsPage() {
  const params = useParams()
  const router = useRouter()
  const decisionId = params.id as string

  const [decision, setDecision] = useState<any>(null)
  const [analytics, setAnalytics] = useState<any>(null)
  const [loading, setLoading] = useState(true)

  type CostBreakdown = Record<string, number | string>

  useEffect(() => {
    fetchAnalytics()
  }, [decisionId])

  async function fetchAnalytics() {
    try {
      const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/v2/decisions/${decisionId}/analytics`, {
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('token')}`
        }
      })

      if (!response.ok) {
        throw new Error('Failed to fetch analytics')
      }

      const data = await response.json()
      setDecision(data.decision)
      setAnalytics(data.analytics)
    } catch (error) {
      router.push(`/decisions/${decisionId}/analysis`)
    } finally {
      setLoading(false)
    }
  }

  if (loading) {
    return (
      <main className="min-h-screen bg-gradient-to-br from-white to-green-50 dark:from-slate-950 dark:to-slate-900">
        <div className="max-w-5xl mx-auto px-6 py-12">
          <div className="text-center py-20">
            <div className="w-16 h-16 border-4 border-green-500 border-t-transparent rounded-full animate-spin mx-auto mb-4"></div>
            <p className="text-foreground/60">Loading analytics...</p>
          </div>
        </div>
      </main>
    )
  }

  if (!decision || !analytics) {
    return null
  }

  // Prepare cost breakdown chart data
  const costData = analytics.costBreakdown ? Object.entries(analytics.costBreakdown).map(([name, value]) => ({
    name: name.replace(/_/g, ' '),
    value: typeof value === 'number' ? value : 0
  })) : []

  const COLORS = ['#10b981', '#3b82f6', '#f59e0b', '#ef4444']

  // Timeline progress data
  const timelineData = analytics.timeline ? analytics.timeline.map((phase: any, idx: number) => ({
    name: phase.name,
    progress: ((idx + 1) / analytics.timeline.length) * 100,
    duration: parseInt(phase.duration) || 25
  })) : []

  return (
    <main className="min-h-screen bg-gradient-to-br from-white to-green-50 dark:from-slate-950 dark:to-slate-900">
      <div className="bg-white/80 dark:bg-slate-950/80 backdrop-blur border-b border-green-100 dark:border-slate-800">
        <div className="max-w-7xl mx-auto px-6 py-4 flex items-center justify-between">
          <div className="flex items-center gap-4">
            <Link href={`/decisions/${decisionId}/analysis`}>
              <Button variant="ghost" size="sm">
                <ChevronLeft className="w-4 h-4 mr-2" />
                Back to Analysis
              </Button>
            </Link>
            <h1 className="text-2xl font-bold">Decision Analytics</h1>
          </div>
        </div>
      </div>

      <div className="max-w-7xl mx-auto px-6 py-8 space-y-6">
        {/* Summary Cards */}
        <div className="grid md:grid-cols-3 gap-4">
          <Card className="p-6 border-green-100 dark:border-slate-700 bg-white/50 dark:bg-slate-900/50 backdrop-blur">
            <div className="flex items-center gap-3 mb-2">
              <div className="p-2 bg-green-100 dark:bg-green-900/30 rounded-lg">
                <TrendingUp className="w-5 h-5 text-green-600 dark:text-green-400" />
              </div>
              <h3 className="font-semibold">Feasibility</h3>
            </div>
            <div className="text-3xl font-bold text-green-600 dark:text-green-400">
              {analytics.feasibilityScore || 'N/A'}%
            </div>
            <p className="text-xs text-foreground/60 mt-1">Overall score</p>
          </Card>

          <Card className="p-6 border-green-100 dark:border-slate-700 bg-white/50 dark:bg-slate-900/50 backdrop-blur">
            <div className="flex items-center gap-3 mb-2">
              <div className="p-2 bg-blue-100 dark:bg-blue-900/30 rounded-lg">
                <Clock className="w-5 h-5 text-blue-600 dark:text-blue-400" />
              </div>
              <h3 className="font-semibold">Timeline</h3>
            </div>
            <div className="text-3xl font-bold text-blue-600 dark:text-blue-400">
              {analytics.estimatedTimeline || 'TBD'}
            </div>
            <p className="text-xs text-foreground/60 mt-1">Estimated duration</p>
          </Card>

          <Card className="p-6 border-green-100 dark:border-slate-700 bg-white/50 dark:bg-slate-900/50 backdrop-blur">
            <div className="flex items-center gap-3 mb-2">
              <div className="p-2 bg-amber-100 dark:bg-amber-900/30 rounded-lg">
                <AlertTriangle className="w-5 h-5 text-amber-600 dark:text-amber-400" />
              </div>
              <h3 className="font-semibold">Risk Level</h3>
            </div>
            <div className="text-3xl font-bold text-amber-600 dark:text-amber-400">
              {analytics.riskLevel || 'Unknown'}
            </div>
            <p className="text-xs text-foreground/60 mt-1">Overall risk</p>
          </Card>
        </div>

        {/* Timeline Progress Chart */}
        {timelineData.length > 0 && (
          <Card className="p-6 border-green-100 dark:border-slate-700 bg-white/50 dark:bg-slate-900/50 backdrop-blur">
            <h2 className="text-lg font-bold mb-4">Timeline Progress</h2>
            <ResponsiveContainer width="100%" height={300}>
              <BarChart data={timelineData}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="name" />
                <YAxis label={{ value: 'Duration (%)', angle: -90, position: 'insideLeft' }} />
                <Tooltip />
                <Legend />
                <Bar dataKey="duration" fill="#10b981" name="Time Allocation %" />
              </BarChart>
            </ResponsiveContainer>
            <p className="text-sm text-foreground/60 mt-2">Distribution of project phases by time allocation</p>
          </Card>
        )}

        {/* Cost Breakdown Chart */}
        {costData.length > 0 && (
          <Card className="p-6 border-green-100 dark:border-slate-700 bg-white/50 dark:bg-slate-900/50 backdrop-blur">
            <h2 className="text-lg font-bold mb-4 flex items-center gap-2">
              <DollarSign className="w-5 h-5 text-green-600 dark:text-green-400" />
              Cost Distribution
            </h2>
            <div className="grid md:grid-cols-2 gap-6">
              <ResponsiveContainer width="100%" height={300}>
                <PieChart>
                  <Pie
                    data={costData}
                    cx="50%"
                    cy="50%"
                    labelLine={false}
                    label={({ name, percent }) => `${name}: ${(percent * 100).toFixed(0)}%`}
                    outerRadius={80}
                    fill="#8884d8"
                    dataKey="value"
                  >
                    {costData.map((entry, index) => (
                      <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                    ))}
                  </Pie>
                  <Tooltip />
                </PieChart>
              </ResponsiveContainer>
              <div className="space-y-3">
                {Object.entries(analytics.costBreakdown as CostBreakdown).map(([category, amount]) => (
                  <div key={category} className="flex items-center justify-between">
                    <span className="text-sm capitalize">{category.replace(/_/g, ' ')}</span>
                    <span className="font-semibold">${typeof amount === 'number' ? amount.toLocaleString() : amount}</span>
                  </div>
                ))}
                {analytics.totalCost && (
                  <div className="pt-3 border-t border-green-100 dark:border-slate-700 flex items-center justify-between">
                    <span className="font-bold">Total</span>
                    <span className="text-xl font-bold text-green-600 dark:text-green-400">
                      ${typeof analytics.totalCost === 'number' ? analytics.totalCost.toLocaleString() : analytics.totalCost}
                    </span>
                  </div>
                )}
              </div>
            </div>
            <p className="text-sm text-foreground/60 mt-4">Budget allocation across project categories</p>
          </Card>
        )}

        {/* Timeline Visualization */}
        {analytics.timeline && analytics.timeline.length > 0 && (
          <Card className="p-6 border-green-100 dark:border-slate-700 bg-white/50 dark:bg-slate-900/50 backdrop-blur">
            <h2 className="text-lg font-bold mb-4">Project Phases</h2>
            <div className="space-y-4">
              {analytics.timeline.map((phase: any, idx: number) => (
                <div key={idx} className="relative pl-8">
                  <div className="absolute left-0 top-1 w-4 h-4 bg-green-500 rounded-full border-4 border-white dark:border-slate-900"></div>
                  {idx < analytics.timeline.length - 1 && (
                    <div className="absolute left-[7px] top-5 bottom-0 w-0.5 bg-green-200 dark:bg-green-800"></div>
                  )}
                  <div>
                    <div className="flex items-center justify-between mb-1">
                      <h3 className="font-semibold">{phase.name}</h3>
                      <span className="text-sm text-foreground/60">{phase.duration}</span>
                    </div>
                    <p className="text-sm text-foreground/70">{phase.description}</p>
                    {phase.milestones && phase.milestones.length > 0 && (
                      <ul className="mt-2 space-y-1">
                        {phase.milestones.map((milestone: string, mIdx: number) => (
                          <li key={mIdx} className="text-xs text-foreground/60 flex items-start gap-2">
                            <span className="text-green-600 dark:text-green-400 mt-0.5">✓</span>
                            {milestone}
                          </li>
                        ))}
                      </ul>
                    )}
                  </div>
                </div>
              ))}
            </div>
          </Card>
        )}

        {/* Assumptions */}
        {analytics.assumptions && analytics.assumptions.length > 0 && (
          <Card className="p-6 border-green-100 dark:border-slate-700 bg-white/50 dark:bg-slate-900/50 backdrop-blur">
            <h2 className="text-lg font-bold mb-4">Key Assumptions</h2>
            <ul className="space-y-2">
              {analytics.assumptions.map((assumption: string, idx: number) => (
                <li key={idx} className="text-sm text-foreground/70 flex items-start gap-2">
                  <span className="text-green-600 dark:text-green-400 mt-0.5">•</span>
                  {assumption}
                </li>
              ))}
            </ul>
          </Card>
        )}

        {/* Decision Context */}
        <Card className="p-6 border-green-100 dark:border-slate-700 bg-white/50 dark:bg-slate-900/50 backdrop-blur">
          <h2 className="text-lg font-bold mb-3 flex items-center gap-2">
            <FileText className="w-5 h-5 text-green-600 dark:text-green-400" />
            Original Decision
          </h2>
          <p className="text-foreground/70">{decision.naturalLanguageInput}</p>
        </Card>

        <div className="flex justify-center">
          <Link href={`/decisions/${decisionId}/analysis`}>
            <Button className="bg-gradient-to-r from-green-500 to-emerald-600 hover:from-green-600 hover:to-emerald-700">
              Back to Analysis
            </Button>
          </Link>
        </div>
      </div>
    </main>
  )
}
