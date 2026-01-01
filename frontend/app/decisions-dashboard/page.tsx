"use client"

import { useState } from "react"
import Link from "next/link"
import { Button } from "@/components/ui/button"
import { Card } from "@/components/ui/card"
import { ArrowRight, TrendingUp, AlertCircle, CheckCircle2, Clock } from "lucide-react"
import { mockDecisions } from "@/lib/mock-data"

export default function DecisionsDashboard() {
  const [selectedFilter, setSelectedFilter] = useState<"all" | "approved" | "rejected" | "pending">("all")

  const filteredDecisions = mockDecisions.filter((d) => {
    if (selectedFilter === "all") return true
    return d.outcome.toLowerCase() === selectedFilter
  })

  const stats = {
    total: mockDecisions.length,
    approved: mockDecisions.filter((d) => d.outcome === "Approved").length,
    rejected: mockDecisions.filter((d) => d.outcome === "Rejected").length,
    pending: mockDecisions.filter((d) => d.outcome === "Pending").length,
    avgConfidence: Math.round(mockDecisions.reduce((sum, d) => sum + d.confidence, 0) / mockDecisions.length),
  }

  const getOutcomeIcon = (outcome: string) => {
    switch (outcome) {
      case "Approved":
        return <CheckCircle2 className="w-5 h-5 text-green-600 dark:text-green-400" />
      case "Rejected":
        return <AlertCircle className="w-5 h-5 text-red-600 dark:text-red-400" />
      case "Pending":
        return <Clock className="w-5 h-5 text-yellow-600 dark:text-yellow-400" />
      default:
        return null
    }
  }

  return (
    <main className="min-h-screen bg-gradient-to-br from-white to-green-50 dark:from-slate-950 dark:to-slate-900">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
        {/* Header */}
        <div className="mb-12">
          <h1 className="text-4xl font-bold mb-2">Decisions Dashboard</h1>
          <p className="text-foreground/60">Overview of all decisions with key metrics and quick access</p>
        </div>

        {/* Stats Grid */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-5 gap-4 mb-12">
          <Card className="p-6 border-green-100 dark:border-slate-700 bg-white/50 dark:bg-slate-900/50 backdrop-blur">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-muted-foreground mb-1">Total Decisions</p>
                <p className="text-3xl font-bold">{stats.total}</p>
              </div>
              <TrendingUp className="w-8 h-8 text-green-600/30" />
            </div>
          </Card>

          <Card className="p-6 border-green-100 dark:border-slate-700 bg-white/50 dark:bg-slate-900/50 backdrop-blur">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-muted-foreground mb-1">Approved</p>
                <p className="text-3xl font-bold text-green-600 dark:text-green-400">{stats.approved}</p>
              </div>
              <CheckCircle2 className="w-8 h-8 text-green-600/30" />
            </div>
          </Card>

          <Card className="p-6 border-green-100 dark:border-slate-700 bg-white/50 dark:bg-slate-900/50 backdrop-blur">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-muted-foreground mb-1">Rejected</p>
                <p className="text-3xl font-bold text-red-600 dark:text-red-400">{stats.rejected}</p>
              </div>
              <AlertCircle className="w-8 h-8 text-red-600/30" />
            </div>
          </Card>

          <Card className="p-6 border-green-100 dark:border-slate-700 bg-white/50 dark:bg-slate-900/50 backdrop-blur">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-muted-foreground mb-1">Pending</p>
                <p className="text-3xl font-bold text-yellow-600 dark:text-yellow-400">{stats.pending}</p>
              </div>
              <Clock className="w-8 h-8 text-yellow-600/30" />
            </div>
          </Card>

          <Card className="p-6 border-green-100 dark:border-slate-700 bg-white/50 dark:bg-slate-900/50 backdrop-blur">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-muted-foreground mb-1">Avg Confidence</p>
                <p className="text-3xl font-bold">{stats.avgConfidence}%</p>
              </div>
              <TrendingUp className="w-8 h-8 text-blue-600/30" />
            </div>
          </Card>
        </div>

        {/* Filter Buttons */}
        <div className="flex flex-wrap gap-2 mb-8">
          {(["all", "approved", "rejected", "pending"] as const).map((filter) => (
            <Button
              key={filter}
              variant={selectedFilter === filter ? "default" : "outline"}
              onClick={() => setSelectedFilter(filter)}
              className="capitalize smooth-transition"
            >
              {filter}
            </Button>
          ))}
        </div>

        {/* Decisions Grid */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {filteredDecisions.map((decision, index) => (
            <Link key={decision.id} href={`/decisions-analytics/${decision.id}`}>
              <Card
                className="p-6 border-green-100 dark:border-slate-700 bg-white/50 dark:bg-slate-900/50 backdrop-blur hover:shadow-lg hover:shadow-green-500/10 smooth-transition cursor-pointer h-full animate-fade-in group"
                style={{ animationDelay: `${index * 50}ms` }}
              >
                <div className="flex items-start justify-between mb-4">
                  <div className="flex-1">
                    <h3 className="font-semibold text-lg mb-1 group-hover:text-green-600 dark:group-hover:text-green-400 smooth-transition">
                      {decision.title}
                    </h3>
                    <p className="text-sm text-muted-foreground">{decision.description}</p>
                  </div>
                  {getOutcomeIcon(decision.outcome)}
                </div>

                <div className="space-y-3 mb-4">
                  <div className="flex items-center justify-between text-sm">
                    <span className="text-muted-foreground">Outcome</span>
                    <span className="font-medium">{decision.outcome}</span>
                  </div>
                  <div className="flex items-center justify-between text-sm">
                    <span className="text-muted-foreground">Confidence</span>
                    <span className="font-medium">{decision.confidence}%</span>
                  </div>
                  <div className="flex items-center justify-between text-sm">
                    <span className="text-muted-foreground">Risk</span>
                    <span
                      className={`font-medium ${decision.riskLevel === "High" ? "text-red-600 dark:text-red-400" : decision.riskLevel === "Medium" ? "text-yellow-600 dark:text-yellow-400" : "text-green-600 dark:text-green-400"}`}
                    >
                      {decision.riskLevel}
                    </span>
                  </div>
                </div>

                <Button
                  variant="ghost"
                  className="w-full justify-between group-hover:bg-green-500/10 smooth-transition"
                  asChild
                >
                  <div>
                    View Analytics
                    <ArrowRight className="w-4 h-4 group-hover:translate-x-1 smooth-transition" />
                  </div>
                </Button>
              </Card>
            </Link>
          ))}
        </div>
      </div>
    </main>
  )
}
