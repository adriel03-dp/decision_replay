export type RiskLevel = "Low" | "Medium" | "High" | "Critical"
export type FactorConfidence = "Low" | "Medium" | "High"

export interface FactorBreakdown {
  factor: string
  score: number
  weight: number
  weightedScore: number
  reason: string
  assumption?: string | null
  confidence: FactorConfidence
}

export interface DecisionRisk {
  code: string
  factor: string
  severity: string
  message: string
  mitigation: string
}

export interface AuditTrailEntry {
  id: string
  action: string
  version: number
  actor: string
  summary: string
  details: Record<string, string>
  timestamp: string
}

export interface PlanTask {
  taskId: string
  taskName: string
  description: string
  priority: string
  estimatedEffort: string
  dependencies: string[]
  riskNotes: string
  successCriteria: string
}

export interface PlanPhase {
  phaseKey: string
  phaseName: string
  startWeek: number
  endWeek: number
  startDate: string
  endDate: string
  goal: string
  tasks: PlanTask[]
}

export interface PlanMilestone {
  name: string
  targetWeek: number
  targetDate: string
  successCriteria: string
}

export interface ActionPlan {
  planId: string
  decisionId: string
  version: number
  timelineMonths: number
  startDate: string
  endDate: string
  feasibilityScore: number
  riskLevel: string
  phases: PlanPhase[]
  milestones: PlanMilestone[]
  generatedAt: string
}

export interface DecisionEngineResponse {
  decisionId: string
  version: number
  domain: string
  title: string
  goal: string
  naturalLanguageInput: string
  structuredFields: Record<string, string>
  feasibilityScore: number
  riskLevel: RiskLevel | string
  factorBreakdown: FactorBreakdown[]
  risks: DecisionRisk[]
  assumptions: string[]
  missingFields: string[]
  recommendations: string[]
  explanation: string
  plan: ActionPlan | null
  auditTrail: AuditTrailEntry[]
  createdAt: string
}

export interface DecisionEngineSummary {
  decisionId: string
  version: number
  domain: string
  title: string
  feasibilityScore: number
  riskLevel: RiskLevel | string
  missingFieldCount: number
  riskCount: number
  updatedAt: string
}

export interface DecisionFieldChange {
  field: string
  from?: string | null
  to?: string | null
}

export interface PlanChange {
  type: string
  oldValue: string
  newValue: string
  impact: string
}

export interface ReplayComparison {
  previousVersion: number
  newVersion: number
  changedFields: DecisionFieldChange[]
  scoreDelta: number
  riskDelta: string
  mainReason: string
  languageSummary: string
  planChanges: PlanChange[]
  comparedAt: string
}

export interface ReplayDecisionResponse {
  decision: DecisionEngineResponse
  comparison: ReplayComparison
}
