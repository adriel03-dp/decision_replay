"use client"

import { useState, useEffect } from "react"
import { useParams, useRouter } from "next/navigation"
import Link from "next/link"
import { ChevronLeft, BarChart3, AlertTriangle, CheckCircle, Loader2, RefreshCw } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { useToast } from "@/hooks/use-toast"
import { decisionApi } from "@/lib/api"
import type { SavedDecisionResponse, SimulationResponse, FeasibilityResultDto, ProjectPlanDto } from "@/lib/api-types"

function ScoreBar({ label, value }: { label: string; value: number }) {
  const pct = Math.round(value)
  const color = pct >= 75 ? "bg-green-500" : pct >= 55 ? "bg-yellow-500" : "bg-red-500"
  return (
    <div className="space-y-1">
      <div className="flex justify-between text-sm">
        <span className="text-muted-foreground">{label}</span>
        <span className="font-medium">{pct}/100</span>
      </div>
      <div className="h-2 bg-muted rounded-full overflow-hidden">
        <div className={`h-full rounded-full transition-all duration-500 ${color}`} style={{ width: `${pct}%` }} />
      </div>
    </div>
  )
}

function VerdictBadge({ verdict }: { verdict: string }) {
  const cls =
    verdict === "Feasible" ? "bg-green-100 text-green-800" :
    verdict === "Risky"    ? "bg-yellow-100 text-yellow-800" : "bg-red-100 text-red-800"
  return <span className={`px-3 py-1 rounded-full text-sm font-medium ${cls}`}>{verdict}</span>
}

