import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import type { DecisionEngineResponse, EconomicEstimate } from "@/lib/api-types"

function amount(value: number, currency: string) {
  return `${currency} ${value.toLocaleString("en-US", { maximumFractionDigits: 2 })}`
}

function CostSummary({ cost }: { cost: EconomicEstimate }) {
  return (
    <div className="space-y-4">
      <dl className="grid gap-4 sm:grid-cols-2">
        {[
          ["Available budget", amount(cost.availableBudget, cost.currency)],
          ["Estimated delivery cost", amount(cost.estimatedCost, cost.currency)],
          ["Monthly burn", amount(cost.monthlyBurn, cost.currency)],
          ["Funding gap", amount(cost.fundingGap, cost.currency)],
        ].map(([label, value]) => (
          <div key={label} className="min-w-0">
            <dt className="text-sm text-slate-400">{label}</dt>
            <dd className="mt-1 break-words text-lg font-semibold text-slate-100">{value}</dd>
          </div>
        ))}
      </dl>
      <p className="text-sm leading-6 text-slate-300">
        Gross runway: {cost.runwayMonths.toFixed(1)} months before contingency.
        Monthly cost per person: {amount(cost.monthlyCostPerPerson, cost.currency)}.
        Cost includes {cost.contingencyPercent}% contingency.
      </p>
      <details className="rounded-lg border border-white/15 px-4">
        <summary className="min-h-11 cursor-pointer py-3 text-sm text-sky-300 focus-visible:outline-2 focus-visible:outline-offset-4 focus-visible:outline-sky-300">Review cost assumptions</summary>
        <ul className="mb-4 list-disc space-y-2 pl-5 text-sm leading-6 text-slate-300">
          {cost.assumptions.map((assumption) => <li key={assumption}>{assumption}</li>)}
        </ul>
      </details>
    </div>
  )
}

export function RulesAssessment({ decision }: { decision: DecisionEngineResponse }) {
  const proposal = decision.improvement
  if (!decision.rulesVersion) return null

  return (
    <section aria-label="Cost assessment and proposed improvements" className="mt-5 space-y-4">
      <p className="text-sm leading-6 text-slate-300">{decision.scoreMeaning}</p>
      {!!decision.validationWarnings?.length && (
        <div className="rounded-xl border border-amber-400/30 bg-amber-400/5 p-5">
          <h2 className="font-semibold text-amber-200">Review input gaps</h2>
          <ul className="mt-2 list-disc space-y-1 pl-5 text-sm leading-6 text-slate-300">
            {decision.validationWarnings.map((warning) => <li key={warning}>{warning}</li>)}
          </ul>
        </div>
      )}
      <div className="grid items-start gap-5 lg:grid-cols-2">
        <Card className="min-w-0 border-white/10 bg-slate-950/75">
          <CardHeader><CardTitle className="text-base">Delivery economics</CardTitle></CardHeader>
          <CardContent>
            {decision.economics ? <CostSummary cost={decision.economics} /> : (
              <p className="text-sm leading-6 text-slate-300">No supported delivery-cost estimate is available. Project, startup and product estimates need a valid budget, team size and timeline. For other currencies, supply a monthly cost per person in that currency.</p>
            )}
          </CardContent>
        </Card>
        <Card className="min-w-0 border-white/10 bg-slate-950/75">
          <CardHeader><CardTitle className="text-base">Proposed plan improvement</CardTitle></CardHeader>
          <CardContent className="space-y-4">
            {proposal?.status === "proposed" && proposal.assessment ? (
              <>
                <p className="text-2xl font-semibold text-slate-100">
                  {decision.feasibilityScore.toFixed(1)} → {proposal.assessment.feasibilityScore.toFixed(1)} / 100
                  <span className="ml-3 inline-block text-base text-emerald-300">+{proposal.scoreDelta.toFixed(1)} points</span>
                </p>
                <p className="text-sm leading-6 text-slate-300">{proposal.notice}</p>
                <dl className="space-y-3">
                  {proposal.changes.map((change) => (
                    <div key={change.field} className="rounded-lg border border-white/15 p-4 text-sm leading-6">
                      <dt className="font-semibold capitalize text-sky-300">{change.field.replaceAll("_", " ")}</dt>
                      <dd className="mt-1 break-words text-slate-300"><span className="font-medium">Current:</span> {change.from}</dd>
                      <dd className="break-words text-slate-100"><span className="font-medium">Proposed:</span> {change.to}</dd>
                    </div>
                  ))}
                </dl>
                <ul className="list-disc space-y-2 pl-5 text-sm leading-6 text-amber-200">
                  {proposal.tradeoffs.map((tradeoff) => <li key={tradeoff}>{tradeoff}</li>)}
                </ul>
                {!!proposal.missingFields.length && <p className="text-sm leading-6 text-slate-300">Still missing: {proposal.missingFields.map((field) => field.replaceAll("_", " ")).join(", ")}.</p>}
                {proposal.plan && (
                  <details className="rounded-lg border border-white/15 px-4">
                    <summary className="min-h-11 cursor-pointer py-3 text-sm text-sky-300 focus-visible:outline-2 focus-visible:outline-offset-4 focus-visible:outline-sky-300">Review proposed action plan</summary>
                    <ol className="mb-4 list-decimal space-y-4 pl-5 text-sm leading-6 text-slate-300">
                      {proposal.plan.phases.map((phase) => (
                        <li key={phase.phaseKey}>
                          <p className="font-semibold text-slate-100">{phase.phaseName} · weeks {phase.startWeek}–{phase.endWeek}</p>
                          {phase.tasks.map((task) => <p key={task.taskId} className="mt-2"><span className="font-medium">{task.taskName}:</span> {task.description} Success criteria: {task.successCriteria}</p>)}
                        </li>
                      ))}
                    </ol>
                  </details>
                )}
              </>
            ) : (
              <p className="text-sm leading-6 text-slate-300">{!proposal ? "No improvement proposal is saved for this version. Create a revised version to evaluate changes." : proposal.status === "needs_information" ? "More requirements are needed before these rules can support an improvement. Review the missing fields and revise your input." : "The tested changes did not produce a supported score improvement. Review the recommendations and try a specific revised scenario."}</p>
            )}
          </CardContent>
        </Card>
      </div>
    </section>
  )
}
