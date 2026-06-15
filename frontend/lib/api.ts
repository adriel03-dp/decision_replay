import { apiClient } from "./api-client"
import type {
  ActionPlan,
  DecisionEngineResponse,
  DecisionEngineSummary,
  ReplayDecisionResponse,
} from "./api-types"

export const decisionApi = {
  async analyze(input: string): Promise<DecisionEngineResponse> {
    const response = await apiClient.post<DecisionEngineResponse>("/api/v2/decisions", { input })
    return response.data
  },

  async list(): Promise<DecisionEngineSummary[]> {
    const response = await apiClient.get<DecisionEngineSummary[]>("/api/v2/decisions")
    return response.data
  },

  async get(decisionId: string): Promise<DecisionEngineResponse> {
    const response = await apiClient.get<DecisionEngineResponse>(`/api/v2/decisions/${decisionId}`)
    return response.data
  },

  async remove(decisionId: string): Promise<void> {
    await apiClient.delete(`/api/v2/decisions/${decisionId}`)
  },

  async replay(decisionId: string, updatedInput: string): Promise<ReplayDecisionResponse> {
    const response = await apiClient.post<ReplayDecisionResponse>(
      `/api/v2/decisions/${decisionId}/replay`,
      { updatedInput },
    )
    return response.data
  },

  async generatePlan(decisionId: string): Promise<ActionPlan> {
    const response = await apiClient.post<ActionPlan>(
      `/api/v2/decisions/${decisionId}/plans/generate`,
    )
    return response.data
  },

  async downloadPlan(
    decisionId: string,
    planId: string,
    format: "pdf" | "excel",
  ): Promise<void> {
    const response = await apiClient.get(
      `/api/v2/decisions/${decisionId}/plans/${planId}/export/${format}`,
      { responseType: "blob" },
    )
    const extension = format === "excel" ? "xlsx" : "pdf"
    const url = URL.createObjectURL(response.data)
    const anchor = document.createElement("a")
    anchor.href = url
    anchor.download = `decision-plan-${decisionId}.${extension}`
    document.body.appendChild(anchor)
    anchor.click()
    anchor.remove()
    URL.revokeObjectURL(url)
  },
}

export async function checkApiHealth(): Promise<boolean> {
  try {
    await apiClient.get("/health")
    return true
  } catch {
    return false
  }
}
