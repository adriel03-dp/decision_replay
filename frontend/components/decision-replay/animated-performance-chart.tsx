"use client"

import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer, Cell } from "recharts"
import { Card } from "@/components/ui/card"

interface ResourceAllocationData {
  resource: string              // e.g., "Team Size", "Budget", "Timeline", "Tools"
  allocated: number             // What was allocated
  required: number              // What AI analysis suggests is needed
  gap: number                   // Difference (negative = shortage)
}

export function AnimatedPerformanceChart({ data }: { data: ResourceAllocationData[] }) {
  // Color bars based on whether there's a resource gap
  const getBarColor = (gap: number) => {
    if (gap >= 0) return "#10b981" // green - sufficient
    if (gap > -20) return "#f59e0b" // amber - tight but possible
    return "#ef4444" // red - significant shortage
  }

  return (
    <Card className="p-6 border-green-100 dark:border-slate-700">
      <h3 className="text-lg font-semibold mb-4">Resource Allocation Analysis</h3>
      <p className="text-sm text-muted-foreground mb-4">
        Compare allocated resources vs. AI-recommended requirements
      </p>
      <ResponsiveContainer width="100%" height={300}>
        <BarChart data={data} margin={{ top: 20, right: 30, left: 0, bottom: 0 }}>
          <CartesianGrid strokeDasharray="3 3" stroke="var(--color-border)" opacity={0.3} />
          <XAxis 
            dataKey="resource" 
            stroke="var(--color-muted-foreground)" 
            fontSize={12}
          />
          <YAxis 
            stroke="var(--color-muted-foreground)" 
            fontSize={12}
            label={{ value: "Units", angle: -90, position: "insideLeft" }}
          />
          <Tooltip
            contentStyle={{
              backgroundColor: "var(--color-card)",
              border: `1px solid var(--color-border)`,
              borderRadius: "8px",
              padding: "12px",
            }}
            formatter={(value: number, name: string) => {
              if (name === "gap") {
                return [value >= 0 ? `+${value}` : value, "Gap"]
              }
              return [value, name === "allocated" ? "Allocated" : "Required"]
            }}
          />
          <Legend 
            wrapperStyle={{ fontSize: "12px", paddingTop: "10px" }}
          />
          <Bar
            dataKey="allocated"
            fill="#3b82f6"
            radius={[8, 8, 0, 0]}
            animationDuration={1500}
            animationBegin={0}
            name="Allocated"
          />
          <Bar
            dataKey="required"
            fill="#8b5cf6"
            radius={[8, 8, 0, 0]}
            animationDuration={1500}
            animationBegin={200}
            name="AI Recommended"
          />
          <Bar
            dataKey="gap"
            radius={[8, 8, 0, 0]}
            animationDuration={1500}
            animationBegin={400}
            name="Gap"
          >
            {data.map((entry, index) => (
              <Cell key={`cell-${index}`} fill={getBarColor(entry.gap)} />
            ))}
          </Bar>
        </BarChart>
      </ResponsiveContainer>
      <div className="mt-4 flex gap-6 text-xs text-muted-foreground">
        <div className="flex items-center gap-2">
          <div className="w-3 h-3 rounded-full bg-green-500" />
          <span>Sufficient</span>
        </div>
        <div className="flex items-center gap-2">
          <div className="w-3 h-3 rounded-full bg-amber-500" />
          <span>Tight</span>
        </div>
        <div className="flex items-center gap-2">
          <div className="w-3 h-3 rounded-full bg-red-500" />
          <span>Shortage</span>
        </div>
      </div>
    </Card>
  )
}
