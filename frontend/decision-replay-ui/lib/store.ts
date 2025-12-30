import { create } from 'zustand';
import type { DecisionEvent, ReplayResponse } from './types';

interface ReplayStore {
  // State
  decisionId: string | null;
  events: DecisionEvent[];
  currentEventIndex: number;
  isLoading: boolean;
  error: string | null;

  // Actions
  setDecisionId: (id: string) => void;
  setEvents: (events: DecisionEvent[]) => void;
  setCurrentEventIndex: (index: number) => void;
  setLoading: (loading: boolean) => void;
  setError: (error: string | null) => void;
  loadReplay: (data: ReplayResponse) => void;
  nextEvent: () => void;
  previousEvent: () => void;
  goToEvent: (index: number) => void;
  reset: () => void;
}

export const useReplayStore = create<ReplayStore>((set, get) => ({
  // Initial state
  decisionId: null,
  events: [],
  currentEventIndex: 0,
  isLoading: false,
  error: null,

  // Actions
  setDecisionId: (id) => set({ decisionId: id }),
  
  setEvents: (events) => set({ events }),
  
  setCurrentEventIndex: (index) => set({ currentEventIndex: index }),
  
  setLoading: (loading) => set({ isLoading: loading }),
  
  setError: (error) => set({ error }),
  
  loadReplay: (data) => set({
    decisionId: data.decisionId,
    events: data.events,
    currentEventIndex: 0,
    error: null,
  }),
  
  nextEvent: () => {
    const { currentEventIndex, events } = get();
    if (currentEventIndex < events.length - 1) {
      set({ currentEventIndex: currentEventIndex + 1 });
    }
  },
  
  previousEvent: () => {
    const { currentEventIndex } = get();
    if (currentEventIndex > 0) {
      set({ currentEventIndex: currentEventIndex - 1 });
    }
  },
  
  goToEvent: (index) => {
    const { events } = get();
    if (index >= 0 && index < events.length) {
      set({ currentEventIndex: index });
    }
  },
  
  reset: () => set({
    decisionId: null,
    events: [],
    currentEventIndex: 0,
    isLoading: false,
    error: null,
  }),
}));
