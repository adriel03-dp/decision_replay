# Implementation report — 2026-09-17

## Existing architecture discovered

Audit preceded changes. Next.js App Router / React / Tailwind / Radix frontend calls a JWT-protected .NET 8 API. Backend projects separate Domain, Application, Infrastructure and API; Mongo stores decisions with embedded versions and users. Domain templates, weighted scoring, replay deltas, recommendations, plans and PDF/Excel exports are deterministic. The existing feasibility `Outcome` enum was not a record of eventual real-world outcomes. Groq extraction was behind `IAiLanguageService`, but provider HTTP/prompt/parsing code was duplicated and embedded in broad language services. Prior Ollama additions did not supply a generic inference/evaluation boundary. See [audit](repository-audit.md).

## Architecture and provider changes

Added a generic `IAiProvider`, provider resolver, embedded prompt/dataset catalogs and an owner-scoped AI-run repository contract. `AiWorkflowService` owns prompt construction, bounded inference/retries, strict parsing, validation, tracing and persistence. Groq and Ollama HTTP providers are isolated in Infrastructure. `AiLanguageService` adapts the existing workflow to this shared pipeline; removed duplicated Groq/Ollama/Hybrid language implementations. Default hybrid routing preserves Groq extraction and uses Ollama elsewhere. Fully local mode needs no API key. No RAG, agent framework or extra Ollama service was introduced.

## Files added and modified

The complete working-tree file manifest is [changed-files.txt](changed-files.txt). Principal groups:

- Domain: AI contracts/executions, historical context/outcomes/scenarios, golden cases/experiments; optional additions to existing decision/version models.
- Application: AI interfaces, validated workflow/compatibility adapter, output validators/schema, T0 projection, historical replay/scenario service, evaluation/scoring.
- Infrastructure: provider configuration/HTTP clients/registration, immutable embedded prompt manifests and dataset, Mongo/in-memory AI repositories; optimistic revision handling in decision repositories.
- API: provider registration and CLI checks/evaluation, safe health information, AI error mapping/rate limiting, authenticated historical/AI controllers.
- Tests: AI infrastructure, output schema, historical replay and evaluation tests; prior Ollama-only tests replaced with broader protocol/workflow coverage.
- Frontend: Historical Replay page, T0 context editor, typed advisory-analysis component, AI Lab/diagnostic view, API contracts, navigation/report links. Added ESLint/typecheck and fixed uncovered hydration/subscription/purity lint issues. Updated vulnerable frontend dependencies with a matching lockfile.
- Operations: root Compose/config example, frontend standalone Dockerfile/ignore/config example; existing backend image continues to publish project dependencies only. README, audit, this report and real evaluation artifacts.

## Database and schema changes

Existing decisions gain optional version context and AI analyses, separate append-only recorded outcomes, copied replay scenarios and optimistic revision. Mongo adds only `ai_executions` and `ai_experiments`. Prompt/dataset versions are immutable embedded source artifacts, not mutable Mongo collections. Legacy documents without the added fields/revision remain supported. No destructive migration, model file or existing sovereign-dev volume changes. Mongo persistence round trips and migration behavior have not yet been tested against a live database; memory repositories use copies and concurrency checks. Histories have count limits, but production still requires archival and size/retention policies.

## Ollama setup and Docker

Use the existing installed `qwen2.5-coder:3b` model. Windows uses `OLLAMA_BASE_URL=http://localhost:11434`; Docker Desktop uses `http://host.docker.internal:11434`. Configuration examples contain both modes, request deadlines and bounded retry settings. Exact model availability is checked through `/api/tags`, then `/api/chat` returns `message.content`. No model download or separate server installation occurs.

Compose manages only Decision Replay's frontend, API and Mongo (own Mongo volume). It does not own the sovereign-dev Ollama container or model volume. Frontend port defaults to 3001. Read-only Compose validation passed. The final backend Docker image built successfully. A temporary Decision Replay check container reached the existing model through Docker Desktop and completed validated summary inference in 62.60 seconds with zero retries (180-second deadline, 256-token limit). Its output is preserved in `artifacts/evaluations/docker-ollama-connectivity.txt`. The check container removed itself; the sovereign-dev runtime remained healthy. Frontend image build and full Compose startup were not performed.

## Prompt versioning and structured outputs

Embedded operation/version manifests are resolved and hashed. Every execution records its prompt/input/schema hashes and parameters. Analysis v1 and v2 remain available; v3 adds compact bounds and an evidence-ID-specific JSON Schema for Ollama. Historical analysis and AI Lab default to v3. A separate versioned repair prompt records its hash per retry. Output is strict JSON with required fields and semantic checks for valid references, bounds and unsupported non-hypothesis claims. Malformed responses never become historical analysis records.

Ollama's schema enforcement is a capability difference from Groq JSON-object mode. It is recorded per attempt. Valid citations establish identifier integrity only; a model may cite an unrelated supplied fact. V1/v2 artifacts were generated before repair-prompt extraction; their primary prompt hashes and raw attempts are retained unchanged.

## Replay and hindsight protection

Initial inference receives an explicit T0 allowlist, not the full decision aggregate. Actual outcomes and prior AI results are excluded. User-entered statement timestamps cannot exceed T0. An explicit outcome-reflection stage requires completed original T0 analysis before revealing T1, and rejects hypothetical scenario/outcome pairing. Tests use outcome sentinels to verify the separation.

