"use client"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import type { DecisionContext } from "@/lib/ai-api"

const title = (value: string) => value.replaceAll("_", " ")
export function DecisionContextEditor({ value, onChange, disabled }: { value: DecisionContext; onChange: (value: DecisionContext) => void; disabled: boolean }) {
  function set<K extends keyof DecisionContext>(key: K, field: DecisionContext[K]) { onChange({ ...value, [key]: field }) }
  return <div className="space-y-5">
    <div><Label htmlFor="decision-at">T0 decision timestamp (UTC / ISO 8601)</Label><Input id="decision-at" value={value.decisionAt} onChange={e => set("decisionAt", e.target.value)} disabled={disabled} /><p className="mt-1 text-xs text-muted-foreground">Use a timestamp ending in Z. Evidence and assumptions must have been known by this time.</p></div>
    <div><Label htmlFor="decision-text">Decision context available at T0</Label><Textarea id="decision-text" rows={4} value={value.decisionText} onChange={e => set("decisionText", e.target.value)} disabled={disabled} maxLength={20000} /></div>
    <div className="grid gap-4 sm:grid-cols-2"><div><Label htmlFor="chosen-action">Chosen action</Label><Input id="chosen-action" value={value.chosenAction} onChange={e => set("chosenAction", e.target.value)} disabled={disabled} maxLength={4000} /></div><div><Label htmlFor="expected-outcome">Expected outcome at T0</Label><Input id="expected-outcome" value={value.expectedOutcome} onChange={e => set("expectedOutcome", e.target.value)} disabled={disabled} maxLength={4000} /></div></div>
    <fieldset className="space-y-3"><legend className="mb-2 font-semibold">Structured constraints used for scoring</legend><div className="grid gap-3 sm:grid-cols-2">{Object.entries(value.fields).map(([key, field]) => <div key={key}><Label htmlFor={`context-${key}`} className="capitalize">{title(key)}</Label><Input id={`context-${key}`} value={field} onChange={e => set("fields", { ...value.fields, [key]: e.target.value })} disabled={disabled} maxLength={20000} /></div>)}</div></fieldset>
    {(["evidence", "assumptions", "constraints"] as const).map(group => <fieldset key={group} className="space-y-3"><legend className="font-semibold capitalize">{group}</legend>{value[group].map((statement, index) => <div key={statement.id} className="rounded-lg border p-3">
      <Label htmlFor={`${group}-${statement.id}`}>{statement.id} · statement</Label><Textarea id={`${group}-${statement.id}`} value={statement.text} maxLength={4000} disabled={disabled} onChange={e => set(group, value[group].map((item, i) => i === index ? { ...item, text: e.target.value } : item))} />
      <Label className="mt-3" htmlFor={`${group}-${statement.id}-at`}>Known at (UTC / ISO 8601)</Label><Input id={`${group}-${statement.id}-at`} value={statement.knownAt} disabled={disabled} onChange={e => set(group, value[group].map((item, i) => i === index ? { ...item, knownAt: e.target.value } : item))} />
      <Button variant="ghost" className="mt-2 min-h-11" disabled={disabled} onClick={() => set(group, value[group].filter((_, i) => i !== index))}>Remove {statement.id}</Button>
    </div>)}<Button variant="outline" className="min-h-11" disabled={disabled || value[group].length >= 50} onClick={() => {
      const ids = new Set([...value.evidence, ...value.assumptions, ...value.constraints].map(item => item.id)); let index = 1; const prefix = group[0]
      while (ids.has(`${prefix}${index}`)) index++
      set(group, [...value[group], { id: `${prefix}${index}`, text: "", knownAt: value.decisionAt }])
    }}>Add {group === "evidence" ? "evidence" : group === "assumptions" ? "assumption" : "constraint"}</Button></fieldset>)}
    <div><Label htmlFor="context-alternatives">Alternatives available at T0 (one per line)</Label><Textarea id="context-alternatives" rows={3} value={value.alternatives.join("\n")} disabled={disabled} onChange={e => set("alternatives", e.target.value.split("\n"))} /><p className="mt-2 text-xs text-muted-foreground">Historical availability is user-attested. Keep actual outcomes and later knowledge out of this form.</p></div>
  </div>
}
