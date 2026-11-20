import { apiClient } from "./api-client"
import type { RequirementDomain, RequirementDraft, RequirementsReview } from "./requirements-types"

export const requirementsApi = {
  domains: async (signal?: AbortSignal) => (await apiClient.get<RequirementDomain[]>("/api/v2/requirements/domains", { signal })).data,
  preview: async (input: string, fixedFields: string[]) =>
    (await apiClient.post<RequirementsReview>("/api/v2/requirements/preview", { input, fixedFields })).data,
  validate: async (draft: RequirementDraft) =>
    (await apiClient.post<RequirementsReview>("/api/v2/requirements/validate", draft)).data,
}
