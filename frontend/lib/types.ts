export type DecisionStatus = "DRAFT" | "IN_REVIEW" | "FINALIZED"
export type DecisionOutcome = "DRAFT" | "FEASIBLE" | "RISKY_BUT_POSSIBLE" | "NEEDS_ADJUSTMENT" | "COMMITTED"
export type EventType = "DECISION_CREATED" | "SCOPE_ANALYZED" | "TIMELINE_EVALUATED" | "RESOURCES_ASSESSED" | "CONSTRAINTS_IDENTIFIED" | "FEASIBILITY_GENERATED" | "USER_REFINEMENT" | "DECISION_FINALIZED"

export interface Decision {
  id: string
  title: string                    // e.g., "Launch MVP Product"
  scope: string                    // Features, deliverables
  timeline: string                 // Target dates, milestones
  resources: string                // Team, budget, tools
  constraints: string              // Risks, dependencies, limitations
  status: DecisionStatus
  outcome: DecisionOutcome
  feasibilityScore: number         // 0-100 score from AI analysis
  createdAt: string
  createdBy: string
  finalizedAt?: string
}

export interface DecisionEvent {
  id: string
  decisionId: string
  eventType: EventType
  timestamp: string
  payload: Record<string, any>
}

export interface FeasibilityFactor {
  factorName: string               // e.g., "Timeline", "Resources", "Technical Complexity"
  score: number                    // 0-100
  impact: "positive" | "negative" | "neutral"
  description: string
}

export interface AIFeasibilityAnalysis {
  overallFeasibility: number       // 0-100
  recommendation: DecisionOutcome
  reasoning: string
  confidence: number
  generatedAt: string
  factors: FeasibilityFactor[]
  risks: string[]
  suggestions: string[]
}

export interface UserRefinement {
  changes: string
  reason: string
  refinedBy: string
  refinedAt: string
  previousFeasibilityScore: number
}
