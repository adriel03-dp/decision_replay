"use client"

import { useState } from "react"
import { Button } from "@/components/ui/button"
import { AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent, AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle, AlertDialogTrigger } from "@/components/ui/alert-dialog"
import { decisionApi } from "@/lib/api"
import type { DecisionEngineResponse, ReplayDecisionResponse } from "@/lib/api-types"
import { requestErrorMessage } from "@/lib/request-error"

export function AcceptImprovement({ decision, onAccepted }: { decision: DecisionEngineResponse; onAccepted: (result: ReplayDecisionResponse) => void }) {
  const [open, setOpen] = useState(false)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const proposal = decision.improvement
  if (proposal?.status !== "proposed" || !proposal.constraintCheck?.estimatesWithinLimits || !proposal.assessment) return null
  async function accept() {
    setBusy(true); setError(null)
    try { const result = await decisionApi.acceptImprovement(decision.decisionId, decision.version); setOpen(false); onAccepted(result) }
    catch (reason) { setError(requestErrorMessage(reason, "The plan could not be adopted. Your original version is unchanged.")) }
    finally { setBusy(false) }
  }
  return <div className="mt-5 rounded-xl border border-emerald-400/25 bg-emerald-400/5 p-5">
    <h2 className="font-semibold text-slate-100">Ready to use the proposed plan?</h2>
    <p className="mt-2 text-sm leading-6 text-slate-300">Review what changes and what is deferred. Adopting it creates version {decision.version + 1}; version {decision.version} remains available.</p>
    <AlertDialog open={open} onOpenChange={value => { if (!busy) setOpen(value) }}>
      <AlertDialogTrigger asChild><Button className="mt-4 min-h-11 bg-emerald-400 text-black hover:bg-emerald-300">Review and adopt proposed plan</Button></AlertDialogTrigger>
      <AlertDialogContent>
        <AlertDialogHeader><AlertDialogTitle>Adopt this improvement as a new version?</AlertDialogTitle><AlertDialogDescription>These are hypothetical changes, not evidence of an achieved outcome. Confirm that the revised scope, cost assumptions and tradeoffs are acceptable.</AlertDialogDescription></AlertDialogHeader>
        <div className="max-h-72 space-y-4 overflow-auto text-sm leading-6">
          <dl className="space-y-3">{proposal.changes.map(change => <div key={change.field}><dt className="font-semibold capitalize">{change.field.replaceAll("_", " ")}</dt><dd className="break-words">Current: {change.from}</dd><dd className="break-words">Proposed: {change.to}</dd></div>)}</dl>
          <ul className="list-disc space-y-2 pl-5">{proposal.tradeoffs.map(tradeoff => <li key={tradeoff}>{tradeoff}</li>)}</ul>
          {decision.structuredFields.required_scope && decision.structuredFields.required_scope !== "none" && <p>Must-haves retained: {decision.structuredFields.required_scope}</p>}
        </div>
        {error && <div role="alert" className="text-sm text-destructive"><p>{error}</p><button type="button" className="min-h-11 underline" onClick={() => window.location.reload()}>Reload report</button></div>}
        <AlertDialogFooter><AlertDialogCancel disabled={busy}>Keep current version</AlertDialogCancel><AlertDialogAction disabled={busy} onClick={event => { event.preventDefault(); void accept() }}>{busy ? "Adopting plan…" : "Confirm and create new version"}</AlertDialogAction></AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  </div>
}