export default function AnalysisPage() {
  const params = useParams()
  const router = useRouter()
  const { toast } = useToast()
  const id = params.id as string

  const [decision, setDecision] = useState<SavedDecisionResponse | null>(null)
  const [loading, setLoading] = useState(true)

  // Simulation panel
  const [simBudget, setSimBudget] = useState("")
  const [simTimeline, setSimTimeline] = useState("")
  const [simTeam, setSimTeam] = useState("")
  const [simulating, setSimulating] = useState(false)
  const [simResult, setSimResult] = useState<SimulationResponse | null>(null)

  useEffect(() => {
    decisionApi.getSavedDecision(id).then(d => {
      if (!d) { toast({ title: "Decision not found", variant: "destructive" }); router.push("/decisions"); return }
      setDecision(d)
      if (d.analysis) {
        setSimBudget(String(d.analysis.input.budgetUsd))
        setSimTimeline(String(d.analysis.input.timelineMonths))
        setSimTeam(String(d.analysis.input.teamSize))
      }
    }).finally(() => setLoading(false))
  }, [id])

  const handleSimulate = async () => {
    if (!decision?.analysis) return
    setSimulating(true)
    setSimResult(null)
    try {
      const res = await decisionApi.simulateProject({
        baseProject: {
          projectType: decision.analysis.input.projectType,
          features: decision.analysis.input.features,
          budgetUsd: decision.analysis.input.budgetUsd,
          timelineMonths: decision.analysis.input.timelineMonths,
          teamSize: decision.analysis.input.teamSize,
          requestAiEnhancement: false,
        },
        budgetUsd: simBudget ? Number(simBudget) : undefined,
        timelineMonths: simTimeline ? Number(simTimeline) : undefined,
        teamSize: simTeam ? Number(simTeam) : undefined,
      })
      setSimResult(res)
    } catch {
      toast({ title: "Simulation failed", variant: "destructive" })
    } finally { setSimulating(false) }
  }

  if (loading) return (
    <div className="min-h-screen flex items-center justify-center">
      <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
    </div>
  )

  const analysis = decision?.analysis
  if (!analysis) return (
    <div className="min-h-screen flex items-center justify-center">
      <p className="text-muted-foreground">No analysis data available.</p>
    </div>
  )

  const f = analysis.feasibility
  const p = analysis.plan

  return (
    <div className="min-h-screen bg-background">
      <div className="max-w-5xl mx-auto px-4 py-8 space-y-6">

        {/* Header */}
        <div className="flex items-center gap-3">
          <Link href="/decisions">
            <Button variant="ghost" size="sm"><ChevronLeft className="h-4 w-4 mr-1" />Decisions</Button>
          </Link>
          <div className="flex-1">
            <h1 className="text-2xl font-bold">{analysis.input.projectType}</h1>
            <p className="text-sm text-muted-foreground">{analysis.input.features.join(", ")}</p>
          </div>
          <VerdictBadge verdict={f.verdict} />
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">

          {/* Left col */}
          <div className="lg:col-span-2 space-y-6">

            {/* Score breakdown */}
            <Card>
              <CardHeader><CardTitle className="flex items-center gap-2"><BarChart3 className="h-4 w-4" />Feasibility Score</CardTitle></CardHeader>
              <CardContent className="space-y-4">
                <div className="flex items-center gap-4">
                  <p className="text-5xl font-bold">{Math.round(f.score)}</p>
                  <div>
                    <p className="text-muted-foreground text-sm">out of 100</p>
                    <VerdictBadge verdict={f.verdict} />
                  </div>
                </div>
                <div className="space-y-3 pt-2">
                  <ScoreBar label="Budget Fit" value={f.budgetFitScore} />
                  <ScoreBar label="Timeline Fit" value={f.timelineFitScore} />
                  <ScoreBar label="Team Capacity" value={f.teamCapacityScore} />
                  <ScoreBar label="Simplicity" value={f.complexityScore} />
                </div>
                <div className="grid grid-cols-3 gap-4 pt-2 text-center border-t">
                  <div><p className="text-xs text-muted-foreground">Est. Cost</p><p className="font-semibold">${f.estimatedCostUsd.toLocaleString()}</p></div>
                  <div><p className="text-xs text-muted-foreground">Est. Months</p><p className="font-semibold">{f.estimatedMonths.toFixed(1)}</p></div>
                  <div><p className="text-xs text-muted-foreground">Req. Team</p><p className="font-semibold">{f.requiredTeamSize}</p></div>
                </div>
              </CardContent>
            </Card>

            {/* Timeline */}
            <Card>
              <CardHeader><CardTitle>Timeline — {p.totalMonths.toFixed(1)} months</CardTitle></CardHeader>
              <CardContent>
                <div className="space-y-3">
                  {p.phases.map((ph, i) => (
                    <div key={i} className="space-y-1">
                      <div className="flex justify-between text-sm font-medium">
                        <span>{ph.name}</span>
                        <span className="text-muted-foreground">{ph.durationMonths.toFixed(1)} mo · {Math.round(ph.percentageOfTotal)}%</span>
                      </div>
                      <div className="h-2 bg-muted rounded-full overflow-hidden">
                        <div className="h-full bg-primary/60 rounded-full" style={{ width: `${ph.percentageOfTotal}%` }} />
                      </div>
                      {ph.tasks.length > 0 && (
                        <p className="text-xs text-muted-foreground">{ph.tasks.slice(0, 3).join(" · ")}{ph.tasks.length > 3 ? "…" : ""}</p>
                      )}
                    </div>
                  ))}
                </div>
              </CardContent>
            </Card>

            {/* Issues */}
            {f.issues.length > 0 && (
              <Card>
                <CardHeader><CardTitle className="flex items-center gap-2"><AlertTriangle className="h-4 w-4 text-yellow-500" />Issues</CardTitle></CardHeader>
                <CardContent className="space-y-2">
                  {f.issues.map((issue, i) => (
                    <div key={i} className="flex items-start gap-2 text-sm">
                      <span className={`mt-0.5 px-1.5 py-0.5 rounded text-xs font-medium shrink-0 ${
                        issue.severity === "High" ? "bg-red-100 text-red-700" :
                        issue.severity === "Medium" ? "bg-yellow-100 text-yellow-700" : "bg-blue-100 text-blue-700"
                      }`}>{issue.severity}</span>
                      <span className="text-muted-foreground">{issue.message}</span>
                    </div>
                  ))}
                </CardContent>
              </Card>
            )}

            {/* Suggestions */}
            {f.suggestedAdjustments.length > 0 && (
              <Card>
                <CardHeader><CardTitle className="flex items-center gap-2"><CheckCircle className="h-4 w-4 text-green-500" />Suggestions</CardTitle></CardHeader>
                <CardContent className="space-y-1">
                  {f.suggestedAdjustments.map((s, i) => (
                    <p key={i} className="text-sm text-muted-foreground">
                      • <strong>{s.parameter}:</strong> {s.description}{s.quantitativeImpact ? ` (${s.quantitativeImpact})` : ""}
                    </p>
                  ))}
                </CardContent>
              </Card>
            )}

            {/* Explainability */}
            {f.explainability.length > 0 && (
              <Card>
                <CardHeader><CardTitle className="text-sm">How the score was calculated</CardTitle></CardHeader>
                <CardContent className="space-y-1">
                  {f.explainability.map((e, i) => <p key={i} className="text-sm text-muted-foreground">• {e}</p>)}
                </CardContent>
              </Card>
            )}
          </div>

          {/* Right col: simulation */}
          <div className="space-y-4">
            <Card>
              <CardHeader><CardTitle className="flex items-center gap-2"><RefreshCw className="h-4 w-4" />Scenario Simulation</CardTitle></CardHeader>
              <CardContent className="space-y-4">
                <p className="text-xs text-muted-foreground">Adjust parameters to see how your score changes.</p>
                <div className="space-y-3">
                  <div className="space-y-1">
                    <Label className="text-xs">Budget (USD)</Label>
                    <Input type="number" value={simBudget} onChange={e => setSimBudget(e.target.value)} placeholder={String(analysis.input.budgetUsd)} />
                  </div>
                  <div className="space-y-1">
                    <Label className="text-xs">Timeline (months)</Label>
                    <Input type="number" value={simTimeline} onChange={e => setSimTimeline(e.target.value)} placeholder={String(analysis.input.timelineMonths)} />
                  </div>
                  <div className="space-y-1">
                    <Label className="text-xs">Team Size</Label>
                    <Input type="number" value={simTeam} onChange={e => setSimTeam(e.target.value)} placeholder={String(analysis.input.teamSize)} />
                  </div>
                </div>
                <Button className="w-full" onClick={handleSimulate} disabled={simulating}>
                  {simulating ? <Loader2 className="h-4 w-4 mr-2 animate-spin" /> : <RefreshCw className="h-4 w-4 mr-2" />}
                  Run Simulation
                </Button>

                {simResult && (
                  <div className="mt-4 space-y-3 border-t pt-4">
                    <div className="flex justify-between items-center">
                      <span className="text-sm font-medium">New Score</span>
                      <span className="text-2xl font-bold">{Math.round(simResult.newFeasibility.score)}</span>
                    </div>
                    <div className="flex justify-between items-center">
                      <span className="text-sm text-muted-foreground">Change</span>
                      <span className={`font-medium ${simResult.scoreDelta >= 0 ? "text-green-600" : "text-red-600"}`}>
                        {simResult.scoreDelta >= 0 ? "+" : ""}{simResult.scoreDelta.toFixed(1)}
                      </span>
                    </div>
                    <p className="text-xs text-muted-foreground">{simResult.verdictDelta}</p>
                    {simResult.impactSummary.length > 0 && (
                      <div className="space-y-1">
                        {simResult.impactSummary.map((s, i) => <p key={i} className="text-xs text-muted-foreground">• {s}</p>)}
                      </div>
                    )}
                  </div>
                )}
              </CardContent>
            </Card>
          </div>
        </div>
      </div>
    </div>
  )
}

