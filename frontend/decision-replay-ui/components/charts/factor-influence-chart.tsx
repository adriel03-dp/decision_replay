'use client';

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Cell } from 'recharts';
import type { FactorInfluence } from '@/lib/types';

interface FactorInfluenceChartProps {
  data: FactorInfluence[];
}

const COLORS = ['#2563eb', '#3b82f6', '#60a5fa', '#93c5fd', '#dbeafe'];

export function FactorInfluenceChart({ data }: FactorInfluenceChartProps) {
  // Sort by weight descending
  const sortedData = [...data].sort((a, b) => b.weight - a.weight);

  return (
    <Card>
      <CardHeader>
        <CardTitle>Factor Influence</CardTitle>
        <CardDescription>What mattered most in this decision</CardDescription>
      </CardHeader>
      <CardContent>
        <ResponsiveContainer width="100%" height={300}>
          <BarChart
            data={sortedData}
            layout="vertical"
            margin={{ top: 5, right: 30, left: 100, bottom: 5 }}
          >
            <CartesianGrid strokeDasharray="3 3" />
            <XAxis
              type="number"
              domain={[0, 100]}
              tickFormatter={(value) => `${value}%`}
            />
            <YAxis type="category" dataKey="factor" />
            <Tooltip
              formatter={(value: number | undefined) => value ? `${value.toFixed(1)}%` : '0%'}
              labelStyle={{ color: '#000' }}
            />
            <Bar dataKey="weight" radius={[0, 8, 8, 0]}>
              {sortedData.map((entry, index) => (
                <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
              ))}
            </Bar>
          </BarChart>
        </ResponsiveContainer>
      </CardContent>
    </Card>
  );
}
