# Decision Replay

Decision Replay is a constraint-based decision engine. A language model is used only to
translate natural-language input into structured fields and to explain immutable backend
results. Validation, feasibility scoring, risk classification, recommendations, replay
comparison, planning, persistence, and exports are owned by deterministic .NET services.

## Decision Flow

```text
Natural-language decision
  -> Groq field extraction
  -> schema and domain validation
  -> deterministic weighted scoring
  -> deterministic risk and recommendation rules
  -> backend action-plan skeleton
  -> optional Groq wording enhancement
  -> version storage, replay comparison, and audit trail
```

Groq cannot provide or change feasibility scores, risk levels, recommendations, dates, or
plan structure.

## Stack

- ASP.NET Core 8, C#, MongoDB, JWT
- Next.js 16, React 19, TypeScript, Tailwind CSS
- Groq through the provider-neutral `IAiLanguageService`
- xUnit for deterministic scoring, replay, and planning tests

## Backend Services

- `DecisionParserService`
- `DecisionValidationService`
- `DomainTemplateService`
- `FeasibilityScoringService`
- `ReplayComparisonService`
- `ExplanationService`
- `AuditTrailService`
- `ActionPlanService`
- `PlanPhaseBuilderService`
- `PlanReplayComparisonService`
- `PdfExportService`
- `ExcelExportService`

Initial domain templates:

- `business_startup`
- `career_decision`
- `education_path`
- `product_launch`
- `project_planning`

## Configuration

Copy `backend/.env.example` to `backend/.env` for local development.

The only AI-provider settings are:

```env
GROQ_API_KEY=
GROQ_MODEL=llama-3.3-70b-versatile
GROQ_BASE_URL=https://api.groq.com/openai/v1
```

MongoDB and JWT settings are also required to run the API. The engine has a deterministic
language-extraction fallback when Groq is unavailable, but production extraction quality is
best with a valid key.

For an isolated local smoke test without MongoDB, Development supports
`DECISION_REPLAY_USE_IN_MEMORY=true`. This storage is process-local and must not be enabled in
production.

Set the frontend API URL in `frontend/.env.local`:

```env
NEXT_PUBLIC_API_URL=http://localhost:5000/api
```

## Run

```powershell
cd backend
dotnet build DecisionReplay.sln
dotnet test DecisionReplay.sln

cd DecisionReplay.API
dotnet run
```

```powershell
cd frontend
npm install
npm run dev
```

Swagger is available at `http://localhost:5000/swagger` in Development.

## Decision API

All decision endpoints except domain metadata require a JWT.

| Method | Endpoint | Purpose |
| --- | --- | --- |
| `POST` | `/api/v2/decisions` | Analyze and store a new decision |
| `GET` | `/api/v2/decisions` | List current decision summaries |
| `GET` | `/api/v2/decisions/{id}` | Get the current version and audit trail |
| `POST` | `/api/v2/decisions/{id}/replay` | Create and compare a new version |
| `GET` | `/api/v2/decisions/{id}/versions` | Get all stored versions |
| `POST` | `/api/v2/decisions/{id}/plans/generate` | Regenerate the current plan |
| `GET` | `/api/v2/decisions/{id}/plans/{planId}` | Get a stored plan |
| `POST` | `/api/v2/decisions/{id}/plans/{planId}/replay` | Replay from a plan |
| `GET` | `/api/v2/decisions/{id}/plans/{planId}/export/pdf` | Export PDF |
| `GET` | `/api/v2/decisions/{id}/plans/{planId}/export/excel` | Export XLSX |
| `GET` | `/api/v2/decisions/domains` | Inspect domain templates |

## Frontend Routes

- `/decisions/new`: natural-language decision input
- `/decisions`: decision register
- `/decisions/[id]/analytics`: score evidence, risks, assumptions, plan, exports, audit
- `/decisions/[id]/analysis`: version replay and delta comparison
- `/analytics`: portfolio-level deterministic metrics

## Verification

```powershell
cd backend
dotnet build DecisionReplay.sln --no-restore
dotnet test DecisionReplay.sln --no-restore

cd ../frontend
npm run build
```
