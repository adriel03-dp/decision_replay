"use client"

import { useEffect, useState } from "react"
import { useParams } from "next/navigation"
import Link from "next/link"
import axios from "axios"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { AnalysisRecordView } from "@/components/ai-analysis"
import { DecisionContextEditor } from "@/components/decision-context-editor"
import { aiApi, type DecisionTimeline, type DecisionContext, type Reflection } from "@/lib/ai-api"

const selectClass = "h-11 w-full rounded-md border bg-background px-3 text-sm focus-visible:outline-2 focus-visible:outline-ring"

export default function HistoryPage() {
  const { id } = useParams<{ id: string }>()
  const [timeline, setTimeline] = useState<DecisionTimeline | null>(null)
  const [version, setVersion] = useState(1)
  const [draft, setDraft] = useState<DecisionContext | null>(null)
  const [fields, setFields] = useState<Record<string, string>>({})
  const [assumptions, setAssumptions] = useState<Record<string, string>>({})
  const [constraints, setConstraints] = useState<Record<string, string>>({})
  const [name, setName] = useState("Reduced resource scenario")
  const [description, setDescription] = useState("")
  const [observedAt, setObservedAt] = useState("")
  const [quality, setQuality] = useState("unknown")
  const [reflection, setReflection] = useState<{ value: Reflection; executionId: string } | null>(null)
  const [revealed, setRevealed] = useState<string | null>(null)
  const [error, setError] = useState("")
  const [status, setStatus] = useState("")
  const [busy, setBusy] = useState(false)
  const [loading, setLoading] = useState(true)
  const current = timeline?.versions.find(item => item.version === version)
  const initial = current?.analyses.at(-1)

  function choose(data: DecisionTimeline, number: number) {
    setTimeline(data); setVersion(number); setDraft(structuredClone(data.versions.find(item => item.version === number)!.context))
    setFields({}); setAssumptions({}); setConstraints({}); setRevealed(null); setReflection(null)
  }
  useEffect(() => {
    let active = true
    aiApi.timeline(id).then(data => { if (active) { setObservedAt(new Date().toISOString()); choose(data, data.versions.at(-1)!.version) } }).catch(() => { if (active) setError("Unable to load decision history.") }).finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [id])
  async function refresh(number = version) { const data = await aiApi.timeline(id); choose(data, number) }
  async function action(message: string, task: () => Promise<void>) {
    setBusy(true); setError(""); setStatus(message)
    try { await task(); setStatus("Saved. Historical records are preserved.") } catch (e) { setError(axios.isAxiosError(e) ? e.response?.data?.message ?? "Request failed. Check the entered values and runtime availability." : "Request failed."); setStatus("") } finally { setBusy(false) }
  }
  function change(key: string, value: string, original: string, setter: (value: React.SetStateAction<Record<string, string>>) => void) {
    setter(current => { const next = { ...current }; if (value === original) delete next[key]; else next[key] = value; return next })
  }

  if (loading) return <main className="px-5 pt-28" aria-live="polite">Loading historical context…</main>
  return <main className="min-h-screen bg-background px-5 pb-16 pt-28 text-foreground"><div className="mx-auto max-w-6xl space-y-7">
    <header className="border-b pb-7"><Link href={`/decisions/${id}/analytics`} className="text-sm text-muted-foreground underline">Back to decision report</Link><p className="mt-5 text-xs font-semibold uppercase tracking-widest text-emerald-700 dark:text-emerald-300">Historical decision workspace</p><h1 className="mt-3 text-3xl font-semibold tracking-tight">What was reasonable at the time?</h1><p className="mt-3 max-w-3xl text-muted-foreground">Freeze what was known at T0. Explore explicit what-if changes. Reveal T1 only after the original-context analysis. A good outcome does not prove a good decision process.</p></header>
    {error && <div role="alert" className="rounded-lg border border-destructive/40 p-4 text-destructive">{error}</div>}
    <p role="status" aria-live="polite" className="text-sm text-muted-foreground">{status}</p>
    {timeline && current && draft && <>
      <div className="max-w-xs"><Label htmlFor="base-version">Historical base version</Label><select id="base-version" className={selectClass} value={version} disabled={busy} onChange={e => choose(timeline, Number(e.target.value))}>{timeline.versions.map(item => <option value={item.version} key={item.version}>Version {item.version} · {new Date(item.context.decisionAt).toLocaleDateString()}</option>)}</select></div>
      <div className="grid gap-6 lg:grid-cols-2">
        <Card><CardHeader><CardTitle>T0 / context & evidence</CardTitle></CardHeader><CardContent className="space-y-5"><p className="text-xs text-muted-foreground">Provenance: {current.context.provenance}</p>
          <DecisionContextEditor value={draft} onChange={setDraft} disabled={busy} />
          <div className="flex flex-wrap gap-3"><Button className="min-h-11" disabled={busy} onClick={() => action("Saving a new context version…", async () => {
            const saved = await aiApi.context(id, { ...draft, alternatives: draft.alternatives.map(item => item.trim()).filter(Boolean) }); await refresh(saved.version)
          })}>Save T0 as a new version</Button><Button variant="outline" className="min-h-11" disabled={busy} onClick={() => action("Analysing the saved T0 context without outcome data…", async () => { await aiApi.analyse(id, version); await refresh() })}>Analyse saved T0</Button></div>
          <p className="text-xs text-muted-foreground">Analysis uses the saved snapshot. Save edits as a new version first. Numeric grades are deterministic feasibility heuristics; AI advice does not change them.</p>
        </CardContent></Card>
        <div className="space-y-6"><Card><CardHeader><CardTitle>Original T0 analysis</CardTitle></CardHeader><CardContent>{initial ? <AnalysisRecordView record={initial} /> : <p className="text-sm text-muted-foreground">No initial analysis for this version. Run the saved T0 analysis before revealing an outcome.</p>}{current.analyses.length > 1 && <details className="mt-5"><summary className="cursor-pointer text-sm">Earlier T0 analyses ({current.analyses.length - 1})</summary><div className="mt-4 space-y-6">{current.analyses.slice(0, -1).map(record => <AnalysisRecordView key={record.id} record={record} />)}</div></details>}</CardContent></Card>
          <Card><CardHeader><CardTitle>What-if / explicit changes</CardTitle></CardHeader><CardContent className="space-y-4"><div><Label htmlFor="scenario-name">Scenario name</Label><Input id="scenario-name" value={name} maxLength={200} disabled={busy} onChange={e => setName(e.target.value)} /></div><p className="text-sm text-muted-foreground">Only edited values are recorded. The original version stays intact. Narrative changes affect AI advice; change corresponding structured fields to affect numeric scoring.</p>
            {Object.entries(current.context.fields).map(([key, value]) => <div key={key}><Label htmlFor={`scenario-${key}`} className="capitalize">{key.replaceAll("_", " ")} <span className="font-normal text-muted-foreground">· original {value}</span></Label><Input id={`scenario-${key}`} value={fields[key] ?? value} disabled={busy} onChange={e => change(key, e.target.value, value, setFields)} /></div>)}
            {(["assumptions", "constraints"] as const).map(group => current.context[group].map(item => <div key={item.id}><Label htmlFor={`scenario-${item.id}`}>{group === "assumptions" ? "Assumption" : "Constraint"} {item.id}</Label><Input id={`scenario-${item.id}`} value={(group === "assumptions" ? assumptions : constraints)[item.id] ?? item.text} disabled={busy} onChange={e => change(item.id, e.target.value, item.text, group === "assumptions" ? setAssumptions : setConstraints)} /></div>))}
            <Button className="min-h-11" disabled={busy || Object.keys(fields).length + Object.keys(assumptions).length + Object.keys(constraints).length === 0} onClick={() => action("Creating an immutable scenario and analysing its T0 inputs…", async () => {
              const scenario = await aiApi.scenario(id, name, version, fields, assumptions, constraints); setTimeline(await aiApi.timeline(id)); await aiApi.analyse(id, version, scenario.id); await refresh()
            })}>Create and analyse scenario</Button>
          </CardContent></Card>
        </div>
      </div>
      <section aria-labelledby="scenarios-heading"><h2 id="scenarios-heading" className="mb-4 text-xl font-semibold">Preserved replay scenarios</h2><div className="space-y-4">{timeline.scenarios.filter(scenario => scenario.baseVersion === version).map(scenario => <Card key={scenario.id}><CardHeader><CardTitle>{scenario.name}</CardTitle></CardHeader><CardContent className="space-y-4">
        <p className="text-sm">Feasibility delta: {scenario.comparison.scoreDelta > 0 ? "+" : ""}{scenario.comparison.scoreDelta} points · {scenario.comparison.riskDelta}</p><p className="text-sm text-muted-foreground">{scenario.comparison.mainReason}</p>
        <dl className="grid gap-2 text-sm sm:grid-cols-2">{scenario.comparison.changedFields.map(change => <div key={change.field}><dt className="font-medium">{change.field.replaceAll("_", " ")}</dt><dd>{change.from ?? "missing"} → {change.to ?? "missing"}</dd></div>)}</dl>
        {Object.keys(scenario.assumptionChanges).length + Object.keys(scenario.constraintChanges).length > 0 && <p className="text-sm text-muted-foreground">Narrative changes: {[...Object.entries(scenario.assumptionChanges), ...Object.entries(scenario.constraintChanges)].map(([key, value]) => `${key}: ${value}`).join("; ")}</p>}
        {scenario.analyses.at(-1) ? <AnalysisRecordView record={scenario.analyses.at(-1)!} /> : <Button variant="outline" className="min-h-11" disabled={busy} onClick={() => action("Analysing the preserved scenario…", async () => { await aiApi.analyse(id, version, scenario.id); await refresh() })}>Run scenario analysis</Button>}
      </CardContent></Card>)}{timeline.scenarios.filter(scenario => scenario.baseVersion === version).length === 0 && <p className="text-sm text-muted-foreground">No scenarios for this version yet.</p>}</div></section>

      <Card><CardHeader><CardTitle>T1 / record & reveal an outcome</CardTitle></CardHeader><CardContent className="space-y-5"><p className="text-sm text-muted-foreground">Outcome records are separate from historical context. Recording one never changes the original grade or AI analysis.</p>
        <div className="grid gap-4 sm:grid-cols-2"><div><Label htmlFor="observed-at">Observed at (UTC / ISO 8601)</Label><Input id="observed-at" value={observedAt} disabled={busy} onChange={e => setObservedAt(e.target.value)} /></div><div><Label htmlFor="outcome-quality">Observed outcome quality</Label><select id="outcome-quality" className={selectClass} value={quality} disabled={busy} onChange={e => setQuality(e.target.value)}>{["unknown", "positive", "negative", "mixed"].map(value => <option key={value}>{value}</option>)}</select></div></div>
        <div><Label htmlFor="outcome-description">What actually happened?</Label><Textarea id="outcome-description" rows={3} maxLength={8000} value={description} disabled={busy} onChange={e => setDescription(e.target.value)} /></div><Button variant="outline" className="min-h-11" disabled={busy || !description.trim()} onClick={() => action("Recording a separate outcome…", async () => {
          await aiApi.outcome(id, { decisionVersion: version, observedAt, description, outcomeQuality: quality }); setDescription(""); await refresh()
        })}>Record outcome for version {version}</Button>
        {timeline.outcomes.filter(outcome => outcome.decisionVersion === version).map(outcome => <div key={outcome.id} className="rounded-lg border p-4"><p className="text-sm">Outcome observed {new Date(outcome.observedAt).toLocaleString()}</p><Button className="mt-3 min-h-11" disabled={busy || !initial} onClick={() => action("Revealing T1 for a separate outcome reflection…", async () => {
          const result = await aiApi.reflect(id, outcome.id, initial!.id); setReflection(result); setRevealed(outcome.id)
        })}>Reveal T1 and compare</Button>{revealed === outcome.id && <p className="mt-3 whitespace-pre-wrap text-sm">{outcome.description} · observed quality: {outcome.outcomeQuality}</p>}</div>)}
        {reflection && <section className="space-y-4 border-t pt-5" aria-label="Separate outcome reflection">{([["Decision process at T0", reflection.value.decisionProcessAssessment], ["Observed outcome at T1", reflection.value.outcomeAssessment], ["Luck & uncertainty", reflection.value.luckAndUncertainty]] as const).map(([heading, text]) => <div key={heading}><h3 className="font-semibold">{heading}</h3><p className="mt-2 whitespace-pre-wrap text-sm text-muted-foreground">{text}</p></div>)}<h3 className="font-semibold">Lessons for future decisions</h3><ul className="list-disc pl-5 text-sm">{reflection.value.lessons.map((lesson, i) => <li key={i}>{lesson}</li>)}</ul><Link className="inline-block text-sm underline" href={`/ai-lab?execution=${reflection.executionId}`}>Inspect reflection execution</Link></section>}
      </CardContent></Card>
    </>}
  </div></main>
}
