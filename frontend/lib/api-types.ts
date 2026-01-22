// ============= Backend API Types (matching .NET DTOs) =============

// V2 Decision Response - matches DecisionV2Response from backend
export interface Decision {
  id: string;
  naturalLanguageInput: string;
  inferredAttributes: Record<string, any>;
  domainType?: string;
  status: DecisionStatus;
  outcome: DecisionOutcome;
  createdAt: string;
  lastModifiedAt?: string;
  createdBy: string;
  analysis?: AnalysisResponse;
  feasibilityScore?: number;
}

// V2 Analysis Response - matches AnalysisResponse from backend
export interface AnalysisResponse {
  analysisId: string;
  feasibilityScore: number;
  feasibilityVerdict: string;
  executiveSummary: string;
  pros: string[];
  cons: string[];
  risks: RiskResponse[];
  assumptions: string[];
  recommendations: string[];
  confidenceLevel: number;
  generatedAt: string;
  modelUsed: string;
}

export interface RiskResponse {
  description: string;
  impact: string;
  mitigation?: string;
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
  input: string;        // V2 uses natural language input
  createdBy?: string;   // Optional - backend gets from JWT
  analyzeNow?: boolean; // Optional - default true. If false, creates Draft without analysis.
}

export interface UpdateDecisionRequest {
  updatedInput?: string;
  status?: string;
}

// ============= Extended Types for UI =============

export interface DecisionExtended extends Decision {
  analysis?: AnalysisResponse;
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
// These match the backend DecisionStatus and DecisionOutcome enums

export type DecisionStatus = 'Draft' | 'InReview' | 'Finalized';
export type DecisionOutcome = 'Draft' | 'Feasible' | 'RiskyButPossible' | 'NeedsAdjustment' | 'Committed';

// ============= Event Types =============

export type EventType =
  | 'INPUT_CAPTURED'
  | 'RULE_EVALUATED'
  | 'AI_REASONING'
  | 'HUMAN_OVERRIDE'
  | 'RISK_CALCULATED'
  | 'DECISION_FINALIZED'
  | 'AUDIT_LOGGED';

