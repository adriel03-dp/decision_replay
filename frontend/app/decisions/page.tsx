"use client"

import { useEffect, useMemo, useState } from "react"
import Link from "next/link"
import { AlertTriangle, ArrowUpRight, Plus, Search, Trash2 } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Card, CardContent } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { decisionApi } from "@/lib/api"
import type { DecisionEngineSummary } from "@/lib/api-types"
import { useToast } from "@/hooks/use-toast"
import { useCustomConfirm } from "@/components/ui/custom-dialogs"

function scoreTone(score: number) {
  if (score >= 75) return "text-green-300"
  if (score >= 55) return "text-amber-300"
  return "text-red-300"
}

function riskTone(risk: string) {
  if (risk === "Low") return "border-green-400/20 bg-green-400/10 text-green-300"
  if (risk === "Medium") return "border-amber-400/20 bg-amber-400/10 text-amber-300"
  return "border-red-400/20 bg-red-400/10 text-red-300"
}

export default function DecisionsPage() {
  const { toast } = useToast()
  const { showConfirm, ConfirmComponent } = useCustomConfirm()
  const [decisions, setDecisions] = useState<DecisionEngineSummary[]>([])
  const [loading, setLoading] = useState(true)
  const [query, setQuery] = useState("")
  const [domain, setDomain] = useState("all")

  useEffect(() => {
    decisionApi
      .list()
      .then(setDecisions)
      .catch(() => toast({ title: "Unable to load decisions", variant: "destructive" }))
      .finally(() => setLoading(false))
  }, [toast])

  const domains = useMemo(
    () => Array.from(new Set(decisions.map((item) => item.domain))).sort(),
    [decisions],
  )

  const filtered = decisions.filter((item) => {
    const search = query.trim().toLowerCase()
    return (
      (!search ||
        item.title.toLowerCase().includes(search) ||
        item.decisionId.toLowerCase().includes(search)) &&
      (domain === "all" || item.domain === domain)
    )
  })

  function removeDecision(decisionId: string) {
    showConfirm(
      "Delete this decision and every stored version? This cannot be undone.",
      async () => {
        try {
          await decisionApi.remove(decisionId)
          setDecisions((current) => current.filter((item) => item.decisionId !== decisionId))
          toast({ title: "Decision deleted" })
        } catch {
          toast({ title: "Delete failed", variant: "destructive" })
        }
      },
      { title: "Delete decision", confirmText: "Delete", variant: "destructive" },
    )
  }

  return (
    <main className="min-h-screen bg-[#080b0d] text-slate-100">
      <div className="mx-auto max-w-7xl px-5 py-10 lg:px-8">
        <header className="flex flex-col gap-6 border-b border-white/10 pb-8 sm:flex-row sm:items-end sm:justify-between">
          <div>
            <div className="text-xs font-semibold uppercase tracking-[0.2em] text-green-300">
              Versioned decision register
            </div>
            <h1 className="mt-3 text-4xl font-semibold tracking-[-0.04em]">Decision Replay</h1>
            <p className="mt-2 text-sm text-slate-500">
              Clear decision profiles, visible assumptions, and every version in one place.
            </p>
          </div>
          <Link href="/decisions/new">
            <Button className="bg-green-400 text-black hover:bg-green-300">
              <Plus className="mr-2 h-4 w-4" />
              Analyze a decision
            </Button>
          </Link>
        </header>

        <div className="mt-7 grid gap-3 sm:grid-cols-[1fr_240px]">
          <div className="flex items-center gap-3 rounded-xl border border-white/10 bg-white/[0.025] px-4">
            <Search className="h-4 w-4 text-slate-600" />
            <input
              value={query}
              onChange={(event) => setQuery(event.target.value)}
              placeholder="Search title or decision ID"
              className="h-11 flex-1 bg-transparent text-sm outline-none placeholder:text-slate-700"
            />
          </div>
          <select
            value={domain}
            onChange={(event) => setDomain(event.target.value)}
            className="h-11 rounded-xl border border-white/10 bg-[#0d1217] px-4 text-sm text-slate-300 outline-none"
          >
            <option value="all">All domains</option>
            {domains.map((item) => <option key={item}>{item}</option>)}
          </select>
        </div>

        {loading ? (
          <div className="mt-8 grid gap-4 md:grid-cols-2 xl:grid-cols-3">
            {[0, 1, 2].map((item) => <div key={item} className="h-60 animate-pulse rounded-2xl bg-white/[0.04]" />)}
          </div>
        ) : filtered.length === 0 ? (
          <Card className="mt-8 border-dashed border-white/15 bg-white/[0.02]">
            <CardContent className="p-12 text-center">
              <AlertTriangle className="mx-auto h-8 w-8 text-slate-600" />
              <h2 className="mt-4 font-semibold">No decisions found</h2>
              <p className="mt-2 text-sm text-slate-500">Create a decision or change the current filters.</p>
            </CardContent>
          </Card>
        ) : (
          <section className="mt-8 grid gap-4 md:grid-cols-2 xl:grid-cols-3">
            {filtered.map((item) => (
              <Card key={item.decisionId} className="group overflow-hidden border-white/10 bg-slate-950/70 transition hover:-translate-y-1 hover:border-green-400/25">
                <div className="h-px bg-gradient-to-r from-transparent via-green-400/70 to-transparent opacity-0 transition group-hover:opacity-100" />
                <CardContent className="p-5">
                  <div className="flex items-start justify-between gap-4">
                    <div>
                      <Badge variant="outline" className="border-white/10 text-slate-400">{item.domain}</Badge>
                      <h2 className="mt-4 line-clamp-2 min-h-12 text-lg font-semibold leading-6">{item.title}</h2>
                    </div>
                    <button
                      aria-label={`Delete ${item.title}`}
                      onClick={() => removeDecision(item.decisionId)}
                      className="rounded-lg p-2 text-slate-700 transition hover:bg-red-400/10 hover:text-red-300"
                    >
                      <Trash2 className="h-4 w-4" />
                    </button>
                  </div>

                  <div className="mt-6 grid grid-cols-3 gap-2 border-y border-white/10 py-4 text-center">
                    <div>
                      <div className={`text-2xl font-semibold ${scoreTone(item.feasibilityScore)}`}>
                        {Math.round(item.feasibilityScore)}
                      </div>
                      <div className="text-[9px] uppercase tracking-wider text-slate-600">Score</div>
                    </div>
                    <div className="border-x border-white/10">
                      <div className="text-2xl font-semibold">{item.riskCount}</div>
                      <div className="text-[9px] uppercase tracking-wider text-slate-600">Risks</div>
                    </div>
                    <div>
                      <div className="text-2xl font-semibold">{item.missingFieldCount}</div>
                      <div className="text-[9px] uppercase tracking-wider text-slate-600">Missing</div>
                    </div>
                  </div>

                  <div className="mt-4 flex items-center justify-between">
                    <Badge className={riskTone(item.riskLevel)}>{item.riskLevel} risk</Badge>
                    <span className="text-xs text-slate-600">v{item.version}</span>
                  </div>
                  <Link
                    href={`/decisions/${item.decisionId}/analytics`}
                    className="mt-5 flex items-center justify-between rounded-lg border border-white/10 px-3 py-2 text-xs font-semibold text-slate-300 transition hover:border-green-400/25 hover:bg-green-400/[0.05]"
                  >
                    Open decision report
                    <ArrowUpRight className="h-4 w-4" />
                  </Link>
                </CardContent>
              </Card>
            ))}
          </section>
        )}
      </div>
      <ConfirmComponent />
    </main>
  )
}
