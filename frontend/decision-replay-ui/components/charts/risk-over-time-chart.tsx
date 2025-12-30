'use client';

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Legend } from 'recharts';
import type { RiskDataPoint } from '@/lib/types';

interface RiskOverTimeChartProps {
  data: RiskDataPoint[];
}

export function RiskOverTimeChart({ data }: RiskOverTimeChartProps) {
  return (
    <Card>
      <CardHeader>
        <CardTitle>Risk Evolution Over Time</CardTitle>
        <CardDescription>How risk assessment changed across events</CardDescription>
      </CardHeader>
      <CardContent>
        <ResponsiveContainer width="100%" height={300}>
          <LineChart
            data={data}
            margin={{ top: 5, right: 30, left: 20, bottom: 5 }}
          >
            <CartesianGrid strokeDasharray="3 3" />
            <XAxis
              dataKey="timestamp"
              tickFormatter={(value) => new Date(value).toLocaleTimeString()}
            />
            <YAxis
              domain={[0, 100]}
              tickFormatter={(value) => `${value}%`}
            />
            <Tooltip
              formatter={(value: number | undefined) => value ? `${value.toFixed(1)}%` : '0%'}
              labelFormatter={(value) => new Date(value).toLocaleString()}
            />
            <Legend />
            <Line
              type="monotone"
              dataKey="riskScore"
              stroke="#ef4444"
              strokeWidth={2}
              dot={{ fill: '#ef4444', r: 4 }}
              activeDot={{ r: 6 }}
              name="Risk Score"
            />
          </LineChart>
        </ResponsiveContainer>
      </CardContent>
    </Card>
  );
}
