"use client"

import { useEffect, useState } from "react"
import axios from "axios"
import Link from "next/link"
import { useParams } from "next/navigation"
import {
  ArrowLeft,
  ArrowRight,
  CalendarRange,
  GitCompareArrows,
  Loader2,
  RefreshCw,
  TrendingDown,
  TrendingUp,
} from "lucide-react"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { decisionApi } from "@/lib/api"
import type { DecisionEngineResponse, ReplayDecisionResponse } from "@/lib/api-types"

function titleCase(value: string) {
  return value.replaceAll("_", " ").replace(/\b\w/g, (letter) => letter.toUpperCase())
}

export default function ReplayPage() {
  const params = useParams()
  const decisionId = params.id as string
  const [decision, setDecision] = useState<DecisionEngineResponse | null>(null)
  const [updatedInput, setUpdatedInput] = useState("")
  const [result, setResult] = useState<ReplayDecisionResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    decisionApi
      .get(decisionId)
      .then((value) => {
        setDecision(value)
        setUpdatedInput(value.naturalLanguageInput)
      })
      .catch(() => setError("Unable to load this decision."))
      .finally(() => setLoading(false))
  }, [decisionId])

  async function handleReplay() {
    if (updatedInput.trim().length < 10) {
      setError("Describe the updated scenario in at least 10 characters.")
      return
    }
    setSubmitting(true)
    setError(null)
    try {
      const replay = await decisionApi.replay(decisionId, updatedInput.trim())
      setResult(replay)
      setDecision(replay.decision)
      setUpdatedInput(replay.decision.naturalLanguageInput)
    } catch (requestError) {
      setError(
        axios.isAxiosError(requestError)
          ? requestError.response?.data?.message ?? "Replay failed."
          : "Replay failed.",
      )
    } finally {
      setSubmitting(false)
    }
  }

  if (loading) {
    return <main className="grid min-h-screen place-items-center bg-[#080b0d]"><Loader2 className="h-8 w-8 animate-spin text-sky-300" /></main>
  }

  return (
    <main className="min-h-screen bg-[#080b0d] text-slate-100">
      <div className="mx-auto max-w-6xl px-5 py-10 lg:px-8">
        <Link href={`/decisions/${decisionId}/analytics`} className="inline-flex items-center gap-2 text-xs font-semibold uppercase tracking-[0.18em] text-slate-500 hover:text-white">
          <ArrowLeft className="h-4 w-4" />
          Decision report
        </Link>

        <header className="mt-7 border-b border-white/10 pb-7">
          <Badge className="border-sky-400/20 bg-sky-400/10 text-sky-300">
            <GitCompareArrows className="mr-1 h-3 w-3" />
            Constraint replay
          </Badge>
          <h1 className="mt-4 text-4xl font-semibold tracking-[-0.04em]">Change the scenario, preserve the evidence.</h1>
          <p className="mt-3 max-w-3xl text-sm leading-6 text-slate-500">
            Edit budget, timeline, resources, scope, or assumptions in the narrative. The backend extracts a new
            structured version, reruns the same deterministic rules, rebuilds the bounded plan, and records the delta.
          </p>
        </header>

        <div className="mt-7 grid gap-6 lg:grid-cols-[1fr_0.9fr]">
          <Card className="border-white/10 bg-slate-950/75">
            <CardHeader className="border-b border-white/10">
              <CardTitle className="flex items-center justify-between gap-3 text-base">
                Updated decision input
                {decision && <Badge variant="outline" className="border-white/10 text-slate-500">Current v{decision.version}</Badge>}
              </CardTitle>
            </CardHeader>
            <CardContent className="p-5">
              <textarea
                value={updatedInput}
                maxLength={20000}
                onChange={(event) => setUpdatedInput(event.target.value)}
                className="min-h-96 w-full resize-y rounded-xl border border-white/10 bg-black/25 p-4 text-sm leading-7 text-slate-200 outline-none focus:border-sky-400/50 focus:ring-2 focus:ring-sky-400/10"
              />
              {error && <div className="mt-3 rounded-lg border border-red-400/20 bg-red-400/10 p-3 text-sm text-red-300">{error}</div>}
              <Button onClick={handleReplay} disabled={submitting} className="mt-4 w-full bg-sky-400 text-black hover:bg-sky-300">
                {submitting ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : <RefreshCw className="mr-2 h-4 w-4" />}
                Create and compare new version
              </Button>
            </CardContent>
          </Card>

          <div className="space-y-5">
            {!result ? (
              <Card className="border-dashed border-white/15 bg-white/[0.02]">
                <CardContent className="p-8 text-center">
                  <GitCompareArrows className="mx-auto h-9 w-9 text-slate-700" />
                  <h2 className="mt-4 font-semibold">No replay yet</h2>
                  <p className="mt-2 text-sm leading-6 text-slate-600">Change at least one constraint and run the replay to see authoritative deltas.</p>
                </CardContent>
              </Card>
            ) : (
              <>
                <Card className="overflow-hidden border-white/10 bg-slate-950/75">
                  <div className={`h-1 ${result.comparison.scoreDelta >= 0 ? "bg-green-400" : "bg-red-400"}`} />
                  <CardContent className="p-6">
                    <div className="flex items-center justify-between">
                      <div>
                        <div className="text-[10px] font-semibold uppercase tracking-[0.18em] text-slate-600">Feasibility delta</div>
                        <div className={`mt-2 flex items-center gap-2 text-4xl font-semibold ${result.comparison.scoreDelta >= 0 ? "text-green-300" : "text-red-300"}`}>
                          {result.comparison.scoreDelta >= 0 ? <TrendingUp className="h-7 w-7" /> : <TrendingDown className="h-7 w-7" />}
                          {result.comparison.scoreDelta >= 0 ? "+" : ""}{result.comparison.scoreDelta.toFixed(1)}
                        </div>
                      </div>
                      <div className="text-right">
                        <div className="text-2xl font-semibold">{Math.round(result.decision.feasibilityScore)}</div>
                        <div className="text-[10px] uppercase tracking-wider text-slate-600">New score</div>
                      </div>
                    </div>
                    <p className="mt-5 text-sm leading-6 text-slate-400">{result.comparison.mainReason}</p>
                    {result.comparison.languageSummary && <p className="mt-3 border-l-2 border-sky-400/30 pl-3 text-xs leading-5 text-slate-500">{result.comparison.languageSummary}</p>}
                  </CardContent>
                </Card>

                <Card className="border-white/10 bg-slate-950/75">
                  <CardContent className="grid grid-cols-2 divide-x divide-white/10 p-5 text-center">
                    <div>
                      <div className="text-xs text-slate-600">Version</div>
                      <div className="mt-1 text-lg font-semibold">{result.comparison.previousVersion} → {result.comparison.newVersion}</div>
                    </div>
                    <div>
                      <div className="text-xs text-slate-600">Risk</div>
                      <div className="mt-1 text-lg font-semibold">{result.comparison.riskDelta}</div>
                    </div>
                  </CardContent>
                </Card>

                <Card className="border-white/10 bg-slate-950/75">
                  <CardHeader className="border-b border-white/10"><CardTitle className="text-base">Changed fields</CardTitle></CardHeader>
                  <CardContent className="divide-y divide-white/10 p-0">
                    {result.comparison.changedFields.length === 0 ? (
                      <p className="p-5 text-sm text-slate-500">No replay-sensitive fields changed.</p>
                    ) : result.comparison.changedFields.map((change) => (
                      <div key={change.field} className="p-5">
                        <div className="text-xs font-semibold text-sky-300">{titleCase(change.field)}</div>
                        <div className="mt-2 grid grid-cols-[1fr_auto_1fr] items-center gap-3 text-xs">
                          <span className="rounded-md bg-red-400/[0.06] p-2 text-red-200/70">{change.from || "Not provided"}</span>
                          <ArrowRight className="h-4 w-4 text-slate-700" />
                          <span className="rounded-md bg-green-400/[0.06] p-2 text-green-200/70">{change.to || "Removed"}</span>
                        </div>
                      </div>
                    ))}
                  </CardContent>
                </Card>

                {result.comparison.planChanges.length > 0 && (
                  <Card className="border-white/10 bg-slate-950/75">
                    <CardHeader className="border-b border-white/10">
                      <CardTitle className="flex items-center gap-2 text-base"><CalendarRange className="h-4 w-4 text-green-300" />Plan changes</CardTitle>
                    </CardHeader>
                    <CardContent className="divide-y divide-white/10 p-0">
                      {result.comparison.planChanges.map((change, index) => (
                        <div key={`${change.type}-${index}`} className="p-5">
                          <div className="text-xs font-semibold">{titleCase(change.type)}</div>
                          <p className="mt-2 text-xs leading-5 text-slate-400">{change.impact}</p>
                        </div>
                      ))}
                    </CardContent>
                  </Card>
                )}

                <Link href={`/decisions/${decisionId}/analytics`}>
                  <Button variant="outline" className="w-full border-white/10">Open updated report</Button>
                </Link>
              </>
            )}
          </div>
        </div>
      </div>
    </main>
  )
}
