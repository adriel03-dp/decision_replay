"use client"

import { useEffect, useMemo, useState } from "react"
import axios from "axios"
import Link from "next/link"
import { useParams } from "next/navigation"
import {
  AlertTriangle,
  ArrowLeft,
  CalendarDays,
  CheckCircle2,
  Download,
  FileQuestion,
  Gauge,
  History,
  Loader2,
  RefreshCw,
  Route,
  Scale,
  ShieldAlert,
} from "lucide-react"
import { Bar, BarChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from "recharts"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { decisionApi } from "@/lib/api"
import type { DecisionEngineResponse } from "@/lib/api-types"

function scoreColor(score: number) {
  if (score >= 75) return "#4ade80"
  if (score >= 55) return "#fbbf24"
  return "#f87171"
}

function riskClasses(risk: string) {
  if (risk === "Low") return "border-green-400/20 bg-green-400/10 text-green-300"
  if (risk === "Medium") return "border-amber-400/20 bg-amber-400/10 text-amber-300"
  return "border-red-400/20 bg-red-400/10 text-red-300"
}

function titleCase(value: string) {
  return value.replaceAll("_", " ").replace(/\b\w/g, (letter) => letter.toUpperCase())
}

export default function DecisionAnalyticsPage() {
  const params = useParams()
  const decisionId = params.id as string
  const [decision, setDecision] = useState<DecisionEngineResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [exporting, setExporting] = useState<"pdf" | "excel" | null>(null)
  const [regenerating, setRegenerating] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    decisionApi
      .get(decisionId)
      .then(setDecision)
      .catch((requestError) => {
        setError(
          axios.isAxiosError(requestError)
            ? requestError.response?.data?.message ?? "Unable to load this decision."
            : "Unable to load this decision.",
        )
      })
      .finally(() => setLoading(false))
  }, [decisionId])

  const factorData = useMemo(
    () =>
      decision?.factorBreakdown.map((factor) => ({
        name: titleCase(factor.factor),
        score: factor.score,
        contribution: factor.weightedScore,
      })) ?? [],
    [decision],
  )

  async function regeneratePlan() {
    if (!decision) return
    setRegenerating(true)
    try {
      const plan = await decisionApi.generatePlan(decision.decisionId)
      setDecision({ ...decision, plan })
    } finally {
      setRegenerating(false)
    }
  }

  async function exportPlan(format: "pdf" | "excel") {
    if (!decision?.plan) return
    setExporting(format)
    try {
      await decisionApi.downloadPlan(decision.decisionId, decision.plan.planId, format)
    } finally {
      setExporting(null)
    }
  }

  if (loading) {
    return (
      <main className="grid min-h-screen place-items-center bg-[#080b0d]">
        <Loader2 className="h-8 w-8 animate-spin text-green-300" />
      </main>
    )
  }

  if (!decision || error) {
    return (
      <main className="grid min-h-screen place-items-center bg-[#080b0d] px-5 text-slate-100">
        <Card className="max-w-lg border-red-400/20 bg-red-400/[0.04]">
          <CardContent className="p-8 text-center">
            <FileQuestion className="mx-auto h-9 w-9 text-red-300" />
            <h1 className="mt-4 text-xl font-semibold">Decision report unavailable</h1>
            <p className="mt-2 text-sm text-slate-500">{error}</p>
            <Link href="/decisions"><Button className="mt-5">Back to decisions</Button></Link>
          </CardContent>
        </Card>
      </main>
    )
  }

  return (
    <main className="min-h-screen bg-[#080b0d] text-slate-100">
      <div
        className="pointer-events-none fixed inset-0 opacity-70"
        style={{
          background:
            "radial-gradient(circle at 10% 4%, rgba(34,197,94,.12), transparent 28%), radial-gradient(circle at 90% 18%, rgba(14,165,233,.09), transparent 25%), repeating-linear-gradient(90deg, rgba(255,255,255,.018) 0, rgba(255,255,255,.018) 1px, transparent 1px, transparent 76px)",
        }}
      />
      <div className="relative mx-auto max-w-7xl px-5 py-8 lg:px-8">
        <header className="overflow-hidden rounded-2xl border border-white/10 bg-slate-950/80">
          <div className="h-1 bg-gradient-to-r from-green-400 via-emerald-400 to-sky-400" />
          <div className="grid gap-7 p-6 lg:grid-cols-[1fr_auto] lg:p-8">
            <div>
              <Link href="/decisions" className="inline-flex items-center gap-2 text-xs font-semibold uppercase tracking-[0.18em] text-slate-500 hover:text-white">
                <ArrowLeft className="h-4 w-4" />
                Decision register
              </Link>
              <div className="mt-6 flex flex-wrap gap-2">
                <Badge variant="outline" className="border-white/10 text-slate-400">{decision.domain}</Badge>
                <Badge variant="outline" className="border-white/10 text-slate-400">Version {decision.version}</Badge>
                <Badge className={riskClasses(decision.riskLevel)}>{decision.riskLevel} risk</Badge>
              </div>
              <h1 className="mt-4 max-w-3xl text-3xl font-semibold tracking-[-0.04em] sm:text-4xl">{decision.title}</h1>
              <p className="mt-3 max-w-3xl text-sm leading-6 text-slate-400">{decision.goal}</p>
              <p className="mt-4 max-w-4xl border-l-2 border-sky-400/40 pl-4 text-sm leading-6 text-slate-300">
                {decision.explanation}
              </p>
            </div>
            <div className="flex items-center gap-5 lg:border-l lg:border-white/10 lg:pl-8">
              <div
                className="grid h-32 w-32 place-items-center rounded-full"
                style={{
                  background: `conic-gradient(${scoreColor(decision.feasibilityScore)} ${decision.feasibilityScore * 3.6}deg, rgba(255,255,255,.07) 0deg)`,
                }}
              >
                <div className="grid h-[104px] w-[104px] place-items-center rounded-full bg-[#0b0f13] text-center">
                  <div>
                    <div className="text-4xl font-semibold">{Math.round(decision.feasibilityScore)}</div>
                    <div className="text-[9px] uppercase tracking-[0.2em] text-slate-600">Feasibility</div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </header>

        <section className="mt-5 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          {[
            [Scale, "Decision factors", String(decision.factorBreakdown.length), "Signals shaping this outcome"],
            [ShieldAlert, "Active risks", String(decision.risks.length), `${decision.riskLevel} aggregate level`],
            [FileQuestion, "Missing evidence", String(decision.missingFields.length), "Worth clarifying before commitment"],
            [History, "Audit events", String(decision.auditTrail.length), `Current version ${decision.version}`],
          ].map(([Icon, label, value, detail]) => {
            const IconComponent = Icon as typeof Scale
            return (
              <Card key={label as string} className="border-white/10 bg-white/[0.025]">
                <CardContent className="p-5">
                  <IconComponent className="h-4 w-4 text-green-300" />
                  <div className="mt-5 text-3xl font-semibold">{value as string}</div>
                  <div className="mt-1 text-xs font-semibold text-slate-300">{label as string}</div>
                  <div className="mt-1 text-[11px] text-slate-600">{detail as string}</div>
                </CardContent>
              </Card>
            )
          })}
        </section>

        <section className="mt-5 grid gap-5 lg:grid-cols-[1.05fr_0.95fr]">
          <Card className="border-white/10 bg-slate-950/75">
            <CardHeader className="border-b border-white/10">
              <CardTitle className="flex items-center gap-2 text-base">
                <Gauge className="h-4 w-4 text-sky-300" />
                Feasibility factor performance
              </CardTitle>
            </CardHeader>
            <CardContent className="p-5">
              <ResponsiveContainer width="100%" height={300}>
                <BarChart data={factorData} layout="vertical" margin={{ left: 24, right: 20 }}>
                  <CartesianGrid horizontal={false} stroke="rgba(255,255,255,.06)" />
                  <XAxis type="number" domain={[0, 100]} tick={{ fill: "#64748b", fontSize: 10 }} axisLine={false} tickLine={false} />
                  <YAxis type="category" dataKey="name" width={138} tick={{ fill: "#94a3b8", fontSize: 10 }} axisLine={false} tickLine={false} />
                  <Tooltip
                    contentStyle={{ background: "#0f1720", border: "1px solid rgba(255,255,255,.1)", borderRadius: 8 }}
                    formatter={(value) => [`${Number(value).toFixed(1)}/100`, "Factor score"]}
                  />
                  <Bar dataKey="score" fill="#38bdf8" radius={[0, 5, 5, 0]} barSize={16} />
                </BarChart>
              </ResponsiveContainer>
            </CardContent>
          </Card>

          <Card className="border-white/10 bg-slate-950/75">
            <CardHeader className="border-b border-white/10">
              <CardTitle className="text-base">Structured decision data</CardTitle>
            </CardHeader>
            <CardContent className="p-5">
              <dl className="divide-y divide-white/10">
                {Object.entries(decision.structuredFields).map(([field, value]) => (
                  <div key={field} className="grid grid-cols-[130px_1fr] gap-4 py-3 text-xs">
                    <dt className="text-slate-600">{titleCase(field)}</dt>
                    <dd className="text-right leading-5 text-slate-300">{value}</dd>
                  </div>
                ))}
              </dl>
              {decision.missingFields.length > 0 && (
                <div className="mt-5 rounded-xl border border-amber-400/20 bg-amber-400/[0.06] p-4">
                  <div className="text-xs font-semibold text-amber-300">Missing fields</div>
                  <div className="mt-2 flex flex-wrap gap-2">
                    {decision.missingFields.map((field) => (
                      <Badge key={field} variant="outline" className="border-amber-400/20 text-amber-200">{titleCase(field)}</Badge>
                    ))}
                  </div>
                </div>
              )}
            </CardContent>
          </Card>
        </section>

        <Card className="mt-5 border-white/10 bg-slate-950/75">
          <CardHeader className="border-b border-white/10">
            <CardTitle className="text-base">Insight summary</CardTitle>
          </CardHeader>
          <CardContent className="p-0">
            <div className="divide-y divide-white/10">
              {decision.factorBreakdown.map((factor) => (
                <article key={factor.factor} className="grid gap-3 p-5 md:grid-cols-[190px_1fr_auto]">
                  <div>
                    <div className="text-sm font-medium">{titleCase(factor.factor)}</div>
                    <div className="mt-1 text-xs text-slate-600">{Math.round(factor.weight * 100)}% weight</div>
                  </div>
                  <div>
                    <p className="text-xs leading-5 text-slate-400">{factor.reason}</p>
                    {factor.assumption && <p className="mt-2 text-xs text-amber-300/80">Assumption: {factor.assumption}</p>}
                  </div>
                  <div className="text-right">
                    <div className="text-xl font-semibold" style={{ color: scoreColor(factor.score) }}>{Math.round(factor.score)}</div>
                    <div className="text-[10px] uppercase tracking-wider text-slate-600">{factor.confidence} confidence</div>
                  </div>
                </article>
              ))}
            </div>
          </CardContent>
        </Card>

        <section className="mt-5 grid gap-5 lg:grid-cols-2">
          <Card className="border-white/10 bg-slate-950/75">
            <CardHeader className="border-b border-white/10"><CardTitle className="text-base">Risk register</CardTitle></CardHeader>
            <CardContent className="divide-y divide-white/10 p-0">
              {decision.risks.length === 0 ? (
                <div className="flex items-center gap-2 p-5 text-sm text-slate-400"><CheckCircle2 className="h-4 w-4 text-green-300" />No active risks found.</div>
              ) : decision.risks.map((risk) => (
                <article key={risk.code} className="p-5">
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <div className="text-sm font-medium">{titleCase(risk.factor)}</div>
                      <p className="mt-1 text-xs leading-5 text-slate-400">{risk.message}</p>
                    </div>
                    <Badge className={riskClasses(risk.severity)}>{risk.severity}</Badge>
                  </div>
                  <p className="mt-3 text-xs leading-5 text-green-200/70">Mitigation: {risk.mitigation}</p>
                </article>
              ))}
            </CardContent>
          </Card>

          <Card className="border-white/10 bg-slate-950/75">
            <CardHeader className="border-b border-white/10"><CardTitle className="text-base">Assumptions and actions</CardTitle></CardHeader>
            <CardContent className="space-y-5 p-5">
              <div>
                <h2 className="text-xs font-semibold uppercase tracking-wider text-slate-600">Assumptions</h2>
                <ul className="mt-3 space-y-2">
                  {decision.assumptions.length === 0 ? <li className="text-xs text-slate-500">No assumptions recorded.</li> :
                    decision.assumptions.map((item) => <li key={item} className="text-xs leading-5 text-slate-400">- {item}</li>)}
                </ul>
              </div>
              <div className="border-t border-white/10 pt-5">
                <h2 className="text-xs font-semibold uppercase tracking-wider text-slate-600">Recommendations</h2>
                <ul className="mt-3 space-y-2">
                  {decision.recommendations.map((item) => <li key={item} className="text-xs leading-5 text-slate-400">- {item}</li>)}
                </ul>
              </div>
            </CardContent>
          </Card>
        </section>

        <Card className="mt-5 border-white/10 bg-slate-950/75">
          <CardHeader className="border-b border-white/10">
            <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
              <div>
                <CardTitle className="flex items-center gap-2 text-base"><Route className="h-4 w-4 text-green-300" />Bounded action plan</CardTitle>
                <p className="mt-1 text-xs text-slate-600">Built from the decision context, constraints, risk signals, and desired outcome.</p>
              </div>
              <div className="flex flex-wrap gap-2">
                <Button variant="outline" size="sm" onClick={regeneratePlan} disabled={regenerating}>
                  {regenerating ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : <RefreshCw className="mr-2 h-4 w-4" />}
                  Regenerate
                </Button>
                <Button variant="outline" size="sm" onClick={() => exportPlan("pdf")} disabled={!decision.plan || exporting !== null}>
                  <Download className="mr-2 h-4 w-4" />PDF
                </Button>
                <Button variant="outline" size="sm" onClick={() => exportPlan("excel")} disabled={!decision.plan || exporting !== null}>
                  <Download className="mr-2 h-4 w-4" />Excel
                </Button>
              </div>
            </div>
          </CardHeader>
          <CardContent className="p-5">
            {!decision.plan ? (
              <div className="py-8 text-center text-sm text-slate-500">No plan is stored for this version.</div>
            ) : (
              <>
                <div className="mb-6 flex flex-wrap gap-3 text-xs text-slate-500">
                  <span>{decision.plan.timelineMonths} months</span>
                  <span>•</span>
                  <span>{new Date(decision.plan.startDate).toLocaleDateString()} to {new Date(decision.plan.endDate).toLocaleDateString()}</span>
                  <span>•</span>
                  <span>{decision.plan.phases.length} phases</span>
                </div>
                <div className="space-y-5">
                  {decision.plan.phases.map((phase, index) => (
                    <article key={phase.phaseKey} className="rounded-xl border border-white/10 bg-white/[0.02] p-5">
                      <div className="flex flex-wrap items-start justify-between gap-3">
                        <div>
                          <div className="text-[10px] font-semibold uppercase tracking-[0.18em] text-green-300">Phase {index + 1}</div>
                          <h2 className="mt-1 text-lg font-semibold">{phase.phaseName}</h2>
                          <p className="mt-1 text-xs leading-5 text-slate-500">{phase.goal}</p>
                        </div>
                        <Badge variant="outline" className="border-white/10 text-slate-400">Weeks {phase.startWeek}-{phase.endWeek}</Badge>
                      </div>
                      <div className="mt-5 grid gap-3 md:grid-cols-2">
                        {phase.tasks.map((task) => (
                          <div key={task.taskId} className="rounded-lg border border-white/10 bg-black/20 p-4">
                            <div className="flex items-start justify-between gap-3">
                              <h3 className="text-sm font-medium">{task.taskName}</h3>
                              <Badge variant="outline" className="border-white/10 text-[10px] text-slate-500">{task.priority}</Badge>
                            </div>
                            <p className="mt-2 text-xs leading-5 text-slate-400">{task.description}</p>
                            <div className="mt-3 text-[11px] text-green-200/70">Success: {task.successCriteria}</div>
                            {task.riskNotes && <div className="mt-2 text-[11px] text-amber-200/70">Risk: {task.riskNotes}</div>}
                          </div>
                        ))}
                      </div>
                    </article>
                  ))}
                </div>
              </>
            )}
          </CardContent>
        </Card>

        <Card className="mt-5 border-white/10 bg-slate-950/75">
          <CardHeader className="border-b border-white/10">
            <CardTitle className="flex items-center gap-2 text-base"><History className="h-4 w-4 text-sky-300" />Audit trail</CardTitle>
          </CardHeader>
          <CardContent className="divide-y divide-white/10 p-0">
            {decision.auditTrail.map((entry) => (
              <div key={entry.id} className="grid gap-2 p-5 sm:grid-cols-[150px_1fr_auto]">
                <time className="text-xs text-slate-600">{new Date(entry.timestamp).toLocaleString()}</time>
                <div>
                  <div className="text-xs font-medium">{entry.summary}</div>
                  <div className="mt-1 text-[11px] text-slate-600">{entry.action}</div>
                </div>
                <Badge variant="outline" className="w-fit border-white/10 text-slate-500">v{entry.version}</Badge>
              </div>
            ))}
          </CardContent>
        </Card>

        <footer className="mt-6 flex flex-col gap-3 border-t border-white/10 pt-6 sm:flex-row sm:items-center sm:justify-between">
          <div className="text-xs text-slate-600">Decision ID {decision.decisionId}</div>
          <Link href={`/decisions/${decision.decisionId}/analysis`}>
            <Button className="bg-sky-400 text-black hover:bg-sky-300">
              <CalendarDays className="mr-2 h-4 w-4" />
              Create replay version
            </Button>
          </Link>
        </footer>
      </div>
    </main>
  )
}
