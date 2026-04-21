import { apiClient } from './api-client';
import type {
  Decision, CreateDecisionRequest, ReplayResponse, DecisionEvent, DecisionExtended,
  ProjectEvaluationRequest, ProjectEvaluationResponse,
  SimulationRequest, SimulationResponse,
  SavedDecisionResponse, DecisionSummaryResponse,
} from './api-types';

export const decisionApi = {
  // ============= Decision CRUD =============

  // Get all decisions
  async getAllDecisions(): Promise<DecisionExtended[]> {
    try {
      const response = await apiClient.get<Decision[]>('/v2/decisions');
      return response.data as DecisionExtended[];
    } catch (error) {
      // Return empty array on error instead of throwing
      return [];
    }
  },

  // Get single decision by ID
  async getDecisionById(decisionId: string): Promise<DecisionExtended | null> {
    try {
      const response = await apiClient.get<Decision>(`/v2/decisions/${decisionId}`);
      return response.data as DecisionExtended;
    } catch (error) {
      return null;
    }
  },

  // Create a new decision (V2 - natural language)
  async createDecision(data: CreateDecisionRequest): Promise<Decision> {
    const response = await apiClient.post<Decision>('/v2/decisions', {
      input: data.input,
      createdBy: data.createdBy || 'current-user',  // Will be overridden by backend from JWT
      analyzeNow: data.analyzeNow ?? true // Default to true if not specified
    });
    return response.data;
  },

  // Update a decision (V2)
  async updateDecision(decisionId: string, data: Partial<Decision>): Promise<Decision> {
    const response = await apiClient.put<Decision>(`/v2/decisions/${decisionId}`, {
      updatedInput: data.naturalLanguageInput
    });
    return response.data;
  },

  // Delete a decision (V2)
  async deleteDecision(decisionId: string): Promise<void> {
    await apiClient.delete(`/v2/decisions/${decisionId}`);
  },

  // ============= Hybrid Pipeline =============

  // Evaluate project (deterministic, no save)
  async evaluateProject(request: ProjectEvaluationRequest): Promise<ProjectEvaluationResponse> {
    const response = await apiClient.post<ProjectEvaluationResponse>('/v2/decisions/evaluate', request);
    return response.data;
  },

  // Simulate scenario adjustments (no save)
  async simulateProject(request: SimulationRequest): Promise<SimulationResponse> {
    const response = await apiClient.post<SimulationResponse>('/v2/decisions/simulate', request);
    return response.data;
  },

  // Evaluate + save
  async createAndEvaluate(request: ProjectEvaluationRequest): Promise<SavedDecisionResponse> {
    const response = await apiClient.post<SavedDecisionResponse>('/v2/decisions', request);
    return response.data;
  },

  // List saved decisions (summary)
  async listDecisions(): Promise<DecisionSummaryResponse[]> {
    const response = await apiClient.get<DecisionSummaryResponse[]>('/v2/decisions');
    return response.data;
  },

  // Get saved decision by id
  async getSavedDecision(id: string): Promise<SavedDecisionResponse | null> {
    try {
      const response = await apiClient.get<SavedDecisionResponse>(`/v2/decisions/${id}`);
      return response.data;
    } catch {
      return null;
    }
  },

  // ============= Replay & Events =============

  // Get decision replay events
  async getReplay(decisionId: string): Promise<ReplayResponse> {
    const response = await apiClient.get<ReplayResponse>(`/replay/${decisionId}`);
    return response.data;
  },

  // Get decision events
  async getEvents(decisionId: string): Promise<DecisionEvent[]> {
    try {
      const response = await apiClient.get<DecisionEvent[]>(`/decisions/${decisionId}/events`);
      return response.data;
    } catch (error) {
      console.error('Failed to fetch events:', error);
      return [];
    }
  },

  // ============= Decision with Events Combined =============

  // Get decision with its events (for replay page)
  async getDecisionWithEvents(decisionId: string): Promise<DecisionExtended | null> {
    try {
      const [decision, replay] = await Promise.all([
        this.getDecisionById(decisionId),
        this.getReplay(decisionId)
      ]);

      if (!decision) return null;

      return {
        ...decision,
        events: replay.events
      };
    } catch (error) {
      console.error('Failed to fetch decision with events:', error);
      return null;
    }
  }
};

// ============= AI & Analytics APIs =============

export const aiApi = {
  // Generate AI analysis via backend (recommended - keeps API key secure)
  async analyzeDecision(decisionId: string): Promise<any> {
    try {
      const response = await apiClient.post(`/v2/decisions/${decisionId}/analyze`);
      return response.data;
    } catch (error) {
      return {
        decisionId,
        reasoning: {
          analysis: 'AI analysis is currently unavailable.',
          isPlaceholder: true
        }
      };
    }
  },

  // Generate AI explanation (frontend route - less secure, for backward compatibility)
  async generateExplanation(decisionId: string): Promise<{ explanation: string; generatedAt: string }> {
    try {
      const response = await apiClient.post<{ explanation: string; generatedAt: string }>(
        `/api/decisions/${decisionId}/explain`
      );
      return response.data;
    } catch (error) {
      console.error('AI explanation not available:', error);
      return {
        explanation: 'AI explanation will be available once the Gemini API key is configured.',
        generatedAt: new Date().toISOString()
      };
    }
  },

  // Get factor analysis
  async getFactors(decisionId: string): Promise<any[]> {
    try {
      const response = await apiClient.get(`/api/decisions/${decisionId}/factors`);
      return response.data;
    } catch (error) {
      console.error('Factor analysis not available:', error);
      return [];
    }
  },

  // Ask a question about a specific decision (V2 - with guardrails)
  async askQuestion(decisionId: string, question: string): Promise<any> {
    try {
      const response = await apiClient.post(`/v2/decisions/${decisionId}/query`, {
        question
      });
      return response.data;
    } catch (error) {
      return {
        response: 'Unable to get an answer at this time.',
        isError: true
      };
    }
  }
};

// ============= Health Check =============

// Helper to check if API is available
export async function checkApiHealth(): Promise<boolean> {
  try {
    await apiClient.get('/health');
    return true;
  } catch {
    return false;
  }
}
