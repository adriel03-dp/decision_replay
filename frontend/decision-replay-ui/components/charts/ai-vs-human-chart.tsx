'use client';

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from 'recharts';
import type { AIVsHumanComparison } from '@/lib/types';

interface AIVsHumanChartProps {
  data: AIVsHumanComparison[];
}

export function AIVsHumanChart({ data }: AIVsHumanChartProps) {
  return (
    <Card>
      <CardHeader>
        <CardTitle>AI vs Human Decision</CardTitle>
        <CardDescription>Comparison of AI recommendation and final decision</CardDescription>
      </CardHeader>
      <CardContent>
        <ResponsiveContainer width="100%" height={300}>
          <BarChart
            data={data}
            margin={{ top: 5, right: 30, left: 20, bottom: 5 }}
          >
            <CartesianGrid strokeDasharray="3 3" />
            <XAxis dataKey="category" />
            <YAxis />
            <Tooltip />
            <Legend />
            <Bar dataKey="aiRecommendation" fill="#3b82f6" name="AI Recommendation" />
            <Bar dataKey="humanDecision" fill="#10b981" name="Human Decision" />
          </BarChart>
        </ResponsiveContainer>
      </CardContent>
    </Card>
  );
}
