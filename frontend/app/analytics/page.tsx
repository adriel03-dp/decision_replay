"use client"

import { useEffect, useMemo, useState } from "react"
import Link from "next/link"
import { Activity, AlertTriangle, BarChart3, Plus, ShieldAlert, Target } from "lucide-react"
import { Bar, BarChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from "recharts"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { decisionApi } from "@/lib/api"
import type { DecisionEngineSummary } from "@/lib/api-types"

export default function AnalyticsPage() {
  const [decisions, setDecisions] = useState<DecisionEngineSummary[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    decisionApi.list().then(setDecisions).finally(() => setLoading(false))
  }, [])

  const metrics = useMemo(() => {
    const average = decisions.length
      ? decisions.reduce((sum, item) => sum + item.feasibilityScore, 0) / decisions.length
      : 0
    const highRisk = decisions.filter((item) => item.riskLevel === "High" || item.riskLevel === "Critical").length
    const missing = decisions.reduce((sum, item) => sum + item.missingFieldCount, 0)
    const domains = Object.entries(
      decisions.reduce<Record<string, { count: number; score: number }>>((result, item) => {
        result[item.domain] ??= { count: 0, score: 0 }
        result[item.domain].count += 1
        result[item.domain].score += item.feasibilityScore
        return result
      }, {}),
    ).map(([domain, value]) => ({
      domain: domain.replaceAll("_", " "),
      decisions: value.count,
      averageScore: Math.round(value.score / value.count),
    }))
    return { average, highRisk, missing, domains }
  }, [decisions])

  return (
    <main className="min-h-screen bg-[#080b0d] text-slate-100">
      <div className="mx-auto max-w-7xl px-5 py-10 lg:px-8">
        <header className="flex flex-col gap-5 border-b border-white/10 pb-8 sm:flex-row sm:items-end sm:justify-between">
          <div>
            <div className="text-xs font-semibold uppercase tracking-[0.2em] text-sky-300">Portfolio view</div>
            <h1 className="mt-3 text-4xl font-semibold tracking-[-0.04em]">Decision analytics</h1>
            <p className="mt-2 text-sm text-slate-500">Aggregated from stored deterministic engine outputs.</p>
          </div>
          <Link href="/decisions/new"><Button className="bg-green-400 text-black hover:bg-green-300"><Plus className="mr-2 h-4 w-4" />New decision</Button></Link>
        </header>

        {loading ? (
          <div className="mt-8 h-72 animate-pulse rounded-2xl bg-white/[0.04]" />
        ) : decisions.length === 0 ? (
          <Card className="mt-8 border-dashed border-white/15 bg-white/[0.02]">
            <CardContent className="p-12 text-center">
              <BarChart3 className="mx-auto h-9 w-9 text-slate-700" />
              <h2 className="mt-4 font-semibold">No analytics yet</h2>
              <p className="mt-2 text-sm text-slate-600">Analyze a decision to populate this dashboard.</p>
            </CardContent>
          </Card>
        ) : (
          <>
            <section className="mt-7 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
              {[
                [Target, "Decisions", decisions.length, "Stored decision records"],
                [Activity, "Average score", `${Math.round(metrics.average)}/100`, "Across current versions"],
                [ShieldAlert, "High risk", metrics.highRisk, "High or critical"],
                [AlertTriangle, "Missing fields", metrics.missing, "Evidence still required"],
              ].map(([Icon, label, value, detail]) => {
                const IconComponent = Icon as typeof Target
                return (
                  <Card key={label as string} className="border-white/10 bg-white/[0.025]">
                    <CardContent className="p-5">
                      <IconComponent className="h-4 w-4 text-sky-300" />
                      <div className="mt-5 text-3xl font-semibold">{value as string | number}</div>
                      <div className="mt-1 text-xs font-semibold">{label as string}</div>
                      <div className="mt-1 text-[11px] text-slate-600">{detail as string}</div>
                    </CardContent>
                  </Card>
                )
              })}
            </section>

            <Card className="mt-5 border-white/10 bg-slate-950/75">
              <CardHeader className="border-b border-white/10"><CardTitle className="text-base">Domain performance</CardTitle></CardHeader>
              <CardContent className="p-5">
                <ResponsiveContainer width="100%" height={340}>
                  <BarChart data={metrics.domains}>
                    <CartesianGrid vertical={false} stroke="rgba(255,255,255,.06)" />
                    <XAxis dataKey="domain" tick={{ fill: "#64748b", fontSize: 11 }} axisLine={false} tickLine={false} />
                    <YAxis domain={[0, 100]} tick={{ fill: "#64748b", fontSize: 11 }} axisLine={false} tickLine={false} />
                    <Tooltip contentStyle={{ background: "#0f1720", border: "1px solid rgba(255,255,255,.1)", borderRadius: 8 }} />
                    <Bar dataKey="averageScore" fill="#4ade80" radius={[5, 5, 0, 0]} />
                  </BarChart>
                </ResponsiveContainer>
              </CardContent>
            </Card>
          </>
        )}
      </div>
    </main>
  )
}
