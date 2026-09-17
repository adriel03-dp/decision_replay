# Repository audit and implementation plan

Audit performed before enhancement edits, 2026-09-17.

## Existing architecture

- `frontend/app`: Next.js 16 App Router, React 19 client views; protected decision,
  analytics and settings routes. Tailwind/Radix components, Axios API client, JWT in
  localStorage via `AuthProvider`. Authentication uses direct fetch calls.
- `backend/DecisionReplay.API`: ASP.NET Core 8 controllers, JWT bearer auth,
  exception and per-user AI rate-limit middleware, Swagger, environment configuration.
- `DecisionReplay.Application`: extraction/normalization, five domain templates,
  validation, weighted scoring, risk rules, deterministic planning, explanation,
  version replay/comparison and audit services. No provider SDK dependencies here.
- `DecisionReplay.Domain`: `DecisionV2` embeds sequential `DecisionVersion` documents,
  structured fields, assumptions, missing fields, feasibility, one plan per version,
  free-form explanation and audit trail. `Outcome` is a feasibility enum, **not** an
  observed real-world outcome. No timestamped evidence, alternatives or historical
  context/outcome boundary exists yet.
- `DecisionReplay.Infrastructure`: MongoDB driver repositories (`decisions_v2`,
  `users`), bcrypt/JWT auth, PDF/XLSX export, Groq and Ollama language implementations.
  Development optionally uses an in-memory decision repository; auth still uses Mongo.
- Existing APIs: auth/register/login/profile/password/account; decision create/analyze,
  list/get/delete; replay; versions; plan generate/get/replay/export; domain metadata;
  simple and detailed health.
- `IAiLanguageService` is an existing application-facing abstraction. The hybrid
  implementation routes intake to Groq and wording to Ollama. Provider transports,
  prompt construction, parsing and fallback are mixed and repeated in concrete services.
  Extraction and task enhancement use JSON; explanations and summaries are free text.
  Important prompts are C# raw strings; executions have no persisted provenance.
- Tests: deterministic feasibility, plan timing, replay deltas, export signatures,
  Ollama transport/failure handling and hybrid routing. Latest baseline: 17 passing.
- Configuration: ignored `backend/.env`, example file, Mongo/JWT/Groq/Ollama settings;
  no cloud keys in frontend. Backend Dockerfile contains .NET dependencies only.
  Existing external `sovereign-dev-ollama-1` owns the model volume. No compose file yet.

## Weaknesses and preservation requirements

Preserve auth, domain templates, deterministic scoring/risk/recommendations, existing
REST shapes, sequential versions, action plans, exports, audit and user ownership.
Avoid reinterpreting existing feasibility values as decision quality or real outcomes.
The in-memory repository returns live references; Mongo replaces whole documents without
concurrency control. Plan regeneration mutates old versions. Prompts are duplicated,
AI failures can disappear into fallbacks, outputs lack strict semantic checks, frontend
timeout is shorter than a local multi-call workflow, and the lint script lacks ESLint.
JWT/localStorage, synchronous long-running evaluation and Mongo embedded document growth
are material limitations to document rather than obscure with new frameworks.

## Incremental plan

1. Add a generic provider contract and isolated HTTP providers, validated configuration,
   controlled retry/fallback, versioned embedded prompts and execution metadata. Keep
   `IAiLanguageService` as a compatibility adapter. Verify existing scoring/export tests.
2. Add typed validated advisory analysis, T0 snapshots, append-only T1 outcomes and
   immutable what-if scenarios. Use explicit T0 payload projection; reveal outcomes only
   in a separately named reflection stage. Add repository concurrency protection.
3. Add a small manually authored golden dataset, sequential reproducible experiments,
   structural/citation/concept proxy metrics and provider comparison. Persist executions
   and experiments in two collections; prompts/datasets remain versioned Git artifacts.
4. Extend existing UI with context/replay/outcome and experiment/execution views.
   Add compose for frontend/API/Mongo **without** managing the existing Ollama service.
5. Verify unit/integration pipelines, one sequential local dataset run, builds/type-check,
   lint availability and Docker configuration. Publish actual results and limitations.

No RAG, agent framework, dynamic tool use, or LLM judge is needed for this initial
structured-data workflow. A judge would require independent human calibration first.
