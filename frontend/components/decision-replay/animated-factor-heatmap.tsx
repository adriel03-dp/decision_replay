"use client"

import { Card } from "@/components/ui/card"
import { TrendingUp, TrendingDown, Minus, AlertTriangle } from "lucide-react"

interface PlanningFactorData {
  factor: string                // e.g., "Scope Complexity", "Timeline Pressure", "Resource Gaps"
  impact: number                // 0-100 (100 = highest impact/concern)
  status: "good" | "warning" | "critical"  // Health status
  trend: "up" | "down" | "stable"
  description?: string          // Optional explanation
}

export function AnimatedFactorHeatmap({ data }: { data: PlanningFactorData[] }) {
  const maxImpact = Math.max(...data.map((d) => d.impact))

  const getHeatmapColor = (impact: number, status: string) => {
    // Green = low concern, Yellow = moderate, Red = high concern
    if (status === "critical") return "bg-gradient-to-r from-red-500 to-orange-500"
    if (status === "warning") return "bg-gradient-to-r from-amber-400 to-yellow-400"
    return "bg-gradient-to-r from-green-400 to-emerald-500"
  }

  const getTrendIcon = (trend: string) => {
    if (trend === "up") return <TrendingUp className="w-3 h-3" />
    if (trend === "down") return <TrendingDown className="w-3 h-3" />
    return <Minus className="w-3 h-3" />
  }

  const getStatusBadge = (status: string) => {
    if (status === "critical") return (
      <div className="flex items-center gap-1 px-2 py-0.5 rounded-full bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-400">
        <AlertTriangle className="w-3 h-3" />
        <span className="text-xs font-medium">Critical</span>
      </div>
    )
    if (status === "warning") return (
      <div className="flex items-center gap-1 px-2 py-0.5 rounded-full bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-400">
        <span className="text-xs font-medium">Needs Attention</span>
      </div>
    )
    return (
      <div className="flex items-center gap-1 px-2 py-0.5 rounded-full bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400">
        <span className="text-xs font-medium">On Track</span>
      </div>
    )
  }

  return (
    <Card className="p-6 border-green-100 dark:border-slate-700">
      <h3 className="text-lg font-semibold mb-2">Planning Risk Factors</h3>
      <p className="text-sm text-muted-foreground mb-6">
        AI-identified factors that could impact plan success
      </p>
      <div className="space-y-5">
        {data.map((item, index) => (
          <div key={index} className="space-y-2 animate-fade-in" style={{ animationDelay: `${index * 100}ms` }}>
            <div className="flex items-center justify-between text-sm">
              <div className="flex items-center gap-2">
                <span className="font-medium">{item.factor}</span>
                {getStatusBadge(item.status)}
              </div>
              <div className="flex items-center gap-3 text-muted-foreground">
                <span className="text-xs">{item.impact}% impact</span>
                <div className="flex items-center gap-1">
                  {getTrendIcon(item.trend)}
                </div>
              </div>
            </div>
            {item.description && (
              <p className="text-xs text-muted-foreground pl-1">{item.description}</p>
            )}
            <div className="w-full h-3 bg-muted rounded-full overflow-hidden">
              <div
                className={`h-full ${getHeatmapColor(item.impact, item.status)} transition-all duration-1000 ease-out`}
                style={{
                  width: `${item.impact}%`,
                  animation: "slideRight 1.2s ease-out",
                }}
              />
            </div>
          </div>
        ))}
      </div>

      <style jsx>{`
        @keyframes slideRight {
          from {
            width: 0;
          }
          to {
            width: inherit;
          }
        }
        
        @keyframes fade-in {
          from {
            opacity: 0;
            transform: translateY(10px);
          }
          to {
            opacity: 1;
            transform: translateY(0);
          }
        }
        
        .animate-fade-in {
          animation: fade-in 0.5s ease-out forwards;
        }
      `}</style>
    </Card>
  )
}
