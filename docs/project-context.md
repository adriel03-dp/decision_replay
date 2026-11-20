# Decision Replay: project refresher and rules-first rework

Updated November 20, 2026.

## What the project does

A person describes a decision or project, reviews the extracted requirements, receives a deterministic feasibility assessment and phased action plan, then revises inputs to compare versions. Historical replay also records what was known at decision time separately from actual later outcomes, helping distinguish decision quality from luck and hindsight.

Your remembered example fits the project: build a mobile application in six months, with six people and USD 100,000. Those three numbers alone cannot establish whether the product will succeed. Scope, delivery cost, technical readiness, dependencies and evidence are still important. The system must expose those gaps rather than invent answers.

Supported templates currently cover project planning, product launches, startups, career decisions and education paths. The project includes authentication, a decision register, reports, replay comparisons, an audit trail, historical context, what-if scenarios, actual outcomes, PDF/Excel exports and an optional AI evaluation lab.

## Stack and provider configuration found before this rework

| Area | Implementation |
| --- | --- |
| Frontend | Next.js 16 / React 19 / TypeScript / Tailwind / shadcn components |
| Backend | ASP.NET Core targeting .NET 8; Domain, Application, Infrastructure and API layers |
| Database | MongoDB; development-only in-memory decision and AI execution repositories are also available |
| Existing scoring | Weighted template factors, missing-information penalties, thresholds and domain risk rules |
| Cloud AI | **Groq**, not xAI Grok; private backend configuration contains a key, whose value is never printed or committed |
| Configured Groq model | `openai/gpt-oss-20b` |
| Local inference | Ollama with **`qwen2.5-coder:3b`**, not currently a LLaMA model |
| Native Ollama URL | `http://localhost:11435`; Compose uses `http://ollama:11434` |
| Previous default | `hybrid`: Groq extraction and Ollama for explicit advisory analysis; optional interactive wording disabled |

These settings were inspected from code and safe allowlisted environment settings. They establish configuration, not current cloud model availability or inference quality. No cloud inference was sent during this rework. The checked-in earlier evaluation artifacts show weak local reliability: v3 validated one of three synthetic cases, with the other two timing out. Those results are diagnostic, not a model ranking.

## Changes on `codex/rules-first-feasibility`

1. **No AI required for the main workflow.** `AI_PROVIDER=none` is the new default. Core creation, replay, scoring, explanations and plans use local rules. Optional AI endpoints return `ai_disabled`, even when an explicit cloud evaluation target is supplied. MongoDB/auth configuration is still needed for the full application.
2. **Explicit requirement parsing.** Rules recognize numeric and simple English quantities (zero through twenty), budgets including `k`/`m`, month durations, team size and labelled template fields. They do not attempt general language understanding. Unknown scope, readiness and business evidence remain missing or conservative. Review the report's structured fields before relying on an assessment.
3. **Structured entry.** `POST /api/v2/decisions/analyze-structured` accepts known fields directly and never invokes AI extraction. Existing historical context editing supplies structured revisions. Invalid fields are excluded from scoring; missing-field claims from a model cannot override template validation.
4. **Visible delivery economics.** Project/startup/product assessments expose monthly burn, estimated delivery cost including contingency, funding gap and gross runway. Monthly rates, currency and contingency can be supplied explicitly. Otherwise the estimate records its assumptions. Non-USD input requires a user-supplied monthly rate; the system does not guess exchange rates.
5. **Recalculated improvement proposals.** A bounded rules search evaluates single changes: additional funding only when budget is explicitly flexible, deferred scope unless scope is fixed, or an extended timeline only when permitted. Budget and timeline are fixed by default. Each candidate uses the same validation and scoring functions as the original, and must satisfy its fixed cost/effort limits before a higher score can qualify. A selected proposal includes before/after fields, tradeoffs, constraint checks, score delta and a separate phased plan. It is hypothetical and does not overwrite the original. It never upgrades market evidence or technical readiness by assertion.
6. **Versioned rules.** New assessments record rules version `2.0`. Previous persisted assessments remain unchanged; changes to historical context clear stale improvement proposals. Replay explicitly discloses when scores use different rules, so a methodology change is not presented as an input improvement. A score is a heuristic feasibility index out of 100, not a calibrated success probability. Factor confidence describes field coverage, not empirical statistical confidence.
7. **Optional local runtime.** Ollama now belongs to Compose's `ai` profile. Default startup does not require the external model volume. Enable the profile only when deliberately using local AI.

