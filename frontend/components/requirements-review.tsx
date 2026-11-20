"use client"

import { Button } from "@/components/ui/button"
import { RequirementField } from "@/components/requirement-field"
import { ScopePriorities } from "@/components/scope-priorities"
import { changeDraftDomain, splitScope, toggleDraftLock } from "@/lib/requirement-draft"
import type { RequirementDomain, RequirementDraft, RequirementsReview as ReviewResult } from "@/lib/requirements-types"

export function RequirementsReview({ draft, domains, review, dirty, acknowledged, busy, onChange, onAcknowledge, onValidate, onCreate, onBack }: {
  draft: RequirementDraft; domains: RequirementDomain[]; review: ReviewResult | null; dirty: boolean; acknowledged: boolean; busy: string | null
  onChange: (draft: RequirementDraft) => void; onAcknowledge: (value: boolean) => void; onValidate: () => void; onCreate: () => void; onBack: () => void
}) {
  const domain = domains.find(item => item.domain === draft.domain)
  if (!domain) return <p role="alert">This domain is unavailable. Return to your description and retry.</p>
  function field(name: string, value: string) { onChange({ ...draft, fields: { ...draft.fields, [name]: value } }) }
  const locks = splitScope(draft.fields.fixed_fields ?? "budget, timeline_months")
  const editableFields = domain.fields.filter(item => ![domain.goalField, "fixed_fields", "required_scope"].includes(item.name))
  return <section aria-labelledby="requirements-review-heading" className="space-y-5 rounded-xl border border-white/15 bg-slate-950/80 p-5 sm:p-7">
    <div><h2 id="requirements-review-heading" tabIndex={-1} className="text-xl font-semibold text-slate-100 outline-none">Review requirements before scoring</h2><p className="mt-2 text-sm leading-6 text-slate-300">Correct the extracted values, mark must-haves and check the draft. Nothing is saved until you create the assessment.</p></div>
    <fieldset disabled={!!busy} className="space-y-5">
      <div><label htmlFor="requirement-domain" className="text-sm font-medium text-slate-100">Decision category</label><select id="requirement-domain" value={draft.domain} className="mt-2 min-h-11 w-full rounded-lg border border-white/20 bg-slate-950 px-3 text-sm text-slate-100" onChange={event => { const next = domains.find(item => item.domain === event.target.value); if (next) onChange(changeDraftDomain(draft, next)) }}>{domains.map(item => <option key={item.domain} value={item.domain}>{item.displayName}</option>)}</select></div>
      <div><label htmlFor="requirement-title" className="text-sm font-medium text-slate-100">Short title</label><input id="requirement-title" value={draft.title} maxLength={200} className="mt-2 min-h-11 w-full rounded-lg border border-white/20 bg-slate-950 px-3 text-sm text-slate-100" onChange={event => onChange({ ...draft, title: event.target.value })} /></div>
      <div><label htmlFor="requirement-goal" className="text-sm font-medium text-slate-100">Goal and expected result</label><textarea id="requirement-goal" value={draft.goal} maxLength={20000} aria-describedby="requirement-goal-help" rows={3} className="mt-2 w-full rounded-lg border border-white/20 bg-slate-950 p-3 text-sm text-slate-100" onChange={event => onChange({ ...draft, goal: event.target.value })} /><p id="requirement-goal-help" className="mt-1 text-sm text-slate-400">Use at least 10 characters to describe the intended result.</p></div>
      <div className="grid gap-5 sm:grid-cols-2">{editableFields.map(item => <RequirementField key={item.name} field={item} value={draft.fields[item.name] ?? ""} onChange={value => field(item.name, value)} disabled={!!busy} invalid={!dirty && !!review?.validation.invalidFields.includes(item.name)} />)}</div>
      {domain.supportsImprovement && <>
        <fieldset className="rounded-xl border border-white/15 p-4"><legend className="px-2 font-semibold text-slate-100">Fixed limits</legend>{[["budget", "Keep budget fixed"], ["timeline_months", "Keep timeline fixed"], ["scope", "Keep every scope item"]].map(([name, label]) => <label key={name} className="flex min-h-11 cursor-pointer items-center gap-3 text-sm text-slate-100"><input type="checkbox" className="h-4 w-4 accent-emerald-400" checked={locks.includes(name)} onChange={event => onChange(toggleDraftLock(draft, name, event.target.checked))} />{label}</label>)}</fieldset>
        <ScopePriorities scope={draft.fields.scope ?? ""} required={draft.fields.required_scope ?? ""} onChange={value => field("required_scope", value)} disabled={!!busy} />
      </>}
    </fieldset>
    {!dirty && review && <div aria-live="polite" className="space-y-2 text-sm leading-6">
      {review.validation.errors.map(message => <p key={message} className="text-red-300">{message}</p>)}
      {!!review.validation.missingFields.length && <p className="text-amber-200">Missing context: {review.validation.missingFields.map(name => name.replaceAll("_", " ")).join(", ")}. Missing values receive conservative assumptions.</p>}
      {!review.canSubmit && <p className="text-red-300">Correct invalid values before creating the assessment.</p>}
    </div>}
    {dirty ? <Button type="button" className="min-h-11 bg-sky-400 text-black hover:bg-sky-300" onClick={onValidate} disabled={!!busy}>{busy === "validate" ? "Checking requirements…" : "Check corrected requirements"}</Button>
      : <label className="flex min-h-11 cursor-pointer items-start gap-3 py-2 text-sm leading-6 text-slate-200"><input type="checkbox" className="mt-1 h-4 w-4 shrink-0 accent-emerald-400" checked={acknowledged} disabled={!!busy || !review?.canSubmit} onChange={event => onAcknowledge(event.target.checked)} /><span>I reviewed these requirements, fixed limits and must-haves, and understand any missing-information warnings.</span></label>}
    <div className="flex flex-wrap gap-3"><Button type="button" variant="outline" className="min-h-11" onClick={onBack} disabled={!!busy}>Start again from description</Button><Button type="button" className="min-h-11 bg-emerald-400 text-black hover:bg-emerald-300" onClick={onCreate} disabled={!!busy || dirty || !acknowledged || !review?.canSubmit}>{busy === "create" ? "Creating assessment…" : review?.validation.missingFields.length ? "Create assessment with gaps" : "Create assessment"}</Button></div>
  </section>
}
