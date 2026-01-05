import { create } from "zustand"
import type { DecisionExtended, DecisionEvent } from "./api-types"

interface DecisionStore {
  currentDecision: DecisionExtended | null
  currentEventIndex: number
  events: DecisionEvent[]
  isPlaying: boolean

  // Filters
  statusFilter: string[]
  riskRange: [number, number]
  typeFilter: string
  searchId: string

  // Actions
  setCurrentDecision: (decision: DecisionExtended, events: DecisionEvent[]) => void
  setCurrentEventIndex: (index: number) => void
  setIsPlaying: (playing: boolean) => void
  setStatusFilter: (statuses: string[]) => void
  setRiskRange: (range: [number, number]) => void
  setTypeFilter: (type: string) => void
  setSearchId: (id: string) => void

  // Computed
  getCurrentEvent: () => DecisionEvent | null
}

export const useDecisionStore = create<DecisionStore>((set, get) => ({
  currentDecision: null,
  currentEventIndex: 0,
  events: [],
  isPlaying: false,
  statusFilter: [],
  riskRange: [0, 100],
  typeFilter: "",
  searchId: "",

  setCurrentDecision: (decision, events) =>
    set({
      currentDecision: decision,
      events,
      currentEventIndex: 0,
    }),

  setCurrentEventIndex: (index) => set({ currentEventIndex: index }),
  setIsPlaying: (playing) => set({ isPlaying: playing }),
  setStatusFilter: (statuses) => set({ statusFilter: statuses }),
  setRiskRange: (range) => set({ riskRange: range }),
  setTypeFilter: (type) => set({ typeFilter: type }),
  setSearchId: (id) => set({ searchId: id }),

  getCurrentEvent: () => {
    const { events, currentEventIndex } = get()
    return events[currentEventIndex] || null
  },
}))