## AI-free example

The existing text submission accepts:

```text
Build a mobile application within six months with a team of six and a budget of 100,000 USD;
scope: login, payments, reporting;
technical_readiness: partial;
monthly_cost_per_person: 2000;
contingency_percent: 20
```

Or submit this authenticated JSON body to `POST /api/v2/decisions/analyze-structured`:

```json
{
  "domain": "project_planning",
  "title": "Mobile application",
  "goal": "Deliver a mobile application within six months",
  "fields": {
    "project_goal": "Deliver a mobile application",
    "budget": "100000",
    "currency": "USD",
    "timeline_months": "6",
    "team_size": "6",
    "scope": "login, payments, reporting",
    "technical_readiness": "partial",
    "monthly_cost_per_person": "2000",
    "contingency_percent": "20"
  }
}
```

The illustrative user-supplied rate produces monthly burn of USD 13,440 and a six-month cost estimate of USD 96,768 including contingency. This uses a 1.12 scope multiplier. Neither the rate nor the feature-effort assumption is a verified quote. Revenue, ROI, demand and actual product success are not estimated from these inputs.

## Limits and next product work

The cost and effort formulas remain heuristics. Feature counts do not capture actual complexity, salaries vary, and lengthening a schedule can increase cost. The proposal search compares a few inspectable possibilities; it is not a global optimizer. Extra funding or a later deadline requires explicit flexibility. Scope deferral remains a hypothetical tradeoff for review unless scope is locked, in which case it is prohibited. Passing estimated limits is not proof that the project will succeed. Original constraints and evidence remain in the record.

More defensible predictions need real project outcomes, validated cost/effort benchmarks, richer constraints and calibration. Useful next work includes confirmed requirement forms, user-selected priorities, project-type benchmarks, operating/revenue models where inputs exist, and human-reviewed evaluation cases. No machine learning prediction model has been trained here.

## Saved existing work

The 19 previously uncommitted files were preserved on `refactor-2` in groups of 7, 6 and 6. Both author and committer timestamps were set at the user's request to August 20, 21 and 22, 2026, at 12:00 Sri Lanka time. These are intentionally assigned commit dates; they are not evidence that the work was performed on those dates. Existing September parent commits and dates within historical documents were retained.

| Assigned date | Commit | Scope |
| --- | --- | --- |
| August 20, 2026 | `514243d` | Extraction validation and optional interactive wording |
| August 21, 2026 | `c37d7d1` | Frontend navigation/loading and local startup |
| August 22, 2026 | `d2b0f7b` | Runtime isolation, configuration and performance notes |

The rework branch was created after those commits. At the user's request, its 33 changed files were split into 17 commits with exactly two changed paths each, using daily assigned author and committer timestamps from August 23 through September 8, 2026, at 12:00 Sri Lanka time. This context document was added first and completed in the final commit to accommodate the odd number of files. These are assigned history dates; the rework was performed on November 20, 2026. The rules-first work remains separate from the August 20–22 fixes requested for main.

## Verification

