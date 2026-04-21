"use client"

import { useState } from "react"
import { useRouter } from "next/navigation"
import Link from "next/link"
import {
  ChevronLeft, Plus, X, Loader2, Save, PlayCircle,
  DollarSign, Clock, Users, Layers
} from "lucide-react"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Badge } from "@/components/ui/badge"
import { useAuth } from "@/lib/auth-context"
import { useToast } from "@/hooks/use-toast"
import { decisionApi } from "@/lib/api"
import type { ProjectEvaluationResponse } from "@/lib/api-types"

const PROJECT_TYPES = [
  "SaaS Platform", "Mobile App", "E-commerce", "API / Backend",
  "Data Pipeline", "Internal Tool", "AI/ML System", "General",
]

const FEATURE_SUGGESTIONS = [
  "Authentication", "Dashboard", "Payments", "Real-time", "Notifications",
  "Analytics", "File Upload", "Search", "AI Integration", "Multi-tenant",
]

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

export default function NewDecisionPage() {
  const router = useRouter()
  const { user } = useAuth()
  const { toast } = useToast()

  const [projectType, setProjectType] = useState("General")
  const [features, setFeatures] = useState<string[]>([])
  const [featureInput, setFeatureInput] = useState("")
  const [budget, setBudget] = useState("")
  const [timeline, setTimeline] = useState("")
  const [teamSize, setTeamSize] = useState("1")

  const [evaluating, setEvaluating] = useState(false)
  const [saving, setSaving] = useState(false)
  const [result, setResult] = useState<ProjectEvaluationResponse | null>(null)

  const addFeature = (f: string) => {
    const trimmed = f.trim()
    if (trimmed && !features.includes(trimmed)) setFeatures(prev => [...prev, trimmed])
    setFeatureInput("")
  }

  const removeFeature = (f: string) => setFeatures(prev => prev.filter(x => x !== f))

  const handleFeatureKey = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === "Enter" || e.key === ",") { e.preventDefault(); addFeature(featureInput) }
  }

  const validate = () => {
    if (!user) { router.push("/login"); return false }
    if (features.length === 0) { toast({ title: "Add at least one feature", variant: "destructive" }); return false }
    if (!budget || Number(budget) <= 0) { toast({ title: "Enter a valid budget", variant: "destructive" }); return false }
    if (!timeline || Number(timeline) <= 0) { toast({ title: "Enter a valid timeline", variant: "destructive" }); return false }
    return true
  }

  const buildRequest = () => ({
    projectType,
    features,
    budgetUsd: Number(budget),
    timelineMonths: Number(timeline),
    teamSize: Math.max(1, parseInt(teamSize) || 1),
    requestAiEnhancement: false,
  })

  const handleEvaluate = async () => {
    if (!validate()) return
    setEvaluating(true)
    try {
      const res = await decisionApi.evaluateProject(buildRequest())
      setResult(res)
    } catch {
      toast({ title: "Evaluation failed", description: "Check your connection and try again.", variant: "destructive" })
    } finally { setEvaluating(false) }
  }

  const handleSave = async () => {
    if (!validate()) return
    setSaving(true)
    try {
      const saved = await decisionApi.createAndEvaluate(buildRequest())
      toast({ title: "Decision saved!", description: `Score: ${Math.round(saved.analysis?.feasibility.score ?? 0)}/100` })
      router.push(`/decisions/${saved.id}`)
    } catch {
      toast({ title: "Save failed", description: "Please try again.", variant: "destructive" })
    } finally { setSaving(false) }
  }

  const verdictColor = (v: string) =>
    v === "Feasible" ? "text-green-600" : v === "Risky" ? "text-yellow-600" : "text-red-600"

  const verdictBadge = (v: string) =>
    v === "Feasible" ? "bg-green-100 text-green-800" :
    v === "Risky" ? "bg-yellow-100 text-yellow-800" : "bg-red-100 text-red-800"

  return (
    <div className="min-h-screen bg-background">
      <div className="max-w-4xl mx-auto px-4 py-8 space-y-6">

        <div className="flex items-center gap-3">
          <Link href="/decisions">
            <Button variant="ghost" size="sm"><ChevronLeft className="h-4 w-4 mr-1" />Back</Button>
          </Link>
          <div>
            <h1 className="text-2xl font-bold">New Project Evaluation</h1>
            <p className="text-sm text-muted-foreground">Get an instant feasibility score — no AI required</p>
          </div>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">

          {/* Form */}
          <div className="space-y-4">

            <Card>
              <CardHeader className="pb-3">
                <CardTitle className="text-base flex items-center gap-2"><Layers className="h-4 w-4" />Project Type</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="flex flex-wrap gap-2">
                  {PROJECT_TYPES.map(t => (
                    <button key={t} onClick={() => setProjectType(t)}
                      className={`px-3 py-1.5 rounded-full text-sm border transition-colors ${
                        projectType === t ? "bg-primary text-primary-foreground border-primary" : "border-input hover:bg-accent"
                      }`}>{t}</button>
                  ))}
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader className="pb-3"><CardTitle className="text-base">Features</CardTitle></CardHeader>
              <CardContent className="space-y-3">
                <div className="flex gap-2">
                  <Input placeholder="Type a feature, press Enter" value={featureInput}
                    onChange={e => setFeatureInput(e.target.value)} onKeyDown={handleFeatureKey} />
                  <Button size="sm" variant="outline" onClick={() => addFeature(featureInput)}>
                    <Plus className="h-4 w-4" />
                  </Button>
                </div>
                {features.length > 0 && (
                  <div className="flex flex-wrap gap-2">
                    {features.map(f => (
                      <Badge key={f} variant="secondary" className="gap-1 pr-1">
                        {f}
                        <button onClick={() => removeFeature(f)} className="hover:text-destructive"><X className="h-3 w-3" /></button>
                      </Badge>
                    ))}
                  </div>
                )}
                <div className="flex flex-wrap gap-1.5 pt-1">
                  {FEATURE_SUGGESTIONS.filter(s => !features.includes(s)).map(s => (
                    <button key={s} onClick={() => addFeature(s)}
                      className="text-xs px-2 py-1 rounded border border-dashed hover:bg-accent text-muted-foreground">
                      + {s}
                    </button>
                  ))}
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardContent className="pt-4 grid grid-cols-3 gap-4">
                <div className="space-y-1">
                  <Label className="flex items-center gap-1 text-xs"><DollarSign className="h-3 w-3" />Budget (USD)</Label>
                  <Input type="number" placeholder="50000" value={budget} onChange={e => setBudget(e.target.value)} />
                </div>
                <div className="space-y-1">
                  <Label className="flex items-center gap-1 text-xs"><Clock className="h-3 w-3" />Timeline (mo)</Label>
                  <Input type="number" placeholder="6" value={timeline} onChange={e => setTimeline(e.target.value)} />
                </div>
                <div className="space-y-1">
                  <Label className="flex items-center gap-1 text-xs"><Users className="h-3 w-3" />Team Size</Label>
                  <Input type="number" placeholder="3" min="1" value={teamSize} onChange={e => setTeamSize(e.target.value)} />
                </div>
              </CardContent>
            </Card>

            <div className="flex gap-3">
              <Button className="flex-1" variant="outline" onClick={handleEvaluate} disabled={evaluating || saving}>
                {evaluating ? <Loader2 className="h-4 w-4 mr-2 animate-spin" /> : <PlayCircle className="h-4 w-4 mr-2" />}
                Evaluate
              </Button>
              <Button className="flex-1" onClick={handleSave} disabled={saving || evaluating}>
                {saving ? <Loader2 className="h-4 w-4 mr-2 animate-spin" /> : <Save className="h-4 w-4 mr-2" />}
                Save
              </Button>
            </div>
          </div>

          {/* Results */}
          <div>
            {!result ? (
              <Card className="h-full flex items-center justify-center min-h-[300px]">
                <div className="text-center text-muted-foreground p-8">
                  <PlayCircle className="h-10 w-10 mx-auto mb-3 opacity-30" />
                  <p className="text-sm">Click <strong>Evaluate</strong> to get an instant feasibility score</p>
                </div>
              </Card>
            ) : (
              <div className="space-y-4">

                <Card>
                  <CardContent className="pt-5">
                    <div className="flex items-center justify-between mb-4">
                      <div>
                        <p className="text-4xl font-bold">{Math.round(result.feasibility.score)}<span className="text-lg text-muted-foreground">/100</span></p>
                        <p className={`text-sm font-medium mt-0.5 ${verdictColor(result.feasibility.verdict)}`}>{result.feasibility.verdict}</p>
                      </div>
                      <span className={`px-3 py-1 rounded-full text-sm font-medium ${verdictBadge(result.feasibility.verdict)}`}>
                        {result.feasibility.verdict}
                      </span>
                    </div>
                    <div className="space-y-2">
                      <ScoreBar label="Budget Fit" value={result.feasibility.budgetFitScore} />
                      <ScoreBar label="Timeline Fit" value={result.feasibility.timelineFitScore} />
                      <ScoreBar label="Team Capacity" value={result.feasibility.teamCapacityScore} />
                      <ScoreBar label="Simplicity" value={result.feasibility.complexityScore} />
                    </div>
                  </CardContent>
                </Card>

                <Card>
                  <CardContent className="pt-4 grid grid-cols-3 gap-4 text-center">
                    <div>
                      <p className="text-xs text-muted-foreground">Est. Cost</p>
                      <p className="font-semibold">${result.feasibility.estimatedCostUsd.toLocaleString()}</p>
                    </div>
                    <div>
                      <p className="text-xs text-muted-foreground">Est. Months</p>
                      <p className="font-semibold">{result.feasibility.estimatedMonths.toFixed(1)}</p>
                    </div>
                    <div>
                      <p className="text-xs text-muted-foreground">Req. Team</p>
                      <p className="font-semibold">{result.feasibility.requiredTeamSize}</p>
                    </div>
                  </CardContent>
                </Card>

                {result.feasibility.issues.length > 0 && (
                  <Card>
                    <CardHeader className="pb-2"><CardTitle className="text-sm">Issues</CardTitle></CardHeader>
                    <CardContent className="space-y-2">
                      {result.feasibility.issues.map((issue, i) => (
                        <div key={i} className="flex items-start gap-2 text-sm">
                          <span className={`mt-0.5 px-1.5 py-0.5 rounded text-xs font-medium ${
                            issue.severity === "High" ? "bg-red-100 text-red-700" :
                            issue.severity === "Medium" ? "bg-yellow-100 text-yellow-700" : "bg-blue-100 text-blue-700"
                          }`}>{issue.severity}</span>
                          <span className="text-muted-foreground">{issue.message}</span>
                        </div>
                      ))}
                    </CardContent>
                  </Card>
                )}

                {result.feasibility.suggestedAdjustments.length > 0 && (
                  <Card>
                    <CardHeader className="pb-2"><CardTitle className="text-sm">Suggestions</CardTitle></CardHeader>
                    <CardContent className="space-y-1">
                      {result.feasibility.suggestedAdjustments.map((s, i) => (
                        <p key={i} className="text-sm text-muted-foreground">• {s.description}{s.quantitativeImpact ? ` (${s.quantitativeImpact})` : ""}</p>
                      ))}
                    </CardContent>
                  </Card>
                )}

                {result.plan.phases.length > 0 && (
                  <Card>
                    <CardHeader className="pb-2"><CardTitle className="text-sm">Timeline — {result.plan.totalMonths.toFixed(1)} months</CardTitle></CardHeader>
                    <CardContent className="space-y-1">
                      {result.plan.phases.map((ph, i) => (
                        <div key={i} className="flex justify-between text-sm">
                          <span className="text-muted-foreground">{ph.name}</span>
                          <span className="font-medium">{ph.durationMonths.toFixed(1)} mo</span>
                        </div>
                      ))}
                    </CardContent>
                  </Card>
                )}
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  )
}

