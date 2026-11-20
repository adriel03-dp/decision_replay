import Link from "next/link"
import type { AnalysisRecord, DecisionAiAnalysis } from "@/lib/ai-api"

export function AnalysisView({ analysis }: { analysis: DecisionAiAnalysis }) {
  return <div className="space-y-5 text-sm leading-6">
    <p className="whitespace-pre-wrap">{analysis.analysis}</p>
    <p className="text-muted-foreground">Model self-reported confidence: {Math.round(analysis.confidence * 100)}%. This is not a calibrated probability.</p>
    <div className="grid gap-5 md:grid-cols-2">
      {[ ["Assumptions", analysis.assumptions], ["Possible risks", analysis.risks] ].map(([heading, claims]) => <section key={heading as string}>
        <h3 className="font-semibold">{heading as string}</h3>
        <ul className="mt-2 space-y-3">{(claims as DecisionAiAnalysis["risks"]).map((claim, i) => <li key={i} className="border-l-2 border-emerald-500/40 pl-3">
          {claim.text}<span className="block text-xs text-muted-foreground">{claim.isHypothesis ? "Hypothesis" : "Evidence-linked claim"}{claim.evidenceIds.length > 0 && ` · ${claim.evidenceIds.join(", ")}`}</span>
        </li>)}</ul>
      </section>)}
    </div>
    <section><h3 className="font-semibold">Missing information</h3><ul className="mt-2 list-disc space-y-1 pl-5">{analysis.missingInformation.map((item, i) => <li key={i}>{item}</li>)}</ul></section>
    <section><h3 className="font-semibold">Alternatives to consider</h3><div className="mt-2 grid gap-3 md:grid-cols-2">{analysis.alternatives.map((item, i) => <div key={i} className="rounded-lg border p-4">
      <h4 className="font-medium">{item.name}</h4><p className="mt-1 text-muted-foreground">{item.tradeoff}</p>
      <p className="mt-2 text-xs text-muted-foreground">{item.isHypothesis ? "Proposed hypothesis" : "Evidence-linked"}{item.evidenceIds.length > 0 && ` · ${item.evidenceIds.join(", ")}`}</p>
    </div>)}</div></section>
  </div>
}

export function AnalysisRecordView({ record }: { record: AnalysisRecord }) {
  return <div><AnalysisView analysis={record.analysis} /><Link className="mt-5 inline-block text-sm font-medium text-emerald-700 underline dark:text-emerald-300" href={`/ai-lab?execution=${record.executionId}`}>Inspect execution metadata</Link></div>
}