Context editing and plan regeneration append versions. Scenarios copy a named version and record modified variables; original data is preserved. Numeric changes are assessed by the deterministic engine; narrative changes influence advisory reasoning only. Legacy snapshots are labelled recorded-time context. Users can misstate historical information and pretrained models may already know subsequent events: this design cannot guarantee epistemic isolation inside the model.

## Evaluation framework and benchmarking

Three manually prepared synthetic cases cover startup demand, career transition and software delivery. Evaluation records actual typed validity, expected keyword-group coverage, citation integrity, latency and failure rate. Coverage is a proxy, not a semantic-quality certification. Failed outputs produce explicit failed case results and contribute zeros to validity/coverage. Model confidence is not calibrated. Human review remains separately null. No LLM judge was added without a human calibration set.

Cases and provider/model targets execute sequentially; concurrent experiments fail clearly. Benchmark fallback is disabled. Comparisons require matching dataset/prompt hashes, generation parameters, request/retry budgets and output contract. New executions/experiments snapshot timeout and retry budgets; earlier checked-in artifacts predate these optional fields and are unchanged. Experiments retain target, dataset version/hash, prompt version/hash, timestamps, case outputs/metrics and execution IDs. Token counts are actual when supplied. Costs remain unknown/null; no claim of universally free local compute.

## Actual local evaluation results

All runs used the existing local model, temperature 0.1, 2048 maximum output tokens, 120-second per-attempt timeout and one retry. Timings below are measured case pipeline averages including retries, not single model-call averages.

| Prompt | Valid cases | Failure rate | Mean case latency | Final failures |
| --- | --- | --- | --- | --- |
| v1 | 0/3 | 100% | 99.23 seconds | Schema validation for all cases |
| v2 | 0/3 | 100% | 183.47 seconds | Schema validation for all cases; some attempts also timed out |
| v3 / constrained schema | 1/3 | 66.67% | 184.42 seconds | Timeout for cases 001 and 003 |

V3 case 002 validated in 70.92 seconds. Its keyword-group missing-information coverage was 0.5, risk coverage 0 and alternative coverage 1. These are computed against the prepared expectations; legal citation IDs do not imply supported conclusions. This result illustrates why schema validation and analysis quality must remain separate.

Raw synthetic outputs, prompt hashes, attempt failures and case metrics are preserved under `artifacts/evaluations`. V3 aggregate structured validity is 1/3, missing-information coverage 1/6, risk coverage 0 and alternative coverage 1/3. The runs are diagnostic observations on three cases, not a statistical ranking. No live cloud benchmark or invented quality numbers are included. Ollama was CPU-only in the observed runtime; an instantaneous sample showed about 2.3 GiB RAM and approximately 458% CPU, not an automatically tracked benchmark resource metric.

## Observability

Executions retain request/decision/context/scenario/experiment identifiers, provider/model and local model digest where returned, prompt/repair/schema hashes, timestamps, latency, construction/inference/validation timings, retries/fallback, actual usage, bounded raw output, validation errors and final status. Owner-protected API/UI exposes details. Logs omit keys and prompt contents. .NET `DecisionReplay.AI` activities and metrics are available, but no external telemetry exporter is configured. Stored output may contain sensitive decision information and needs retention governance before production.

## Tests and verification

- Backend `dotnet test DecisionReplay.sln --no-restore`: **43 passed, 0 failed**, including existing deterministic tests.
- Backend release API build: **passed, 0 warnings, 0 errors**.
- Frontend ESLint: **passed** after fixes (0 errors, 0 warnings in final lint run).
- Frontend typecheck: **passed** on the final code.
- Frontend production build: passed, including `/ai-lab` and `/decisions/[id]/history`.
- Docker Compose configuration validation: passed.
- Backend Docker image build and actual Docker-to-Ollama availability/validated inference: **passed**.
- Final npm production-dependency audit: **0 vulnerabilities**.
- Git diff whitespace check: passed.
- Windows backend-to-Ollama: real availability/inference and three evaluation runs performed; v3 includes actual output validated through the shared pipeline.
- Mock HTTP transport tests exercise both Ollama and Groq envelopes, credentials, usage, missing model and structured-output handling; they are not claims of live cloud connectivity.

Tests cover provider selection/configuration, prompt resolution/hashes, strict/invalid output, schema construction, bounded retries, explicit fallback, provider timeout versus caller cancellation, deterministic compatibility fallback, future-context rejection, T0/T1 separation, scenario immutability, historical plan preservation, owner isolation, stale writes, dataset/scoring and experiment persistence. Full-stack authenticated browser flow, live Mongo round trips and cloud inference remain unverified.

## Known limitations and next steps

Prioritize a human-reviewed dataset with semantic groundedness/consistency judgments, measured CPU inference budgets, reproducible matched-provider runs and larger repeated samples. Add live Mongo/migration and authenticated browser integration tests, indexes/archival/retention, account-data cleanup policy and telemetry export. Existing JWT localStorage handling and deployment security need review before production use. Do not weaken evidence validation just to improve reported validity, and do not add RAG or agent frameworks without a domain need.

The application provides real instrumentation and exposes failures; it does not claim that the installed 3B model reliably supplies useful historical analysis under the current deadline.