import { AnimatedPerformanceChart } from "@/components/decision-replay/animated-performance-chart"
import { AnimatedFactorHeatmap } from "@/components/decision-replay/animated-factor-heatmap"

// Chart data interfaces
interface FeasibilityTimelineData {
  time: string
  feasibilityScore: number
  timelinePressure: number
  resourceAdequacy: number
  scopeComplexity: number
}

interface ResourceAllocationData {
  resource: string
  allocated: number
  required: number
  gap: number
}

interface PlanningFactorData {
  factor: string
  impact: number
  status: "good" | "warning" | "critical"
  trend: "up" | "down" | "stable"
  description?: string
}

interface ChartData {
  timeline: FeasibilityTimelineData[]
  performance: ResourceAllocationData[]
  riskHeatmap: PlanningFactorData[]
}

interface AnalysisResponse {
  analysisId: string
  feasibilityScore: number
  feasibilityVerdict: string
  executiveSummary: string
  currentPlanAnalysis?: any
  pros: string[]
  cons: string[]
  optimizedSolution?: any
  optimizedPros: string[]
  optimizedCons: string[]
  risks: Array<{ description: string; impact: string; mitigation?: string }>
  assumptions: string[]
  recommendations: string[]
  confidenceLevel: number
  generatedAt: string
  modelUsed: string
  chartData?: ChartData
}

