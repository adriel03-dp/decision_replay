import type { RequirementDomain, RequirementDraft, RequirementsReview } from "./requirements-types"

export function splitScope(value = ""): string[] {
  const seen = new Set<string>()
  return value.split(/[,;\n|]/).map(item => item.trim()).filter(item => {
    const key = item.toLowerCase()
    if (!key || key === "none" || seen.has(key)) return false
    seen.add(key)
    return true
  })
}

export function draftFromReview(review: RequirementsReview, originalInput: string): RequirementDraft {
  return { domain: review.data.domain, title: review.data.title, goal: review.data.goal,
    fields: Object.fromEntries(Object.entries(review.data.fields).map(([key, value]) => [key.toLowerCase(), value])), originalInput }
}

export function changeDraftDomain(draft: RequirementDraft, domain: RequirementDomain): RequirementDraft {
  const supported = new Set(domain.fields.map(field => field.name))
  const fields = Object.fromEntries(Object.entries(draft.fields).filter(([key]) => supported.has(key)))
  if (domain.goalField) fields[domain.goalField] = draft.goal
  if (domain.supportsImprovement) {
    const locks = splitScope(draft.fields.fixed_fields ?? "budget, timeline_months").filter(name => supported.has(name))
    fields.fixed_fields = locks.join(", ") || "none"
  }
  return { ...draft, domain: domain.domain, fields }
}

export function requirementPayload(draft: RequirementDraft, domain: RequirementDomain): RequirementDraft {
  const supported = new Set(domain.fields.map(field => field.name))
  const fields = Object.fromEntries(Object.entries(draft.fields)
    .filter(([key, value]) => supported.has(key) && value.trim())
    .map(([key, value]) => [key, value.trim()]))
  if (domain.goalField) fields[domain.goalField] = draft.goal.trim()
  if (fields.currency) fields.currency = fields.currency.toUpperCase()
  return { ...draft, domain: domain.domain, goal: draft.goal.trim(), title: draft.title.trim(), fields }
}

export function toggleDraftLock(draft: RequirementDraft, field: string, fixed: boolean): RequirementDraft {
  const locks = new Set(splitScope(draft.fields.fixed_fields ?? "budget, timeline_months"))
  if (fixed) locks.add(field)
  else locks.delete(field)
  return { ...draft, fields: { ...draft.fields, fixed_fields: [...locks].join(", ") || "none" } }
}
