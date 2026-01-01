"use client"

import { AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Legend } from "recharts"
import { Card } from "@/components/ui/card"

interface FeasibilityTimelineData {
  time: string
  feasibilityScore: number      // Overall feasibility 0-100
  timelinePressure: number       // How aggressive the timeline is
  resourceAdequacy: number       // Resource availability score
  scopeComplexity: number        // Complexity level
}

export function AnimatedTimelineChart({ data }: { data: FeasibilityTimelineData[] }) {
  return (
    <Card className="p-6 border-green-100 dark:border-slate-700">
      <h3 className="text-lg font-semibold mb-4">Feasibility Analysis Timeline</h3>
      <p className="text-sm text-muted-foreground mb-4">
        Track how feasibility factors evolved as the plan was refined
      </p>
      <ResponsiveContainer width="100%" height={300}>
        <AreaChart data={data} margin={{ top: 10, right: 30, left: 0, bottom: 0 }}>
          <defs>
            <linearGradient id="colorFeasibility" x1="0" y1="0" x2="0" y2="1">
              <stop offset="5%" stopColor="#10b981" stopOpacity={0.8} />
              <stop offset="95%" stopColor="#10b981" stopOpacity={0.1} />
            </linearGradient>
            <linearGradient id="colorTimeline" x1="0" y1="0" x2="0" y2="1">
              <stop offset="5%" stopColor="#f59e0b" stopOpacity={0.8} />
              <stop offset="95%" stopColor="#f59e0b" stopOpacity={0.1} />
            </linearGradient>
            <linearGradient id="colorResources" x1="0" y1="0" x2="0" y2="1">
              <stop offset="5%" stopColor="#3b82f6" stopOpacity={0.8} />
              <stop offset="95%" stopColor="#3b82f6" stopOpacity={0.1} />
            </linearGradient>
          </defs>
          <CartesianGrid strokeDasharray="3 3" stroke="var(--color-border)" opacity={0.3} />
          <XAxis 
            dataKey="time" 
            stroke="var(--color-muted-foreground)" 
            fontSize={12}
          />
          <YAxis 
            stroke="var(--color-muted-foreground)" 
            fontSize={12}
            domain={[0, 100]}
          />
          <Tooltip
            contentStyle={{
              backgroundColor: "var(--color-card)",
              border: `1px solid var(--color-border)`,
              borderRadius: "8px",
              padding: "12px",
            }}
            formatter={(value: number) => [`${value}%`, ""]}
          />
          <Legend 
            wrapperStyle={{ fontSize: "12px", paddingTop: "10px" }}
            iconType="circle"
          />
          <Area
            type="monotone"
            dataKey="feasibilityScore"
            stroke="#10b981"
            strokeWidth={2}
            fillOpacity={1}
            fill="url(#colorFeasibility)"
            animationDuration={1500}
            animationBegin={0}
            name="Overall Feasibility"
          />
          <Area
            type="monotone"
            dataKey="resourceAdequacy"
            stroke="#3b82f6"
            strokeWidth={2}
            fillOpacity={1}
            fill="url(#colorResources)"
            animationDuration={1500}
            animationBegin={200}
            name="Resource Adequacy"
          />
          <Area
            type="monotone"
            dataKey="timelinePressure"
            stroke="#f59e0b"
            strokeWidth={2}
            fillOpacity={1}
            fill="url(#colorTimeline)"
            animationDuration={1500}
            animationBegin={400}
            name="Timeline Pressure"
          />
        </AreaChart>
      </ResponsiveContainer>
    </Card>
  )
}
