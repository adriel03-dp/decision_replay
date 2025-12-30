'use client';

import { FactorInfluenceChart } from '@/components/charts/factor-influence-chart';
import { RiskOverTimeChart } from '@/components/charts/risk-over-time-chart';
import { AIVsHumanChart } from '@/components/charts/ai-vs-human-chart';
import { useReplayStore } from '@/lib/store';
import type { FactorInfluence, RiskDataPoint, AIVsHumanComparison, DecisionEvent } from '@/lib/types';

export function ChartsPanel() {
  const { events } = useReplayStore();

  if (events.length === 0) {
    return null;
  }

  // Transform events data into chart data
  const factorData: FactorInfluence[] = extractFactorInfluences(events);
  const riskData: RiskDataPoint[] = extractRiskOverTime(events);
  const comparisonData: AIVsHumanComparison[] = extractAIVsHuman(events);

  return (
    <div className="space-y-6">
      <FactorInfluenceChart data={factorData} />
      <RiskOverTimeChart data={riskData} />
      {comparisonData.length > 0 && <AIVsHumanChart data={comparisonData} />}
    </div>
  );
}

function extractFactorInfluences(events: DecisionEvent[]): FactorInfluence[] {
  // Look for AI reasoning event
  const aiEvent = events.find(e => e.eventType === 'AIReasoningGenerated');
  
  if (aiEvent?.payload && typeof aiEvent.payload === 'object' && 'factors' in aiEvent.payload) {
    return aiEvent.payload.factors as FactorInfluence[];
  }

  // Mock data for demo
  return [
    { factor: 'Credit Score', weight: 60 },
    { factor: 'Income Stability', weight: 25 },
    { factor: 'Employment Length', weight: 15 },
  ];
}

function extractRiskOverTime(events: DecisionEvent[]): RiskDataPoint[] {
  return events.map((event) => ({
    timestamp: event.timestamp,
    riskScore: (event.payload && typeof event.payload === 'object' && 'riskScore' in event.payload ? event.payload.riskScore as number : null) || Math.random() * 100,
    eventType: event.eventType,
  }));
}

function extractAIVsHuman(events: DecisionEvent[]): AIVsHumanComparison[] {
  const aiEvent = events.find(e => e.eventType === 'AIReasoningGenerated');
  const humanEvent = events.find(e => e.eventType === 'HumanOverride');

  if (!aiEvent || !humanEvent) {
    return [];
  }

  const getPayloadValue = (payload: unknown, key: string): number => {
    if (payload && typeof payload === 'object' && key in payload) {
      return (payload as Record<string, unknown>)[key] as number;
    }
    return 0;
  };

  return [
    {
      category: 'Risk Score',
      aiRecommendation: getPayloadValue(aiEvent.payload, 'riskScore'),
      humanDecision: getPayloadValue(humanEvent.payload, 'riskScore'),
    },
  ];
}