- Backend: all 71 tests passed, including 24 rules-first test cases. These include a Mongo BSON round-trip for the saved economics/proposal and a legacy-rule replay check. The API build passed with no warnings or errors.
- Authenticated HTTP smoke checks used a temporary API and synthetic in-memory decisions. They covered text and structured creation, original-version preservation, replay score changes, reproducible proposed scores, disabled AI (503), unknown-field rejection (400), and another owner's inaccessible decision (404). Core requests created zero AI executions. No real accounts or stored decisions were changed.
- The earlier unconstrained default-rate smoke sample scored 76.0 versus 95.1 by increasing funding. The fixed-limit guard now rejects that proposal under the default USD 100,000 budget. These indices are not observed business-success rates.
- Frontend lint, TypeScript checking and the production Webpack build passed. On this Windows host, Turbopack failed to launch its CSS evaluation worker both inside and outside the sandbox. Development/build scripts now select Next.js's supported Webpack mode instead.
- A browser check rendered the real report component with synthetic API data and the production stylesheet. The cost assumptions and proposed action-plan disclosures expanded correctly. Observed narrow and wide document widths were 417px and 1405px, respectively, with no horizontal overflow. This targeted component check is not a claim of full authenticated browser coverage.
- Compose configuration lists only MongoDB, backend and frontend by default, with `AI_PROVIDER=none` and no backend dependency on Ollama. Full Docker/Mongo integration and new live model benchmarks were not run.

## Fixed-budget and deadline follow-up

After the user pushed both branches, work resumed on the rules-first branch. Budget and timeline are now locked by default for project, startup and product proposals. The engine checks the cost estimate including contingency against the original budget, and the shared effort estimate against the chosen timeline. It refuses to publish an improved score/plan when tested candidates still violate a fixed limit. Unknown costs, scope or team data produce an information gap rather than a false claim that the limits passed.

The creation form has labelled budget/timeline controls, both checked by default, plus an optional scope lock. Unchecking a limit explicitly permits hypothetical changes; original submitted amounts and versions remain intact. For API callers:

```json
{
  "input": "Build an app in six months with a team of six and budget 100000 USD; scope: login, payments, reporting",
  "fixedFields": ["budget", "timeline_months", "scope"]
}
```

`fixedFields: []` explicitly allows all changes. Structured fields/context use `fixed_fields: "budget, timeline_months"`, or `none` to allow flexibility. Unsupported or misspelled field names fail validation. Request metadata overrides text labels; AI extraction cannot change constraint policy. Replay inherits saved locks unless the owner deliberately overrides them. Constraints affect proposal eligibility, not the original feasibility score.

Proposal responses include a versioned `constraintCheck`, the fixed field names, budget/timeline ceilings, a calendar deadline where known, and reasons when limits cannot be met. Legacy saved proposals without a constraint check remain stored, but the report requests regeneration before presenting an improved score. Regeneration creates a new version, reevaluates legacy scoring under the current rules and preserves the previous calendar start when the timeline is fixed. Replay also preserves that start. Whole months use calendar arithmetic; fractional months retain a documented average-day assumption. Plan end dates are inclusive, and phase/milestone dates cannot overrun them.

Authenticated synthetic HTTP checks rejected the original over-budget example, accepted a scope reduction costing USD 93,312 within the USD 100,000 budget and six-month limit, preserved the calendar deadline through replay/regeneration, allowed increased funding only with explicit flexibility, rejected a misspelled lock with 400, and created zero AI executions. Browser checks used the real form/report components with synthetic fixtures and production CSS; both default controls were checked, the over-budget state showed no improved score, and the affordable state displayed its applied limits and deferred scope. This remains targeted browser coverage rather than a full authenticated browser flow.

The follow-up uses the requested two-file commit pattern, with daily assigned author/committer dates continuing September 9–18, 2026. These dates organize history; the work was performed on November 20, 2026. The published `main` merge remains separate from this follow-up.

Follow-up verification: all 97 backend tests passed (26 additional constraint/calendar cases), API build passed with zero warnings/errors, and frontend lint, typecheck and production build passed. The calendar cases include February, leap years, short durations, preserved historical start dates, and milestone/phase bounds. API checks used synthetic in-memory records; no live cloud inference or real Mongo records were involved.
