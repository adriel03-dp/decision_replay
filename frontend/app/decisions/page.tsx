"use client"

import Link from "next/link"
import { useState, useEffect } from "react"
import { Search, Filter, Plus, Trash2, Eye, BarChart3, AlertTriangle } from "lucide-react"
import { Button } from "@/components/ui/button"
import { OutcomeBadge } from "@/components/decision-replay/outcome-badge"
import { decisionApi } from "@/lib/api"
import { useDecisionStore } from "@/lib/store"
import { useToast } from "@/hooks/use-toast"
import { useCustomConfirm } from "@/components/ui/custom-dialogs"
import type { DecisionExtended, DecisionStatus } from "@/lib/api-types"

export default function DecisionsPage() {
  const { setSearchId, searchId } = useDecisionStore()
  const { toast } = useToast()
  const { showConfirm, ConfirmComponent } = useCustomConfirm()
  const [decisions, setDecisions] = useState<DecisionExtended[]>([])
  const [loading, setLoading] = useState(true)
  const [deleting, setDeleting] = useState<string | null>(null)
  const [localStatusFilter, setLocalStatusFilter] = useState<string[]>([])
  const [localDomainFilter, setLocalDomainFilter] = useState("")

  // Fetch decisions from API
  useEffect(() => {
    let mounted = true
    
    async function loadDecisions() {
      if (!mounted) return
      
      setLoading(true)
      try {
        const data = await decisionApi.getAllDecisions()
        if (mounted) {
          setDecisions(data)
        }
      } catch (error) {
        console.error('Failed to load decisions:', error)
        if (mounted) {
          toast({
            title: "Decision Replay Alert",
            description: "Failed to load decisions. Please refresh the page.",
            variant: "destructive"
          })
        }
      } finally {
        if (mounted) {
          setLoading(false)
        }
      }
    }
    
    loadDecisions()
    
    return () => {
      mounted = false
    }
  }, [])

  const filteredDecisions = decisions.filter((decision) => {
    const matchesSearch = 
      decision.id.toLowerCase().includes(searchId.toLowerCase()) ||
      decision.naturalLanguageInput?.toLowerCase().includes(searchId.toLowerCase())
    const matchesStatus = localStatusFilter.length === 0 || localStatusFilter.includes(decision.status)
    const matchesDomain = !localDomainFilter || decision.domainType === localDomainFilter

    return matchesSearch && matchesStatus && matchesDomain
  })

  const domainTypes = Array.from(new Set(decisions.map((d) => d.domainType).filter(Boolean)))
  const statuses: DecisionStatus[] = ["Draft", "InReview", "Finalized"]

  const toggleStatusFilter = (status: string) => {
    const updated = localStatusFilter.includes(status)
      ? localStatusFilter.filter((s) => s !== status)
      : [...localStatusFilter, status]
    setLocalStatusFilter(updated)
  }

  // Get a preview of the natural language input
  const getPreview = (input: string, maxLength = 100) => {
    if (!input) return "No description"
    return input.length > maxLength ? input.substring(0, maxLength) + "..." : input
  }

  // Get feasibility score from analysis if available
  const getFeasibilityScore = (decision: DecisionExtended): number | null => {
    return decision.analysis?.feasibilityScore ?? null
  }

  // Delete decision function
  const handleDeleteDecision = async (decisionId: string, event: React.MouseEvent) => {
    event.preventDefault()
    event.stopPropagation()
    
    showConfirm(
      'Are you sure you want to delete this decision? This action cannot be undone.',
      async () => {
        setDeleting(decisionId)
        try {
          const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/v2/decisions/${decisionId}`, {
            method: 'DELETE',
            headers: {
              'Authorization': `Bearer ${localStorage.getItem('token')}`
            }
          })

          if (!response.ok) {
            throw new Error('Failed to delete decision')
          }

          // Remove from local state
          setDecisions(prev => prev.filter(d => d.id !== decisionId))
          
          toast({
            title: "Decision Replay - Action Completed",
            description: "The decision has been successfully deleted.",
            variant: "default"
          })
        } catch (error) {
          console.error('Failed to delete decision:', error)
          toast({
            title: "Decision Replay Alert",
            description: "Failed to delete the decision. Please try again.",
            variant: "destructive"
          })
        } finally {
          setDeleting(null)
        }
      },
      {
        title: "Delete Decision",
        confirmText: "Delete",
        cancelText: "Cancel",
        variant: "destructive"
      }
    )
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

          <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
            {/* Search */}
            <div className="flex items-center gap-2 px-3 py-2.5 bg-card rounded-lg border border-border hover:border-accent/50 transition-colors">
              <Search className="w-4 h-4 text-muted-foreground" />
              <input
                type="text"
                placeholder="Search by ID or description..."
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

            {/* Domain Type Filter */}
            <div className="space-y-2">
              <label className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">Domain</label>
              <select
                value={localDomainFilter}
                onChange={(e) => setLocalDomainFilter(e.target.value)}
                className="w-full px-3 py-2 text-sm bg-card border border-border rounded-lg text-foreground placeholder:text-muted-foreground hover:border-accent/50 transition-colors focus:border-accent focus:outline-none"
              >
                <option value="">All Domains</option>
                {domainTypes.map((domain) => (
                  <option key={domain} value={domain}>
                    {domain}
                  </option>
                ))}
              </select>
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
              {filteredDecisions.map((decision) => {
                const feasibilityScore = getFeasibilityScore(decision)
                const isDeleting = deleting === decision.id
                return (
                  <div key={decision.id} className="group relative bg-card border border-border rounded-lg p-5 hover:border-accent/50 hover:shadow-xl hover:shadow-primary/10 transition-all duration-300 h-full">
                    {/* Delete Button */}
                    <button
                      onClick={(e) => handleDeleteDecision(decision.id, e)}
                      disabled={isDeleting}
                      className="absolute top-3 right-3 p-2 text-muted-foreground hover:text-red-600 hover:bg-red-50 dark:hover:bg-red-900/20 rounded-md transition-all duration-200 opacity-0 group-hover:opacity-100 z-10"
                    >
                      {isDeleting ? (
                        <div className="w-4 h-4 border-2 border-red-600 border-t-transparent rounded-full animate-spin" />
                      ) : (
                        <Trash2 className="w-4 h-4" />
                      )}
                    </button>

                    <Link href={`/decisions/${decision.id}/analysis`} className="block h-full">
                      <div className="space-y-4 h-full">
                        {/* Header */}
                        <div className="flex items-start justify-between pr-8">
                          <div className="flex-1 min-w-0">
                            <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">Decision</p>
                            <p className="text-sm font-mono text-foreground mt-1 group-hover:text-accent transition-colors truncate">
                              {decision.id.slice(0, 8)}...
                            </p>
                          </div>
                          <OutcomeBadge outcome={decision.outcome} size="sm" />
                        </div>

                        {/* Description Preview */}
                        <div>
                          <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">Description</p>
                          <p className="text-sm text-foreground mt-1 line-clamp-2">
                            {getPreview(decision.naturalLanguageInput, 80)}
                          </p>
                        </div>

                        {/* Domain Type */}
                        {decision.domainType && (
                          <div>
                            <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">Domain</p>
                            <p className="text-sm text-foreground font-medium mt-1">{decision.domainType}</p>
                          </div>
                        )}

                        {/* Feasibility Score (if analyzed) */}
                        {feasibilityScore !== null && (
                          <div>
                            <div className="flex items-center justify-between mb-2">
                              <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">Feasibility</p>
                              <p className="text-sm font-bold text-foreground">{Math.round(feasibilityScore)}%</p>
                            </div>
                            <div className="h-1.5 bg-secondary rounded-full overflow-hidden">
                              <div
                                className={`h-full transition-all ${
                                  feasibilityScore >= 70
                                    ? "bg-emerald-500"
                                    : feasibilityScore >= 40
                                    ? "bg-amber-500"
                                    : "bg-red-500"
                                }`}
                                style={{ width: `${Math.max(0, Math.min(100, feasibilityScore))}%` }}
                              />
                            </div>
                          </div>
                        )}

                        {/* Actions */}
                        <div className="flex items-center justify-between pt-2 mt-auto">
                          <div className="flex items-center gap-3">
                            <div className="flex items-center gap-1">
                              <Eye className="w-4 h-4 text-muted-foreground" />
                              <span className="text-xs text-muted-foreground">View</span>
                            </div>
                            {feasibilityScore !== null && (
                              <div className="flex items-center gap-1">
                                <BarChart3 className="w-4 h-4 text-emerald-600" />
                                <span className="text-xs text-emerald-600">Analyzed</span>
                              </div>
                            )}
                          </div>
                          <time className="text-xs text-muted-foreground">
                            {new Date(decision.createdAt).toLocaleDateString()}
                          </time>
                        </div>
                      </div>
                    </Link>
                  </div>
                )
              })}
            </div>
          )}

          {!loading && filteredDecisions.length === 0 && (
            <div className="text-center py-16">
              <p className="text-muted-foreground">No decisions match your filters</p>
            </div>
          )}
        </div>
      </div>
      <ConfirmComponent />
    </main>
  )
}
