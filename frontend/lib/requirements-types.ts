export interface RequirementField {
  name: string
  description: string
  type: "Text" | "Number" | "Currency" | "Integer" | "Date" | "List" | "Level"
  required: boolean
}

export interface RequirementDomain {
  domain: string
  displayName: string
  goalField: string | null
  fields: RequirementField[]
  supportsImprovement: boolean
}

export interface RequirementDraft {
  domain: string
  title: string
  goal: string
  fields: Record<string, string>
  originalInput: string
}

export interface RequirementsReview {
  data: { domain: string; title: string; goal: string; fields: Record<string, string>; assumptions: string[] }
  validation: { errors: string[]; warnings: string[]; missingFields: string[]; invalidFields: string[] }
  canSubmit: boolean
}
