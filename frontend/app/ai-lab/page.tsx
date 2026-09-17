"use client"

import { useEffect, useState } from "react"
import axios from "axios"
import Link from "next/link"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { AnalysisView } from "@/components/ai-analysis"
import { aiApi, type AiConfiguration, type AiExecution, type AiExecutionSummary, type AiExperiment, type EvaluationMetrics } from "@/lib/ai-api"

const percent = (value: number) => `${(value * 100).toFixed(1)}%`
const seconds = (value: number) => `${(value / 1000).toFixed(2)}s`
const selectClass = "h-11 w-full rounded-md border bg-background px-3 text-sm focus-visible:outline-2 focus-visible:outline-ring"

export default function AiLabPage() {
  const [configuration, setConfiguration] = useState<AiConfiguration | null>(null)
  const [experiments, setExperiments] = useState<AiExperiment[]>([])
  const [executions, setExecutions] = useState<AiExecutionSummary[]>([])
  const [detail, setDetail] = useState<AiExecution | null>(null)
  const [provider, setProvider] = useState("ollama")
  const [model, setModel] = useState("")
  const [prompt, setPrompt] = useState("v3")
  const [selected, setSelected] = useState<string[]>([])
  const [comparison, setComparison] = useState<{ id: string; target: { provider: string; model: string }; metrics: EvaluationMetrics }[] | null>(null)
  const [error, setError] = useState("")
  const [loading, setLoading] = useState(true)
  const [running, setRunning] = useState(false)
  const [inspecting, setInspecting] = useState(false)
  const [comparing, setComparing] = useState(false)

  async function refresh() {
    const [runs, requests] = await Promise.all([aiApi.experiments(), aiApi.executions()])
    setExperiments(runs); setExecutions(requests)
  }
  useEffect(() => {
    let active = true
    Promise.all([aiApi.configuration(), aiApi.experiments(), aiApi.executions()]).then(async ([config, runs, requests]) => {
      if (!active) return
      setConfiguration(config); setProvider(config.analysis.provider); setModel(config.analysis.model)
      setExperiments(runs); setExecutions(requests)
      const id = new URLSearchParams(window.location.search).get("execution")
      if (id) { const execution = await aiApi.execution(id); if (active) setDetail(execution) }
    }).catch(() => { if (active) setError("Unable to load AI engineering data.") }).finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [])

  function fail(error: unknown) { setError(axios.isAxiosError(error) ? error.response?.data?.message ?? "The request failed." : "The request failed.") }
  async function evaluate() {
    setError(""); setRunning(true)
    try { await aiApi.evaluate({ provider, model }, prompt); await refresh() } catch (e) { fail(e); await refresh().catch(() => undefined) } finally { setRunning(false) }
  }
  async function inspect(id: string) { setError(""); setInspecting(true); try { setDetail(await aiApi.execution(id)) } catch (e) { fail(e) } finally { setInspecting(false) } }
  async function compare() { setError(""); setComparing(true); try { setComparison(await aiApi.compare(selected)) } catch (e) { fail(e) } finally { setComparing(false) } }

  return <main className="min-h-screen bg-background px-5 pb-16 pt-28 text-foreground">
    <div className="mx-auto max-w-6xl space-y-7">
      <header className="border-b pb-7"><p className="text-xs font-semibold uppercase tracking-widest text-emerald-700 dark:text-emerald-300">Decision Replay / AI engineering</p>
        <h1 className="mt-3 text-3xl font-semibold tracking-tight">Experiments & execution evidence</h1>
        <p className="mt-3 max-w-3xl text-muted-foreground">Run controlled decision cases, inspect validated model output, and compare recorded results. Every value below comes from a saved execution.</p>
      </header>
      {error && <div role="alert" className="rounded-lg border border-destructive/40 p-4 text-destructive">{error}</div>}
      <p aria-live="polite" className="text-sm text-muted-foreground">{loading ? "Loading experiments…" : running ? "Evaluating three cases sequentially. Local inference may take several minutes. Keep this page open." : `${experiments.length} recorded experiments · extraction uses ${configuration?.extraction.provider ?? "the configured provider"}`}</p>
      <Card><CardHeader><CardTitle>Run a controlled experiment</CardTitle></CardHeader><CardContent className="space-y-5">
        <div className="grid gap-4 sm:grid-cols-3"><div><Label htmlFor="provider">Provider</Label><select id="provider" className={selectClass} value={provider} disabled={running || loading} onChange={e => {
          setProvider(e.target.value); const target = [configuration?.analysis, configuration?.extraction].find(item => item?.provider === e.target.value); setModel(target?.model ?? "")
        }}><option value="ollama">Ollama / local</option><option value="groq">Groq / cloud</option></select></div>
          <div><Label htmlFor="model">Model</Label><Input id="model" className="h-11" value={model} onChange={e => setModel(e.target.value)} disabled={running || loading} maxLength={200} /></div>
          <div><Label htmlFor="prompt">Analysis prompt</Label><select id="prompt" className={selectClass} value={prompt} disabled={running || loading} onChange={e => setPrompt(e.target.value)}>{configuration?.prompts.filter(item => item.operation === "decision-analysis").map(item => <option key={item.version}>{item.version}</option>)}</select></div>
        </div>
        <p className="text-sm text-muted-foreground">Golden dataset v1: startup demand, career transition, and portal delivery. Cloud runs need server-side credentials. Model downloads and provider fallback are disabled for benchmarks.</p>
        <Button className="min-h-11" onClick={evaluate} disabled={running || loading || !model.trim()}>{running ? "Evaluation running…" : "Evaluate three golden cases"}</Button>
      </CardContent></Card>

      <section aria-labelledby="experiments-heading"><div className="mb-4 flex flex-wrap items-center justify-between gap-3"><h2 id="experiments-heading" className="text-xl font-semibold">Recorded experiments</h2><Button variant="outline" className="min-h-11" onClick={compare} disabled={selected.length < 2 || selected.length > 6 || comparing}>{comparing ? "Comparing…" : "Compare selected models"}</Button></div>
        <p className="mb-3 text-sm text-muted-foreground">Concept coverage and citation integrity are deterministic proxies, not proof of semantic groundedness. Fair model comparisons require matching dataset, prompt, and parameters. API cost is unknown unless measured; local hardware has a cost.</p>
        <div className="overflow-x-auto rounded-xl border"><table className="w-full text-left text-sm"><caption className="sr-only">Saved model evaluations with measured validity, concept coverage, latency and failures</caption><thead className="bg-muted"><tr>{["Select", "Model / prompt", "Status", "Schema valid", "Missing info", "Risk concepts", "Alternatives", "Avg latency", "Failures"].map(label => <th scope="col" key={label} className="whitespace-nowrap p-3 font-medium">{label}</th>)}</tr></thead><tbody>
          {experiments.map(run => <tr key={run.id} className="border-t"><td className="p-3"><input type="checkbox" className="h-5 w-5 accent-emerald-600" aria-label={`Select ${run.target.model} ${run.promptVersion} ${run.id}`} checked={selected.includes(run.id)} onChange={e => setSelected(current => e.target.checked ? [...current, run.id] : current.filter(id => id !== run.id))} /></td>
            <th scope="row" className="p-3 font-medium">{run.target.model}<span className="block text-xs text-muted-foreground">{run.target.provider} · {run.promptVersion} · {new Date(run.startedAt).toLocaleString()}</span></th><td className="p-3">{run.status.replaceAll("_", " ")}</td>
            <td className="p-3">{percent(run.metrics.structuredValidity)}</td><td className="p-3">{percent(run.metrics.missingInformationCoverage)}</td><td className="p-3">{percent(run.metrics.riskConceptCoverage)}</td><td className="p-3">{percent(run.metrics.alternativeConceptCoverage)}</td><td className="p-3">{seconds(run.metrics.latencyMs)}</td><td className="p-3">{percent(run.metrics.failureRate)}</td></tr>)}
          {!loading && experiments.length === 0 && <tr><td colSpan={9} className="p-7 text-center text-muted-foreground">No evaluations yet. Run the dataset to produce the first measured results.</td></tr>}
        </tbody></table></div>
        {comparison && <div className="mt-4 grid gap-3 sm:grid-cols-2">{comparison.map(item => <div key={item.id} className="rounded-lg border p-4"><h3 className="font-semibold">{item.target.model}</h3><p className="mt-2 text-sm">Validity {percent(item.metrics.structuredValidity)} · failures {percent(item.metrics.failureRate)} · average {seconds(item.metrics.latencyMs)}</p></div>)}</div>}
      </section>

      {experiments.map(run => <details key={run.id} className="rounded-xl border p-5"><summary className="cursor-pointer font-medium">Case results · {run.target.model} / {run.promptVersion}</summary><p className="mt-3 break-all text-xs text-muted-foreground">Dataset hash: {run.datasetHash}<br />Prompt hash: {run.promptHash}</p><div className="mt-5 space-y-6">{run.results.map(result => <section key={result.caseId} className="border-t pt-4"><h3 className="mb-3 font-semibold">{result.caseId} · {result.success ? "validated" : result.errorCode ?? "failed"}</h3>{result.output && <AnalysisView analysis={result.output} />}{result.executionId && <Button variant="outline" className="mt-4 min-h-11" onClick={() => inspect(result.executionId!)} disabled={inspecting}>Inspect execution</Button>}</section>)}</div></details>)}

      <section aria-labelledby="execution-heading"><h2 id="execution-heading" className="mb-4 text-xl font-semibold">AI execution trail</h2><div className="space-y-2">{executions.map(item => <button type="button" key={item.id} onClick={() => inspect(item.id)} disabled={inspecting} className="flex min-h-14 w-full flex-wrap items-center justify-between gap-2 rounded-lg border p-4 text-left hover:bg-muted focus-visible:outline-2 focus-visible:outline-ring">
        <span className="font-medium">{item.operation}<span className="ml-2 text-sm text-muted-foreground">{item.provider} / {item.model} · {item.promptVersion}</span></span><span className="text-sm">{item.status} · {seconds(item.latencyMs)} · {item.retryCount} retries</span>
      </button>)}{!loading && executions.length === 0 && <p className="text-muted-foreground">No AI execution records yet.</p>}</div></section>

      {detail && <Card><CardHeader><CardTitle>Execution detail · {detail.operation}</CardTitle></CardHeader><CardContent className="space-y-4 text-sm"><dl className="grid gap-4 sm:grid-cols-2">
        {Object.entries({ "Execution ID": detail.id, "Request ID": detail.requestId, "Context version": detail.contextVersion ?? "none", "Prompt": detail.promptVersion, "Status": detail.status, "Request timeout": detail.requestTimeoutSeconds == null ? "not recorded" : `${detail.requestTimeoutSeconds}s`, "Maximum retries": detail.maxRetries ?? "not recorded", "Construction": `${detail.promptConstructionMs.toFixed(2)}ms`, "Input hash": detail.inputHash, "Prompt hash": detail.promptHash, "Output schema hash": detail.outputSchemaHash ?? "JSON-object mode", "Fallback used": String(detail.usedFallback), "Parameters": JSON.stringify(detail.parameters) }).map(([key, value]) => <div key={key}><dt className="text-muted-foreground">{key}</dt><dd className="mt-1 break-all font-mono text-xs">{value}</dd></div>)}
        </dl>{detail.decisionId && <Link className="inline-block underline" href={`/decisions/${detail.decisionId}/history`}>Open decision history</Link>}
        {detail.attempts.map(attempt => <div key={attempt.number} className="rounded-lg border p-4"><h3 className="font-medium">Attempt {attempt.number} · {attempt.provider} / {attempt.model}</h3><p className="mt-2 text-muted-foreground">Inference {seconds(attempt.inferenceMs)} · validation {attempt.validationMs.toFixed(2)}ms · tokens {attempt.inputTokens ?? "unknown"} in / {attempt.outputTokens ?? "unknown"} out · {attempt.errorCode ?? "validated"}</p><p className="mt-2 break-all text-xs text-muted-foreground">Schema enforced: {String(attempt.schemaEnforced)} · model revision: {attempt.modelRevision ?? "not supplied"}{attempt.repairPromptVersion && ` · repair ${attempt.repairPromptVersion}`}</p>{attempt.validationError && <p className="mt-2 text-destructive">{attempt.validationError}</p>}{attempt.output && <details className="mt-3"><summary className="cursor-pointer">Inspect untrusted model output</summary><pre className="mt-3 max-h-80 overflow-auto whitespace-pre-wrap break-words rounded-md bg-muted p-3 text-xs">{attempt.output}</pre></details>}</div>)}
      </CardContent></Card>}
    </div>
  </main>
}
