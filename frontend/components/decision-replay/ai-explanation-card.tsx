"use client"

import { useState } from "react"
import { Card } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Sparkles, ChevronDown, Loader2 } from "lucide-react"

interface AIExplanationCardProps {
  title: string
  decisionId: string
  endpoint: string
  className?: string
}

export function AIExplanationCard({ title, decisionId, endpoint, className = "" }: AIExplanationCardProps) {
  const [isExpanded, setIsExpanded] = useState(false)
  const [explanation, setExplanation] = useState("")
  const [loading, setLoading] = useState(false)

  const handleFetch = async () => {
    if (explanation) {
      setIsExpanded(!isExpanded)
      return
    }

    setLoading(true)
    try {
      const res = await fetch(`/api/decisions/${decisionId}/${endpoint}`, {
        method: "POST",
      })
      const data = await res.json()
      setExplanation(data.explanation || data.analysis || "No explanation available")
      setIsExpanded(true)
    } catch (error) {
      setExplanation("Failed to fetch explanation")
    } finally {
      setLoading(false)
    }
  }

  return (
    <Card
      className={`p-6 border-green-100 dark:border-slate-700 bg-white/50 dark:bg-slate-900/50 backdrop-blur ${className}`}
    >
      <Button
        variant="ghost"
        className="w-full justify-between mb-4 p-0 h-auto hover:bg-transparent"
        onClick={handleFetch}
      >
        <div className="flex items-center gap-2">
          <Sparkles className="w-5 h-5 text-green-600 dark:text-green-400" />
          <h3 className="text-lg font-semibold">{title}</h3>
        </div>
        <ChevronDown className={`w-5 h-5 transition-transform duration-300 ${isExpanded ? "rotate-180" : ""}`} />
      </Button>

      {isExpanded && (
        <div className="space-y-3 animate-slide-down">
          {loading ? (
            <div className="flex items-center justify-center py-8">
              <Loader2 className="w-6 h-6 animate-spin text-green-600" />
            </div>
          ) : (
            <div className="text-sm text-foreground/70 space-y-2">
              {explanation.split("\n").map((line, i) => (
                <p key={i}>{line}</p>
              ))}
            </div>
          )}
        </div>
      )}
    </Card>
  )
}
