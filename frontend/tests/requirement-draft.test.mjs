import test from "node:test"
import assert from "node:assert/strict"
import { changeDraftDomain, draftFromReview, requirementPayload, splitScope, toggleDraftLock } from "../lib/requirement-draft.ts"

const project = { domain: "project_planning", goalField: "project_goal", supportsImprovement: true,
  fields: ["project_goal", "budget", "timeline_months", "scope", "required_scope", "fixed_fields", "currency"].map(name => ({ name })) }
const career = { domain: "career_decision", goalField: null, supportsImprovement: false, fields: [{ name: "timeline_months" }, { name: "current_role" }] }
const draft = () => ({ domain: "project_planning", title: " App ", goal: " Deliver a mobile app ", originalInput: "The original requirement had budget 100000 USD", fields: { project_goal: "Old extracted goal", budget: " 110000 ", currency: "usd", scope: "login, reporting", required_scope: "reporting", fixed_fields: "budget, timeline_months", invented_score: "100" } })

test("confirmed correction preserves the narrative and synchronizes the goal field", () => {
  const source = draft()
  const payload = requirementPayload(source, project)
  assert.equal(payload.fields.budget, "110000")
  assert.equal(payload.originalInput, source.originalInput)
  assert.equal(payload.fields.project_goal, "Deliver a mobile app")
  assert.equal(payload.fields.currency, "USD")
  assert.equal(payload.fields.invented_score, undefined)
  assert.equal(source.fields.budget, " 110000 ")
})
test("switching categories discards incompatible fields rather than sending hidden stale values", () => {
  const changed = changeDraftDomain(draft(), career)
  assert.equal(changed.domain, "career_decision")
  assert.equal(changed.fields.budget, undefined)
  assert.equal(changed.fields.required_scope, undefined)
  assert.equal(changed.fields.fixed_fields, undefined)
  assert.equal(changed.originalInput, draft().originalInput)
})
test("unlocking one limit retains the others and requires an explicit none to unlock all", () => {
  const budgetFlexible = toggleDraftLock(draft(), "budget", false)
  assert.equal(budgetFlexible.fields.fixed_fields, "timeline_months")
  assert.equal(toggleDraftLock(budgetFlexible, "timeline_months", false).fields.fixed_fields, "none")
  assert.equal(toggleDraftLock(budgetFlexible, "scope", true).fields.fixed_fields, "timeline_months, scope")
})
test("scope priorities support separators and deduplicate names without changing their order", () => {
  assert.deepEqual(splitScope("login, Reporting\nLOGIN; payments|none"), ["login", "Reporting", "payments"])
})
test("draft preparation copies server fields rather than mutating a retained review", () => {
  const review = { data: { domain: "project_planning", title: "App", goal: "Deliver an app", fields: { Budget: "100000" } } }
  const result = draftFromReview(review, "original")
  result.fields.budget = "90000"
  assert.equal(review.data.fields.Budget, "100000")
})
