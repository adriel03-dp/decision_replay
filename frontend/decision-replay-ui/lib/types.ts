// API Types
export interface Decision {
  id: string;
  type: string;
  status: string;
  currentOutcome: string;
  createdAt: string;
}

export interface DecisionEvent {
  eventType: string;
  timestamp: string;
  payload: unknown;
}

export interface ReplayResponse {
  decisionId: string;
  events: DecisionEvent[];
}

export interface CreateDecisionRequest {
  type: string;
  createdBy: string;
  inputData: Record<string, unknown>;
}

// Chart Data Types
export interface FactorInfluence {
  factor: string;
  weight: number;
}

export interface RiskDataPoint {
  timestamp: string;
  riskScore: number;
  eventType: string;
}

export interface AIVsHumanComparison {
  category: string;
  aiRecommendation: number;
  humanDecision: number;
}
