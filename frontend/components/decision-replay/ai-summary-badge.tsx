"use client"

import { useState } from "react"
import { Card } from "@/components/ui/card"
import { Sparkles, Loader2 } from "lucide-react"

interface AISummaryBadgeProps {
  decisionId: string
  title?: string
}

export function AISummaryBadge({ decisionId, title = "AI Summary" }: AISummaryBadgeProps) {
  const [summary, setSummary] = useState<string>("")
  const [loading, setLoading] = useState(false)
  const [shown, setShown] = useState(false)

  const handleShow = async () => {
    if (summary || shown) {
      setShown(!shown)
      return
    }

    setLoading(true)
    setShown(true)
    try {
      const res = await fetch(`/api/decisions/${decisionId}/explain`, {
        method: "POST",
      })
      const data = await res.json()
      const text = data.explanation || ""
      // Get first 150 characters as summary
      setSummary(text.substring(0, 150) + (text.length > 150 ? "..." : ""))
    } catch (error) {
      setSummary("Unable to fetch AI summary")
    } finally {
      setLoading(false)
    }
  }

  return (
    <Card
      className="p-4 border-green-100 dark:border-slate-700 bg-gradient-to-r from-green-50/50 to-emerald-50/50 dark:from-green-900/20 dark:to-emerald-900/20 backdrop-blur cursor-pointer hover:shadow-md smooth-transition"
      onClick={handleShow}
    >
      <div className="flex items-start gap-3">
        <Sparkles className="w-5 h-5 text-green-600 dark:text-green-400 flex-shrink-0 mt-0.5" />
        <div className="flex-1">
          <h4 className="font-semibold text-sm mb-1">{title}</h4>
          {loading ? (
            <div className="flex items-center gap-2">
              <Loader2 className="w-4 h-4 animate-spin text-green-600" />
              <span className="text-xs text-muted-foreground">Generating...</span>
            </div>
          ) : (
            <p className="text-xs text-foreground/60">{summary}</p>
          )}
        </div>
      </div>
    </Card>
  )
}
