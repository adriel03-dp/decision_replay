"use client"

import Link from "next/link"
import { useState, useEffect } from "react"
import { Search, Filter, Plus } from "lucide-react"
import { Button } from "@/components/ui/button"
import { OutcomeBadge } from "@/components/decision-replay/outcome-badge"
import { decisionApi } from "@/lib/api"
import { useDecisionStore } from "@/lib/store"
import type { DecisionExtended } from "@/lib/api-types"

export default function DecisionsPage() {
  const { setSearchId, searchId } = useDecisionStore()
  const [decisions, setDecisions] = useState<DecisionExtended[]>([])
  const [loading, setLoading] = useState(true)
  const [localStatusFilter, setLocalStatusFilter] = useState<string[]>([])
  const [localRiskRange, setLocalRiskRange] = useState<[number, number]>([0, 100])
  const [localTypeFilter, setLocalTypeFilter] = useState("")

  // Fetch decisions from API
  useEffect(() => {
    async function loadDecisions() {
      setLoading(true)
      try {
        const data = await decisionApi.getAllDecisions()
        setDecisions(data)
      } catch (error) {
        console.error('Failed to load decisions:', error)
      } finally {
        setLoading(false)
      }
    }
    loadDecisions()
  }, [])

  const filteredDecisions = decisions.filter((decision) => {
    const matchesSearch = decision.id.toLowerCase().includes(searchId.toLowerCase())
    const matchesStatus = localStatusFilter.length === 0 || localStatusFilter.includes(decision.status)
    const matchesRisk = 
      (decision.riskScore ?? 0) >= localRiskRange[0] && 
      (decision.riskScore ?? 0) <= localRiskRange[1]
    const matchesType = !localTypeFilter || decision.type === localTypeFilter

    return matchesSearch && matchesStatus && matchesRisk && matchesType
  })

  const decisionTypes = Array.from(new Set(decisions.map((d) => d.type)))
  const statuses = ["DRAFT", "IN_REVIEW", "FINALIZED"]

  const toggleStatusFilter = (status: string) => {
    const updated = localStatusFilter.includes(status)
      ? localStatusFilter.filter((s) => s !== status)
      : [...localStatusFilter, status]
    setLocalStatusFilter(updated)
  }

  return (
    <main className="min-h-screen bg-background">
      <div className="border-b border-border">
        <div className="max-w-7xl mx-auto px-6 py-8">
          <div className="flex items-center justify-between mb-2">
            <div>
              <h1 className="text-foreground">Decision Replay</h1>
              <p className="text-muted-foreground mt-2">Review and audit all decisions with complete replay history</p>
            </div>
            <Link href="/decisions/new">
              <Button className="gap-2">
                <Plus className="w-4 h-4" />
                New Decision
              </Button>
            </Link>
          </div>
        </div>
      </div>

      <div className="max-w-7xl mx-auto px-6 py-8 space-y-8">
        <div className="space-y-4">
          <div className="flex items-center gap-2">
            <Filter className="w-4 h-4 text-muted-foreground" />
            <span className="text-sm font-semibold text-foreground uppercase tracking-wide">Filters</span>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-4 gap-3">
            {/* Search */}
            <div className="flex items-center gap-2 px-3 py-2.5 bg-card rounded-lg border border-border hover:border-accent/50 transition-colors">
              <Search className="w-4 h-4 text-muted-foreground" />
              <input
                type="text"
                placeholder="Search by ID..."
                value={searchId}
                onChange={(e) => setSearchId(e.target.value)}
                className="bg-transparent border-0 outline-none text-sm flex-1 text-foreground placeholder:text-muted-foreground"
              />
            </div>

            {/* Status Filter */}
            <div className="space-y-2">
              <label className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">Status</label>
              <div className="flex gap-2 flex-wrap">
                {statuses.map((status) => (
                  <button
                    key={status}
                    onClick={() => toggleStatusFilter(status)}
                    className={`px-3 py-1.5 text-xs font-medium rounded-md transition-all duration-200 ${
                      localStatusFilter.includes(status)
                        ? "bg-primary text-primary-foreground shadow-lg shadow-primary/20"
                        : "bg-card text-muted-foreground border border-border hover:border-accent/50 hover:text-foreground"
                    }`}
                  >
                    {status}
                  </button>
                ))}
              </div>
            </div>

            {/* Type Filter */}
            <div className="space-y-2">
              <label className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">Type</label>
              <select
                value={localTypeFilter}
                onChange={(e) => setLocalTypeFilter(e.target.value)}
                className="w-full px-3 py-2 text-sm bg-card border border-border rounded-lg text-foreground placeholder:text-muted-foreground hover:border-accent/50 transition-colors focus:border-accent focus:outline-none"
              >
                <option value="">All Types</option>
                {decisionTypes.map((type) => (
                  <option key={type} value={type}>
                    {type}
                  </option>
                ))}
              </select>
            </div>

            {/* Risk Range */}
            <div className="space-y-2">
              <label className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">
                Risk: {localRiskRange[0]} - {localRiskRange[1]}
              </label>
              <div className="flex items-center gap-2">
                <input
                  type="range"
                  min="0"
                  max="100"
                  value={localRiskRange[0]}
                  onChange={(e) => setLocalRiskRange([Number(e.target.value), localRiskRange[1]])}
                  className="flex-1 h-1.5 bg-card rounded-full appearance-none cursor-pointer accent-primary"
                />
                <input
                  type="range"
                  min="0"
                  max="100"
                  value={localRiskRange[1]}
                  onChange={(e) => setLocalRiskRange([localRiskRange[0], Number(e.target.value)])}
                  className="flex-1 h-1.5 bg-card rounded-full appearance-none cursor-pointer accent-primary"
                />
              </div>
            </div>
          </div>
        </div>

        <div className="space-y-4">
          <p className="text-sm text-muted-foreground font-medium">
            {loading ? 'Loading...' : `${filteredDecisions.length} decision${filteredDecisions.length !== 1 ? "s" : ""} found`}
          </p>

          {loading ? (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
              {[1, 2, 3].map((i) => (
                <div key={i} className="bg-card border border-border rounded-lg p-5 h-64 animate-pulse">
                  <div className="h-4 bg-muted rounded w-20 mb-4" />
                  <div className="h-3 bg-muted rounded w-32 mb-4" />
                  <div className="h-3 bg-muted rounded w-full mb-2" />
                  <div className="h-3 bg-muted rounded w-3/4" />
                </div>
              ))}
            </div>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
              {filteredDecisions.map((decision) => (
                <Link key={decision.id} href={`/decisions/${decision.id}`}>
                  <div className="group relative bg-card border border-border rounded-lg p-5 hover:border-accent/50 hover:shadow-xl hover:shadow-primary/10 transition-all duration-300 cursor-pointer h-full">
                    <div className="space-y-4">
                      {/* Header */}
                      <div className="flex items-start justify-between">
                        <div>
                          <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">ID</p>
                          <p className="text-sm font-mono text-foreground mt-1 group-hover:text-accent transition-colors">
                            {decision.id}
                          </p>
                        </div>
                        <OutcomeBadge outcome={decision.outcome || decision.currentOutcome} size="sm" />
                      </div>

                      {/* Type */}
                      <div>
                        <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">Type</p>
                        <p className="text-sm text-foreground font-medium mt-1">{decision.type}</p>
                      </div>

                      {/* Risk Score */}
                      <div>
                        <div className="flex items-center justify-between mb-2">
                          <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">Risk</p>
                          <p className="text-sm font-bold text-foreground">{decision.riskScore ?? 0}%</p>
                        </div>
                        <div className="h-1.5 bg-secondary rounded-full overflow-hidden">
                          <div
                            className={`h-full transition-all ${
                              (decision.riskScore ?? 0) >= 70
                                ? "bg-red-500"
                                : (decision.riskScore ?? 0) >= 40
                                  ? "bg-amber-500"
                                  : "bg-emerald-500"
                            }`}
                            style={{ width: `${decision.riskScore ?? 0}%` }}
                          />
                        </div>
                      </div>

                      {/* Status and Timestamp */}
                      <div className="pt-3 border-t border-border space-y-3">
                        <div className="flex items-center justify-between">
                          <span
                            className={`inline-block px-2.5 py-1 text-xs font-semibold rounded-md ${
                              decision.status === "FINALIZED"
                                ? "bg-emerald-500/10 text-emerald-400"
                                : decision.status === "IN_REVIEW"
                                  ? "bg-blue-500/10 text-blue-400"
                                  : "bg-muted text-muted-foreground"
                            }`}
                          >
                            {decision.status}
                          </span>
                        </div>
                        <p className="text-xs text-muted-foreground">{new Date(decision.createdAt).toLocaleString()}</p>
                      </div>
                    </div>
                  </div>
                </Link>
              ))}
            </div>
          )}

          {!loading && filteredDecisions.length === 0 && (
            <div className="text-center py-16">
              <p className="text-muted-foreground">No decisions match your filters</p>
            </div>
          )}
        </div>
      </div>
    </main>
  )
}
