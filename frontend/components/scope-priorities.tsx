"use client"

import { splitScope } from "@/lib/requirement-draft"

export function ScopePriorities({ scope, required, onChange, disabled }: {
  scope: string; required: string; onChange: (value: string) => void; disabled: boolean
}) {
  const items = splitScope(scope)
  const selected = splitScope(required)
  const orphaned = selected.filter(item => !items.some(candidate => candidate.toLowerCase() === item.toLowerCase()))
  function toggle(item: string, checked: boolean) {
    const next = selected.filter(value => value.toLowerCase() !== item.toLowerCase())
    if (checked) next.push(item)
    onChange(next.join(", ") || "none")
  }
  return <fieldset disabled={disabled} className="rounded-xl border border-white/15 p-4">
    <legend className="px-2 font-semibold text-slate-100">Must-have deliverables</legend>
    <p className="text-sm leading-6 text-slate-300">Selected items must remain in every proposed plan, even when other scope can be deferred.</p>
    {items.length ? items.map(item => <label key={item.toLowerCase()} className="flex min-h-11 cursor-pointer items-start gap-3 py-2 text-sm leading-6 text-slate-100">
      <input type="checkbox" className="mt-1 h-4 w-4 shrink-0 accent-emerald-400" checked={selected.some(value => value.toLowerCase() === item.toLowerCase())} onChange={event => toggle(item, event.target.checked)} />
      <span className="break-words">{item}</span>
    </label>) : <p className="mt-2 text-sm text-slate-400">List scope items first to choose must-haves.</p>}
    {orphaned.map(item => <div key={item} className="mt-2 text-sm text-amber-200"><p>Must-have missing from scope: {item}</p><button type="button" className="min-h-11 underline" onClick={() => toggle(item, false)}>Explicitly remove this must-have</button></div>)}
  </fieldset>
}
