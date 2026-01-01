// ============= Backend API Types (matching .NET DTOs) =============

export interface Decision {
  id: string;
  type: string;
  status: string;
  currentOutcome: string;
  createdAt: string;
  createdBy?: string;
  riskScore?: number;
}

export interface DecisionEvent {
  id?: string;
  decisionId?: string;
  eventType: string;
  timestamp: string;
  payload: any; // Using any for flexibility with event payloads
}

export interface ReplayResponse {
  decisionId: string;
  events: DecisionEvent[];
}

export interface CreateDecisionRequest {
  type: string;
  createdBy: string;
  inputData: Record<string, any>;
}

export interface UpdateDecisionRequest {
  status?: string;
  outcome?: string;
  riskScore?: number;
}

// ============= Extended Types for UI =============

export interface DecisionExtended extends Decision {
  outcome?: string;
  finalizedAt?: string;
  confidence?: number;
  events?: DecisionEvent[];
}

// ============= Chart & Analytics Data Types =============

export interface FactorInfluence {
  factor: string;
  weight: number;
  impact?: 'positive' | 'negative' | 'neutral';
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

// ============= AI Reasoning Types =============

export interface AIReasoning {
  reasoning: string;
  confidence: number;
  source: string;
  generatedAt: string;
  factors: FactorInfluence[];
}

export interface AIExplanationResponse {
  id: string;
  explanation: string;
  generatedAt: string;
}

// ============= Audit Types =============

export interface AuditEntry {
  timestamp: string;
  actor: string;
  action: string;
  details: Record<string, any>;
}

export interface AuditTrail {
  decisionId: string;
  entries: AuditEntry[];
  exportedAt: string;
}

// ============= Decision Status & Outcome Enums =============

export type DecisionStatus = 'DRAFT' | 'IN_REVIEW' | 'FINALIZED';
export type DecisionOutcome = 'APPROVED' | 'REJECTED' | 'PENDING' | 'FLAGGED';
export type DecisionType = 
  | 'LOAN_APPROVAL' 
  | 'FRAUD_DETECTION' 
  | 'CLAIM_PROCESSING' 
  | 'PRICING_ADJUSTMENT'
  | 'RISK_ASSESSMENT'
  | 'CREDIT_EVALUATION';

// ============= Event Types =============

export type EventType =
  | 'INPUT_CAPTURED'
  | 'RULE_EVALUATED'
  | 'AI_REASONING'
  | 'HUMAN_OVERRIDE'
  | 'RISK_CALCULATED'
  | 'DECISION_FINALIZED'
  | 'AUDIT_LOGGED';

