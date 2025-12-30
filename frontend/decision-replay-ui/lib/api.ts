import { apiClient } from './api-client';
import type { Decision, CreateDecisionRequest, ReplayResponse } from './types';

export const decisionService = {
  // Create a new decision
  async createDecision(data: CreateDecisionRequest): Promise<Decision> {
    const response = await apiClient.post<Decision>('/decisions', data);
    return response.data;
  },

  // Get decision replay events
  async getReplay(decisionId: string): Promise<ReplayResponse> {
    const response = await apiClient.get<ReplayResponse>(`/replay/${decisionId}`);
    return response.data;
  },
};
