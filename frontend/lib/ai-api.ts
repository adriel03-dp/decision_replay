import { apiClient } from "./api-client"
import type { ReplayComparison } from "./api-types"

export interface AiTarget { provider: string; model: string }
export interface ContextStatement { id: string; text: string; knownAt: string }
export interface DecisionContext {
  decisionAt: string; decisionText: string; chosenAction: string; expectedOutcome: string
  fields: Record<string, string>; evidence: ContextStatement[]; assumptions: ContextStatement[]
  constraints: ContextStatement[]; alternatives: string[]; provenance: string
}
export interface GroundedClaim { text: string; evidenceIds: string[]; isHypothesis: boolean }
export interface DecisionAiAnalysis {
  assumptions: GroundedClaim[]; risks: GroundedClaim[]; missingInformation: string[]
  alternatives: { name: string; tradeoff: string; evidenceIds: string[]; isHypothesis: boolean }[]
  analysis: string; confidence: number
}
export interface AnalysisRecord { id: string; executionId: string; createdAt: string; analysis: DecisionAiAnalysis }
export interface RecordedOutcome { id: string; decisionVersion: number; observedAt: string; recordedAt: string; description: string; outcomeQuality: string }
export interface ReplayScenario {
  id: string; name: string; baseVersion: number; context: DecisionContext; fieldChanges: Record<string, string>
  assumptionChanges: Record<string, string>; constraintChanges: Record<string, string>
  assessment: { feasibilityScore: number; riskLevel: string | number }; comparison: ReplayComparison; analyses: AnalysisRecord[]
}
export interface DecisionTimeline {
  versions: { version: number; context: DecisionContext; analyses: AnalysisRecord[] }[]
  outcomes: RecordedOutcome[]; scenarios: ReplayScenario[]
}
export interface EvaluationMetrics {
  structuredValidity: number; missingInformationCoverage: number; riskConceptCoverage: number
  alternativeConceptCoverage: number; citationIntegrity: number; uncitedNonHypothesisClaims: number
  latencyMs: number; failureRate: number
}
export interface AiExperiment {
  id: string; datasetVersion: string; datasetHash: string; target: AiTarget; promptVersion: string; promptHash: string
  requestTimeoutSeconds?: number; maxRetries?: number; startedAt: string; finishedAt?: string; status: string; metrics: EvaluationMetrics; apiCost: number | null
  results: { caseId: string; executionId?: string; success: boolean; errorCode?: string; metrics: EvaluationMetrics; output?: DecisionAiAnalysis; humanReview?: string }[]
}
export interface AiExecutionSummary {
  id: string; decisionId?: string; replayId?: string; contextVersion?: number; experimentId?: string
  operation: string; promptVersion: string; status: string; latencyMs: number; startedAt: string
  retryCount: number; usedFallback: boolean; errorCode?: string; provider?: string; model?: string
}
export interface AiExecution extends AiExecutionSummary {
  requestId: string; inputHash: string; promptHash: string; promptConstructionMs: number
  parameters: { temperature: number; maxOutputTokens: number }; output?: string
  outputSchemaHash?: string; requestTimeoutSeconds?: number; maxRetries?: number
  attempts: { number: number; provider: string; model: string; modelRevision?: string; schemaEnforced: boolean; repairPromptVersion?: string; repairPromptHash?: string; inferenceMs: number; validationMs: number; errorCode?: string; validationError?: string; inputTokens?: number; outputTokens?: number; output?: string }[]
}
export interface AiConfiguration {
  extraction: AiTarget; analysis: AiTarget; totalSeconds: number; maxRetries: number
  prompts: { operation: string; version: string; hash: string }[]; datasetVersions: string[]
}
export interface Reflection { decisionProcessAssessment: string; outcomeAssessment: string; luckAndUncertainty: string; lessons: string[] }

export const aiApi = {
  configuration: async () => (await apiClient.get<AiConfiguration>("/api/ai/configuration")).data,
  executions: async (decisionId?: string) => (await apiClient.get<AiExecutionSummary[]>("/api/ai/executions", { params: { decisionId } })).data,
  execution: async (id: string) => (await apiClient.get<AiExecution>(`/api/ai/executions/${id}`)).data,
  experiments: async () => (await apiClient.get<AiExperiment[]>("/api/ai/experiments")).data,
  evaluate: async (target: AiTarget, promptVersion: string) => (await apiClient.post<AiExperiment[]>("/api/ai/experiments", { targets: [target], datasetVersion: "v1", promptVersion })).data,
  compare: async (ids: string[]) => (await apiClient.get<{ id: string; target: AiTarget; status: string; metrics: EvaluationMetrics }[]>("/api/ai/experiments/comparison", { params: { ids }, paramsSerializer: { indexes: null } })).data,
  timeline: async (id: string) => (await apiClient.get<DecisionTimeline>(`/api/v2/decisions/${id}/timeline`)).data,
  context: async (id: string, context: DecisionContext) => (await apiClient.post<{ version: number }>(`/api/v2/decisions/${id}/context`, context)).data,
  scenario: async (id: string, name: string, baseVersion: number, fields: Record<string, string>, assumptions: Record<string, string>, constraints: Record<string, string>) =>
    (await apiClient.post<ReplayScenario>(`/api/v2/decisions/${id}/scenarios`, { name, baseVersion, fields, assumptions, constraints })).data,
  analyse: async (id: string, version: number, scenarioId?: string) => (await apiClient.post<AnalysisRecord>(`/api/v2/decisions/${id}/ai-analysis`, { version, scenarioId, promptVersion: "v3" })).data,
  outcome: async (id: string, outcome: Omit<RecordedOutcome, "id" | "recordedAt">) => (await apiClient.post<RecordedOutcome>(`/api/v2/decisions/${id}/outcomes`, outcome)).data,
  reflect: async (id: string, outcomeId: string, analysisId: string) => (await apiClient.post<{ value: Reflection; executionId: string }>(`/api/v2/decisions/${id}/outcome-reflection`, { outcomeId, analysisId })).data,
}
