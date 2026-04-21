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

// ============= Hybrid Decision Types (new pipeline) =============

export interface ProjectEvaluationRequest {
  projectType: string;
  features: string[];
  budgetUsd: number;
  timelineMonths: number;
  teamSize: number;
  rawInput?: string;
  requestAiEnhancement?: boolean;
}

export interface SimulationRequest {
  baseProject: ProjectEvaluationRequest;
  budgetUsd?: number;
  timelineMonths?: number;
  teamSize?: number;
  addFeatures?: string[];
  removeFeatures?: string[];
}

export interface ProjectInputDto {
  projectType: string;
  features: string[];
  budgetUsd: number;
  timelineMonths: number;
  teamSize: number;
  isStructured: boolean;
  rawInput?: string;
}

export interface FeasibilityIssueDto {
  dimension: string;
  message: string;
  severity: string;
}

export interface SuggestedAdjustmentDto {
  parameter: string;
  description: string;
  quantitativeImpact?: string;
}

export interface FeasibilityResultDto {
  score: number;
  verdict: string;
  budgetFitScore: number;
  timelineFitScore: number;
  teamCapacityScore: number;
  complexityScore: number;
  estimatedCostUsd: number;
  estimatedMonths: number;
  requiredTeamSize: number;
  issues: FeasibilityIssueDto[];
  suggestedAdjustments: SuggestedAdjustmentDto[];
  explainability: string[];
  generatedAt: string;
}

export interface TimelinePhaseDto {
  name: string;
  startMonth: number;
  endMonth: number;
  durationMonths: number;
  tasks: string[];
  deliverables: string[];
  percentageOfTotal: number;
}

export interface ProjectPlanDto {
  totalMonths: number;
  phases: TimelinePhaseDto[];
  milestones: string[];
  generatedAt: string;
}

export interface ProjectEvaluationResponse {
  input: ProjectInputDto;
  feasibility: FeasibilityResultDto;
  plan: ProjectPlanDto;
  aiEnhancement: null;
  aiAvailable: boolean;
  generatedAt: string;
}

export interface SavedDecisionResponse {
  id: string;
  analysis: ProjectEvaluationResponse | null;
  createdAt: string;
}

export interface DecisionSummaryResponse {
  id: string;
  projectType: string;
  features: string[];
  feasibilityScore: number | null;
  verdict: string | null;
  status: string;
  createdAt: string;
}

export interface SimulationResponse {
  adjustedInput: ProjectInputDto;
  newFeasibility: FeasibilityResultDto;
  scoreDelta: number;
  verdictDelta: string;
  impactSummary: string[];
}