export default function DecisionAnalysisPage() {
  const params = useParams()
  const router = useRouter()
  const { toast } = useToast()
  const decisionId = params.id as string

  const [decision, setDecision] = useState<any>(null)
  const [analysis, setAnalysis] = useState<AnalysisResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [analyzing, setAnalyzing] = useState(false)
  const [question, setQuestion] = useState("")
  const [queryResponse, setQueryResponse] = useState<string | null>(null)
  const [querying, setQuerying] = useState(false)

  useEffect(() => {
    fetchDecision()
  }, [decisionId])

  async function fetchDecision() {
    try {
      const controller = new AbortController()
      const timeoutId = setTimeout(() => controller.abort(), 15000) // 15s timeout for better reliability

      const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/v2/decisions/${decisionId}`, {
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('token')}`
        },
        signal: controller.signal
      })

      clearTimeout(timeoutId)

      if (response.ok) {
        const data = await response.json()
        setDecision(data)

        // Try to fetch existing analysis
        await fetchExistingAnalysis()
      } else {
        // Get detailed error information
        let errorDetail = 'Unknown error'
        try {
          const errorData = await response.json()
          errorDetail = errorData.error || errorData.message || `HTTP ${response.status}`
        } catch {
          errorDetail = `HTTP ${response.status} - ${response.statusText}`
        }

        console.error('API Error Details:', {
          status: response.status,
          statusText: response.statusText,
          url: `${process.env.NEXT_PUBLIC_API_URL}/v2/decisions/${decisionId}`,
          detail: errorDetail
        })

        throw new Error(`Failed to fetch decision: ${errorDetail}`)
      }
    } catch (error) {
      console.error('Failed to fetch decision:', error)

      // Extract user-friendly error message
      let errorMessage = 'Failed to load decision.'
      if (error instanceof Error) {
        if (error.name === 'AbortError') {
          errorMessage = 'Request timed out. Please check your connection.'
        } else {
          errorMessage = error.message
        }
      }

      toast({
        title: "Unable to Load Decision",
        description: errorMessage,
        variant: "destructive",
      })

      // Redirect after showing error
      setTimeout(() => router.push('/decisions'), 2000)
    } finally {
      setLoading(false)
    }
  }

  async function fetchExistingAnalysis() {
    try {
      const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/v2/decisions/${decisionId}/analysis`, {
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('token')}`
        }
      })

      if (response.ok) {
        const analysisData = await response.json()
        setAnalysis(analysisData)
      } else if (response.status === 404) {
        // No analysis exists, trigger automatic analysis
        console.log('No existing analysis found, triggering automatic analysis...')
        await analyzeDecision()
      }
      // If other error, ignore - user can manually trigger analysis
    } catch (error) {
      // Ignore errors - analysis might not exist yet
      console.log('Error fetching analysis, user can trigger manually')
    }
  }

  async function analyzeDecision() {
    if (analyzing) return

    setAnalyzing(true)
    try {
      const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/v2/decisions/${decisionId}/analyze`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('token')}`
        }
      })

      if (!response.ok) {
        const error = await response.json()
        throw new Error(error.message || 'Analysis failed')
      }

      const result = await response.json()
      setAnalysis(result)
    } catch (error) {
      console.error('Analysis failed:', error)

      // Extract user-friendly error message  
      let errorMessage = 'Failed to analyze decision'
      if (error instanceof Error) {
        errorMessage = error.message
      }

      toast({
        title: "Analysis Failed",
        description: errorMessage,
        variant: "destructive",
      })
    } finally {
      setAnalyzing(false)
    }
  }

  async function askQuestion() {
    if (!question.trim() || querying) return

    setQuerying(true)
    setQueryResponse(null)

    try {
      const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/v2/decisions/${decisionId}/query`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${localStorage.getItem('token')}`
        },
        body: JSON.stringify({ question })
      })

      if (!response.ok) {
        throw new Error('Query failed')
      }

      const result = await response.json()
      setQueryResponse(result.answer)
      setQuestion("")
    } catch (error) {
      console.error('Query failed:', error)
      setQueryResponse("Failed to get answer. Please try again.")
    } finally {
      setQuerying(false)
    }
  }

  if (loading) {
    return (
      <main className="min-h-screen bg-gradient-to-br from-white to-green-50 dark:from-slate-950 dark:to-slate-900">
        <div className="max-w-5xl mx-auto px-6 py-12">
          <div className="text-center py-20">
            <div className="w-16 h-16 border-4 border-green-500 border-t-transparent rounded-full animate-spin mx-auto mb-4"></div>
            <p className="text-foreground/60">Loading decision...</p>
          </div>
        </div>
      </main>
    )
  }

  if (!decision) {
    return null
  }

  return (
    <main className="min-h-screen bg-gradient-to-br from-white to-green-50 dark:from-slate-950 dark:to-slate-900">
      {/* Header */}
      <div className="bg-white/80 dark:bg-slate-950/80 backdrop-blur border-b border-green-100 dark:border-slate-800">
        <div className="max-w-5xl mx-auto px-6 py-4 flex items-center justify-between">
          <div className="flex items-center gap-4">
            <Link href="/decisions">
              <Button variant="ghost" size="sm">
                <ChevronLeft className="w-4 h-4 mr-2" />
                Back
              </Button>
            </Link>
            <h1 className="text-2xl font-bold">Decision Analysis</h1>
          </div>
          <Link href={`/decisions/${decisionId}/analytics`}>
            <Button variant="outline">
              <BarChart3 className="w-4 h-4 mr-2" />
              View Analytics
            </Button>
          </Link>
        </div>
      </div>

      <div className="max-w-5xl mx-auto px-6 py-8 space-y-6">
        {/* Decision Context */}
        <Card className="p-6 border-green-100 dark:border-slate-700 bg-white/50 dark:bg-slate-900/50 backdrop-blur">
          <h2 className="text-lg font-bold mb-3">Your Decision</h2>
          <p className="text-foreground/70">{decision.naturalLanguageInput}</p>
          {decision.inferredAttributes && Object.keys(decision.inferredAttributes).length > 0 && (
            <div className="mt-4 pt-4 border-t border-green-100 dark:border-slate-700">
              <h3 className="text-sm font-semibold mb-2">Inferred Attributes:</h3>
              <div className="grid grid-cols-2 gap-3 text-sm">
                {Object.entries(decision.inferredAttributes).map(([key, value]) => (
                  <div key={key}>
                    <span className="text-foreground/60">{key}:</span>{" "}
                    <span className="font-medium">{String(value)}</span>
                  </div>
                ))}
              </div>
            </div>
          )}
        </Card>

        {/* Analysis Results */}
        {!analysis && !analyzing && (
          <Card className="p-8 border-green-100 dark:border-slate-700 bg-white/50 dark:bg-slate-900/50 backdrop-blur text-center">
            <Sparkles className="w-12 h-12 text-green-600 dark:text-green-400 mx-auto mb-4" />
            <h2 className="text-xl font-bold mb-2">Ready to Analyze</h2>
            <p className="text-foreground/60 mb-4">
              Click below to get AI-powered feasibility analysis, risk identification, and recommendations.
            </p>
            <Button
              onClick={analyzeDecision}
              className="bg-gradient-to-r from-green-500 to-emerald-600 hover:from-green-600 hover:to-emerald-700"
            >
              <Sparkles className="w-4 h-4 mr-2" />
              Analyze Decision
            </Button>
          </Card>
        )}

        {analyzing && (
          <Card className="p-8 border-green-100 dark:border-slate-700 bg-white/50 dark:bg-slate-900/50 backdrop-blur text-center">
            <div className="w-16 h-16 border-4 border-green-500 border-t-transparent rounded-full animate-spin mx-auto mb-4"></div>
            <h2 className="text-xl font-bold mb-2">Analyzing Decision...</h2>
            <p className="text-foreground/60">Gemini AI is evaluating feasibility, risks, and recommendations.</p>
          </Card>
        )}

        {analysis && (
          <>
            {/* Feasibility Score */}
            <Card className="p-6 border-green-100 dark:border-slate-700 bg-white/50 dark:bg-slate-900/50 backdrop-blur">
              <div className="flex items-center justify-between mb-4">
                <h2 className="text-lg font-bold">Feasibility Assessment</h2>
                <div className="flex items-center gap-2">
                  {analysis.feasibilityScore >= 70 ? (
                    <CheckCircle2 className="w-5 h-5 text-green-600 dark:text-green-400" />
                  ) : analysis.feasibilityScore >= 40 ? (
                    <AlertCircle className="w-5 h-5 text-yellow-600 dark:text-yellow-400" />
                  ) : (
                    <AlertCircle className="w-5 h-5 text-red-600 dark:text-red-400" />
                  )}
                  <span className="text-2xl font-bold">{Math.round(analysis.feasibilityScore)}/100</span>
                </div>
              </div>
              <div className="mb-3">
                <div className="text-sm font-semibold mb-1">Verdict: {analysis.feasibilityVerdict}</div>
                <div className="w-full bg-gray-200 dark:bg-slate-700 rounded-full h-2">
                  <div
                    className={`h-2 rounded-full transition-all ${analysis.feasibilityScore >= 70
                        ? 'bg-green-500'
                        : analysis.feasibilityScore >= 40
                          ? 'bg-yellow-500'
                          : 'bg-red-500'
                      }`}
                    style={{ width: `${analysis.feasibilityScore}%` }}
                  />
                </div>
              </div>
              <p className="text-foreground/70">{analysis.executiveSummary}</p>
            </Card>

            {/* Current Plan Assessment */}
            {analysis.currentPlanAnalysis && (
              <Card className="p-6 border-blue-100 dark:border-slate-700 bg-blue-50/30 dark:bg-blue-900/10 backdrop-blur">
                <h2 className="text-lg font-bold mb-4 flex items-center gap-2">
                  <AlertCircle className="w-5 h-5 text-blue-600 dark:text-blue-400" />
                  Current Plan Assessment
                </h2>
                <div className="grid md:grid-cols-2 gap-4">
                  <div>
                    <h4 className="font-semibold text-sm mb-1 text-blue-900 dark:text-blue-200">Timeline Assessment</h4>
                    <p className="text-sm text-foreground/70 mb-3">{analysis.currentPlanAnalysis.timelineAssessment}</p>
                    <h4 className="font-semibold text-sm mb-1 text-blue-900 dark:text-blue-200">Scope Assessment</h4>
                    <p className="text-sm text-foreground/70">{analysis.currentPlanAnalysis.scopeAssessment}</p>
                  </div>
                  <div>
                    <h4 className="font-semibold text-sm mb-1 text-blue-900 dark:text-blue-200">Budget Assessment</h4>
                    <p className="text-sm text-foreground/70 mb-3">{analysis.currentPlanAnalysis.budgetAssessment}</p>
                    <h4 className="font-semibold text-sm mb-1 text-blue-900 dark:text-blue-200">Resource Assessment</h4>
                    <p className="text-sm text-foreground/70">{analysis.currentPlanAnalysis.resourceAssessment}</p>
                  </div>
                </div>
              </Card>
            )}

            {/* Current Plan Pros & Cons */}
            <div className="grid md:grid-cols-2 gap-6">
              <Card className="p-6 border-green-100 dark:border-slate-700 bg-white/50 dark:bg-slate-900/50 backdrop-blur">
                <h3 className="text-lg font-bold mb-3 flex items-center gap-2">
                  <CheckCircle2 className="w-5 h-5 text-green-600 dark:text-green-400" />
                  Current Plan Strengths
                </h3>
                <ul className="space-y-2">
                  {analysis.pros?.map((pro: string, idx: number) => (
                    <li key={idx} className="text-sm text-foreground/70 flex items-start gap-2">
                      <span className="text-green-600 dark:text-green-400 mt-0.5">✓</span>
                      {pro}
                    </li>
                  ))}
                </ul>
              </Card>

              <Card className="p-6 border-red-100 dark:border-slate-700 bg-red-50/30 dark:bg-red-900/10 backdrop-blur">
                <h3 className="text-lg font-bold mb-3 flex items-center gap-2">
                  <AlertCircle className="w-5 h-5 text-red-600 dark:text-red-400" />
                  Current Plan Issues
                </h3>
                <ul className="space-y-2">
                  {analysis.cons?.map((con: string, idx: number) => (
                    <li key={idx} className="text-sm text-foreground/70 flex items-start gap-2">
                      <span className="text-red-600 dark:text-red-400 mt-0.5">⚠</span>
                      {con}
                    </li>
                  ))}
                </ul>
              </Card>
            </div>

            {/* Optimized Solution */}
            {analysis.optimizedSolution && (
              <Card className="p-6 border-emerald-100 dark:border-slate-700 bg-emerald-50/30 dark:bg-emerald-900/10 backdrop-blur">
                <h2 className="text-lg font-bold mb-4 flex items-center gap-2">
                  <Sparkles className="w-5 h-5 text-emerald-600 dark:text-emerald-400" />
                  Optimized Solution (Success Rate: {analysis.optimizedSolution.successProbability}%)
                </h2>
                <div className="grid md:grid-cols-2 gap-4">
                  <div>
                    <h4 className="font-semibold text-sm mb-1 text-emerald-900 dark:text-emerald-200">Improved Timeline</h4>
                    <p className="text-sm text-foreground/70 mb-3">{analysis.optimizedSolution.improvedTimeline}</p>
                    <h4 className="font-semibold text-sm mb-1 text-emerald-900 dark:text-emerald-200">Clarified Scope</h4>
                    <p className="text-sm text-foreground/70">{analysis.optimizedSolution.clarifiedScope}</p>
                  </div>
                  <div>
                    <h4 className="font-semibold text-sm mb-1 text-emerald-900 dark:text-emerald-200">Budget Optimization</h4>
                    <p className="text-sm text-foreground/70 mb-3">{analysis.optimizedSolution.budgetOptimization}</p>
                    <h4 className="font-semibold text-sm mb-1 text-emerald-900 dark:text-emerald-200">Resource Strategy</h4>
                    <p className="text-sm text-foreground/70">{analysis.optimizedSolution.resourceStrategy}</p>
                  </div>
                </div>
              </Card>
            )}

            {/* Optimized Plan Pros & Cons */}
            {analysis.optimizedPros && analysis.optimizedCons && (
              <div className="grid md:grid-cols-2 gap-6">
                <Card className="p-6 border-emerald-100 dark:border-slate-700 bg-emerald-50/30 dark:bg-emerald-900/10 backdrop-blur">
                  <h3 className="text-lg font-bold mb-3 flex items-center gap-2">
                    <CheckCircle2 className="w-5 h-5 text-emerald-600 dark:text-emerald-400" />
                    Optimized Plan Benefits
                  </h3>
                  <ul className="space-y-2">
                    {analysis.optimizedPros.map((pro: string, idx: number) => (
                      <li key={idx} className="text-sm text-foreground/70 flex items-start gap-2">
                        <span className="text-emerald-600 dark:text-emerald-400 mt-0.5">★</span>
                        {pro}
                      </li>
                    ))}
                  </ul>
                </Card>

                <Card className="p-6 border-orange-100 dark:border-slate-700 bg-orange-50/30 dark:bg-orange-900/10 backdrop-blur">
                  <h3 className="text-lg font-bold mb-3 flex items-center gap-2">
                    <AlertCircle className="w-5 h-5 text-orange-600 dark:text-orange-400" />
                    Optimized Plan Trade-offs
                  </h3>
                  <ul className="space-y-2">
                    {analysis.optimizedCons.map((con: string, idx: number) => (
                      <li key={idx} className="text-sm text-foreground/70 flex items-start gap-2">
                        <span className="text-orange-600 dark:text-orange-400 mt-0.5">◊</span>
                        {con}
                      </li>
                    ))}
                  </ul>
                </Card>
              </div>
            )}

            {/* Risks */}
            {analysis.risks && analysis.risks.length > 0 && (
              <Card className="p-6 border-green-100 dark:border-slate-700 bg-white/50 dark:bg-slate-900/50 backdrop-blur">
                <h3 className="text-lg font-bold mb-3">Risk Factors</h3>
                <div className="space-y-3">
                  {analysis.risks.map((risk: any, idx: number) => (
                    <div key={idx} className="p-3 bg-red-50 dark:bg-red-900/20 rounded-lg border border-red-100 dark:border-red-900/50">
                      <div className="flex items-start justify-between mb-1">
                        <span className="font-semibold text-sm">{risk.description}</span>
                        <span className={`text-xs px-2 py-0.5 rounded-full ${risk.impact === 'HIGH' ? 'bg-red-200 dark:bg-red-900 text-red-900 dark:text-red-200' :
                            risk.impact === 'MEDIUM' ? 'bg-yellow-200 dark:bg-yellow-900 text-yellow-900 dark:text-yellow-200' :
                              'bg-green-200 dark:bg-green-900 text-green-900 dark:text-green-200'
                          }`}>
                          {risk.impact} Impact
                        </span>
                      </div>
                      {risk.mitigation && (
                        <p className="text-xs text-foreground/60">Mitigation: {risk.mitigation}</p>
                      )}
                    </div>
                  ))}
                </div>
              </Card>
            )}

            {/* Recommendations */}
            {analysis.recommendations && analysis.recommendations.length > 0 && (
              <Card className="p-6 border-green-100 dark:border-slate-700 bg-white/50 dark:bg-slate-900/50 backdrop-blur">
                <h3 className="text-lg font-bold mb-3 flex items-center gap-2">
                  <TrendingUp className="w-5 h-5 text-green-600 dark:text-green-400" />
                  Recommendations
                </h3>
                <ul className="space-y-2">
                  {analysis.recommendations.map((rec: string, idx: number) => (
                    <li key={idx} className="text-sm text-foreground/70 flex items-start gap-2">
                      <span className="text-green-600 dark:text-green-400 mt-0.5">→</span>
                      {rec}
                    </li>
                  ))}
                </ul>
              </Card>
            )}

            {/* Interactive Charts Section */}
            {analysis.chartData && (
              <>
                <div className="space-y-6">
                  <div className="flex items-center gap-3 mb-4">
                    <BarChart3 className="w-6 h-6 text-green-600 dark:text-green-400" />
                    <h2 className="text-xl font-bold">Visual Analytics</h2>
                    <span className="text-sm text-muted-foreground">Interactive charts based on AI analysis</span>
                  </div>

                  {/* Timeline Chart */}
                  {analysis.chartData.timeline && analysis.chartData.timeline.length > 0 && (
                    <AnimatedTimelineChart data={analysis.chartData.timeline} />
                  )}

                  {/* Resource Performance Chart */}
                  {analysis.chartData.performance && analysis.chartData.performance.length > 0 && (
                    <AnimatedPerformanceChart data={analysis.chartData.performance} />
                  )}

                  {/* Risk Factor Heatmap */}
                  {analysis.chartData.riskHeatmap && analysis.chartData.riskHeatmap.length > 0 && (
                    <AnimatedFactorHeatmap data={analysis.chartData.riskHeatmap} />
                  )}
                </div>
              </>
            )}

            {/* Fallback Charts with Sample Data (when chartData not available) */}
            {analysis && !analysis.chartData && (
              <>
                <div className="space-y-6">
                  <div className="flex items-center gap-3 mb-4">
                    <BarChart3 className="w-6 h-6 text-green-600 dark:text-green-400" />
                    <h2 className="text-xl font-bold">Visual Analytics</h2>
                    <span className="text-sm text-muted-foreground">Charts based on analysis data</span>
                  </div>

                  {/* Sample Timeline Chart */}
                  <AnimatedTimelineChart data={[
                    { time: "Initial", feasibilityScore: Math.max(0, analysis.feasibilityScore - 15), timelinePressure: 100 - analysis.feasibilityScore + 10, resourceAdequacy: analysis.feasibilityScore - 10, scopeComplexity: 100 - analysis.feasibilityScore },
                    { time: "Refined", feasibilityScore: analysis.feasibilityScore, timelinePressure: 100 - analysis.feasibilityScore, resourceAdequacy: analysis.feasibilityScore, scopeComplexity: 100 - analysis.feasibilityScore - 10 },
                    { time: "Optimized", feasibilityScore: Math.min(100, analysis.feasibilityScore + 10), timelinePressure: Math.max(0, 100 - analysis.feasibilityScore - 15), resourceAdequacy: Math.min(100, analysis.feasibilityScore + 15), scopeComplexity: Math.max(0, 100 - analysis.feasibilityScore - 20) }
                  ]} />

                  {/* Sample Performance Chart */}
                  <AnimatedPerformanceChart data={[
                    { resource: "Team Size", allocated: Math.round(analysis.feasibilityScore * 0.8), required: Math.round(analysis.feasibilityScore * 0.9), gap: Math.round(analysis.feasibilityScore * 0.1) },
                    { resource: "Budget (k$)", allocated: Math.round(analysis.feasibilityScore * 1.2), required: Math.round(analysis.feasibilityScore * 1.35), gap: Math.round(analysis.feasibilityScore * 0.15) },
                    { resource: "Timeline (weeks)", allocated: Math.round(analysis.feasibilityScore * 0.6), required: Math.round(analysis.feasibilityScore * 0.7), gap: Math.round(analysis.feasibilityScore * 0.1) }
                  ]} />

                  {/* Sample Risk Heatmap */}
                  <AnimatedFactorHeatmap data={[
                    ...analysis.risks.map(risk => ({
                      factor: risk.description,
                      impact: risk.impact === "HIGH" ? 85 : risk.impact === "MEDIUM" ? 60 : 35,
                      status: (risk.impact === "HIGH" ? "critical" : risk.impact === "MEDIUM" ? "warning" : "good") as "good" | "warning" | "critical",
                      trend: "stable" as "up" | "down" | "stable",
                      description: risk.mitigation
                    })),
                    { factor: "Timeline Feasibility", impact: 100 - analysis.feasibilityScore, status: analysis.feasibilityScore >= 70 ? "good" : analysis.feasibilityScore >= 40 ? "warning" : "critical" as "good" | "warning" | "critical", trend: "stable" as "up" | "down" | "stable", description: "Overall timeline assessment" },
                    { factor: "Resource Availability", impact: analysis.feasibilityScore > 70 ? 30 : 70, status: analysis.feasibilityScore >= 70 ? "good" : "warning" as "good" | "warning" | "critical", trend: "up" as "up" | "down" | "stable", description: "Resource allocation status" }
                  ]} />
                </div>
              </>
            )}

            {/* Q&A Section */}
            <Card className="p-6 border-green-100 dark:border-slate-700 bg-white/50 dark:bg-slate-900/50 backdrop-blur">
              <h3 className="text-lg font-bold mb-3">Ask Questions About This Decision</h3>
              <p className="text-sm text-foreground/60 mb-4">
                Ask decision-related questions only. Off-topic queries will be rejected.
              </p>
              <div className="flex gap-2">
                <input
                  type="text"
                  value={question}
                  onChange={(e) => setQuestion(e.target.value)}
                  onKeyPress={(e) => e.key === 'Enter' && askQuestion()}
                  placeholder="e.g., What if we extend the timeline by 2 months?"
                  className="flex-1 px-4 py-2 text-sm bg-white dark:bg-slate-800 border border-green-200 dark:border-slate-700 rounded-lg focus:ring-2 focus:ring-green-500 focus:border-transparent"
                  disabled={querying}
                />
                <Button
                  onClick={askQuestion}
                  disabled={querying || !question.trim()}
                  className="bg-gradient-to-r from-green-500 to-emerald-600 hover:from-green-600 hover:to-emerald-700"
                >
                  {querying ? (
                    <div className="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></div>
                  ) : (
                    <Send className="w-4 h-4" />
                  )}
                </Button>
              </div>
              {queryResponse && (
                <div className="mt-4 p-4 bg-green-50 dark:bg-green-900/20 rounded-lg border border-green-100 dark:border-green-900/50">
                  <p className="text-sm">{queryResponse}</p>
                </div>
              )}
            </Card>
          </>
        )}
      </div>
    </main>
  )
}
