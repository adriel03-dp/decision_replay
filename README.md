# Decision Replay

> **AI-Powered Decision Intelligence Platform** — Submit any decision in natural language, receive instant feasibility analysis, identify risks, and replay decisions with updated constraints to explore "what-if" scenarios.

---

## Table of Contents

- [Overview](#overview)
- [Key Features](#key-features)
- [Tech Stack](#tech-stack)
- [Architecture](#architecture)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Backend Setup](#backend-setup)
  - [Frontend Setup](#frontend-setup)
- [Environment Variables](#environment-variables)
- [API Reference](#api-reference)
- [Domain Model](#domain-model)
- [How Decision Replay Works](#how-decision-replay-works)
- [License](#license)

---

## Overview

Decision Replay is a full-stack web application that helps individuals and teams make better decisions by leveraging AI. Users describe their decision in plain English — a project plan, budget allocation, hiring decision, or any other scenario — and the platform provides:

- A **feasibility score** (0–100) backed by structured AI reasoning
- **Pros, cons, risks, and recommendations** for both the current plan and an AI-optimized alternative
- A **Replay Engine** that lets users tweak constraints (timeline, budget, resources) and immediately see the impact on feasibility
- An **Analytics Dashboard** showing decision trends, domain distributions, and risk profiles over time
- An **Audit Trail** for tracking the history of decisions

---

## Key Features

| Feature | Description |
|---|---|
| **Natural Language Input** | Describe any decision in plain English — no rigid forms or dropdown fields required |
| **AI Feasibility Analysis** | Google Gemini AI evaluates feasibility (0–100), generates executive summaries, pros/cons, risks, and recommendations |
| **Optimised Plan** | AI proposes an improved version of your plan alongside a side-by-side comparison |
| **Decision Replay** | Change constraints and re-run analysis to see exactly what changes and why outcomes differ |
| **Domain-Agnostic** | Works for any domain: software projects, business strategy, finance, healthcare, construction, education, and more |
| **Risk Identification** | Structured risk factors with HIGH / MEDIUM / LOW impact ratings and suggested mitigations |
| **Analytics Dashboard** | Charts for monthly trends, feasibility distributions, domain breakdowns, and risk analysis by domain |
| **Audit Trail** | Full history of decisions with timestamps |
| **User Accounts** | Register, log in, update profile, change password, delete account |
| **Dark / Light Mode** | Full theme support via `next-themes` |

---

## Tech Stack

### Frontend

| Technology | Version | Purpose |
|---|---|---|
| [Next.js](https://nextjs.org/) | 16 | React framework with App Router |
| [React](https://react.dev/) | 19 | UI library |
| [TypeScript](https://www.typescriptlang.org/) | 5 | Type safety |
| [Tailwind CSS](https://tailwindcss.com/) | 4 | Utility-first styling |
| [Radix UI](https://www.radix-ui.com/) | Various | Accessible headless UI primitives |
| [Recharts](https://recharts.org/) | 2 | Data visualisation / charts |
| [Zustand](https://zustand-demo.pmnd.rs/) | 5 | Client-side state management |
| [React Hook Form](https://react-hook-form.com/) + [Zod](https://zod.dev/) | — | Form handling and validation |
| [Axios](https://axios-http.com/) | 1 | HTTP client |
| [Lucide React](https://lucide.dev/) | — | Icon library |
| [jsPDF](https://github.com/parallax/jsPDF) + html2canvas | — | PDF export |
| [Vercel Analytics](https://vercel.com/analytics) | — | Usage analytics |

### Backend

| Technology | Version | Purpose |
|---|---|---|
| [.NET / ASP.NET Core](https://dotnet.microsoft.com/) | 8+ | REST API framework |
| [MongoDB](https://www.mongodb.com/) + MongoDB.Driver | — | NoSQL persistence |
| [Google Gemini AI](https://ai.google.dev/) | — | LLM for feasibility analysis |
| [JWT Bearer](https://jwt.io/) | — | Authentication tokens |
| [Swagger / OpenAPI](https://swagger.io/) | — | API documentation (dev only) |
| [DotNetEnv](https://github.com/tonerdo/dotnet-env) | — | `.env` file loading |

---

## Architecture

Decision Replay follows **Clean Architecture** with strict separation of concerns across four .NET projects:

```
┌──────────────────────────────────────────────────────────┐
│                     Frontend (Next.js)                   │
│  Pages: home · decisions · analytics · audit · settings  │
└────────────────────────┬─────────────────────────────────┘
                         │ HTTP / REST (JSON)
┌────────────────────────▼─────────────────────────────────┐
│              DecisionReplay.API  (ASP.NET Core)          │
│  Controllers · DTOs · Middleware · Mapping               │
└────────────────────────┬─────────────────────────────────┘
                         │
┌────────────────────────▼─────────────────────────────────┐
│           DecisionReplay.Application                     │
│  Interfaces (IDecisionV2Repository, IReplayEngine, …)    │
│  Services (DecisionServiceV2, AuthService, …)            │
└──────────┬─────────────────────────┬─────────────────────┘
           │                         │
┌──────────▼──────────┐   ┌──────────▼──────────────────────┐
│  DecisionReplay     │   │  DecisionReplay.Infrastructure   │
│  .Domain            │   │  Services: GeminiIntentParser,   │
│  Entities, Enums,   │   │    GeminiReasoningServiceV2,      │
│  Value Objects      │   │    DecisionReplayEngine, …       │
└─────────────────────┘   │  Persistence: MongoContext,      │
                          │    DecisionV2Repository, …       │
                          └──────────────────────────────────┘
```

**Key design principles:**

- **SOLID** — every service, repository, and domain object is interface-driven and single-purpose
- **Domain-Agnostic** — decisions are stored as natural language + AI-inferred attributes; no hard-coded domain fields
- **Clean separation** — the Domain layer has zero infrastructure dependencies

---

## Project Structure

```
decision_replay/
├── backend/
│   ├── DecisionReplay.sln
│   ├── DecisionReplay.API/            # REST API, controllers, middleware, DTOs
│   │   ├── Controllers/
│   │   │   ├── AuthController.cs
│   │   │   ├── DecisionsV2Controller.cs
│   │   │   └── HealthController.cs
│   │   ├── DTOs/                      # Request / response models
│   │   ├── Middleware/
│   │   │   ├── GeminiRateLimitMiddleware.cs
│   │   │   └── GlobalExceptionHandler.cs
│   │   ├── Mapping/                   # Entity ↔ DTO mapping
│   │   └── Program.cs
│   ├── DecisionReplay.Application/    # Use-cases, interfaces, app services
│   │   ├── Interfaces/
│   │   └── Services/
│   ├── DecisionReplay.Domain/         # Pure domain model
│   │   ├── Entities/
│   │   │   ├── DecisionV2.cs
│   │   │   └── User.cs
│   │   ├── Enums/
│   │   │   ├── DecisionStatus.cs
│   │   │   └── DecisionOutcome.cs
│   │   └── ValueObjects/
│   │       ├── DecisionContext.cs
│   │       ├── DecisionAnalysis.cs
│   │       └── DecisionSchema.cs
│   └── DecisionReplay.Infrastructure/ # MongoDB, Gemini AI, external services
│       ├── Persistence/
│       │   ├── Mongo/
│       │   └── Repositories/
│       └── Services/
│           ├── GeminiIntentParser.cs
│           ├── GeminiReasoningServiceV2.cs
│           ├── DecisionReplayEngine.cs
│           └── AuthService.cs
└── frontend/
    ├── app/
    │   ├── page.tsx                   # Landing page
    │   ├── login/
    │   ├── signup/
    │   ├── decisions/                 # Decision list, create, detail, replay
    │   ├── analytics/                 # Analytics dashboard
    │   ├── audit/                     # Audit trail
    │   ├── settings/                  # User profile & account settings
    │   └── about/
    ├── components/                    # Shared UI components
    ├── hooks/                         # Custom React hooks
    └── lib/                           # API client, auth context, types
```

---

## Getting Started

### Prerequisites

- **Node.js** ≥ 20 and **npm**
- **.NET SDK** ≥ 8
- A running **MongoDB** instance (local or cloud, e.g. MongoDB Atlas)
- A **Google Gemini API key** (get one at <https://ai.google.dev/>)

### Backend Setup

1. **Clone the repository:**

   ```bash
   git clone https://github.com/adriel03-dp/decision_replay.git
   cd decision_replay/backend
   ```

2. **Create a `.env` file** inside the `backend/` directory (see [Environment Variables](#environment-variables)):

   ```bash
   cp .env.example .env   # or create manually
   ```

3. **Restore dependencies and run:**

   ```bash
   dotnet restore DecisionReplay.sln
   dotnet run --project DecisionReplay.API
   ```

   The API will start on `https://localhost:7xxx` (HTTPS) or `http://localhost:5xxx` (HTTP).  
   Swagger UI is available at `http://localhost:{port}/swagger` when running in Development mode.

### Frontend Setup

1. **Navigate to the frontend directory:**

   ```bash
   cd decision_replay/frontend
   ```

2. **Install dependencies:**

   ```bash
   npm install
   ```

3. **Create a `.env.local` file:**

   ```bash
   NEXT_PUBLIC_API_URL=http://localhost:5000/api
   ```

4. **Start the development server:**

   ```bash
   npm run dev
   ```

   The app will be available at `http://localhost:3000`.

---

## Environment Variables

### Backend — `backend/.env`

| Variable | Required | Description |
|---|---|---|
| `MONGODB_CONNECTION_STRING` | ✅ | MongoDB connection string (e.g. `mongodb://localhost:27017`) |
| `MONGODB_DATABASE_NAME` | ✅ | Database name (default: `DecisionReplay`) |
| `JWT_SECRET` | ✅ | Secret key for signing JWT tokens (min. 32 characters) |
| `JWT_ISSUER` | ❌ | JWT issuer claim (default: `DecisionReplay`) |
| `GEMINI_API_KEY` | ✅ | Google Gemini API key |

**Example:**

```env
MONGODB_CONNECTION_STRING=mongodb://localhost:27017
MONGODB_DATABASE_NAME=DecisionReplay
JWT_SECRET=your-super-secret-key-at-least-32-chars
JWT_ISSUER=DecisionReplay
GEMINI_API_KEY=AIza...
```

### Frontend — `frontend/.env.local`

| Variable | Required | Description |
|---|---|---|
| `NEXT_PUBLIC_API_URL` | ✅ | Base URL of the backend API (without trailing slash) |

---

## API Reference

All endpoints are prefixed with `/api`. Authentication endpoints are open; all others require a JWT Bearer token in the `Authorization` header.

### Authentication — `/api/auth`

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| `POST` | `/api/auth/register` | No | Create a new account |
| `POST` | `/api/auth/login` | No | Log in, receive JWT token |
| `PUT` | `/api/auth/profile` | Yes | Update display name |
| `PUT` | `/api/auth/password` | Yes | Change password |
| `DELETE` | `/api/auth/account` | Yes | Delete account |

### Decisions (V2) — `/api/v2/decisions`

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| `GET` | `/api/v2/decisions` | Yes | List all decisions for the current user |
| `POST` | `/api/v2/decisions` | Yes | Create a decision from natural language input |
| `GET` | `/api/v2/decisions/{id}` | Yes | Get a single decision with full analysis |
| `PUT` | `/api/v2/decisions/{id}` | Yes | Update decision input or status |
| `DELETE` | `/api/v2/decisions/{id}` | Yes | Delete a decision |
| `POST` | `/api/v2/decisions/{id}/replay` | Yes | Replay a decision with updated input |

#### Create Decision — Request Body

```json
{
  "input": "I want to launch a mobile app in 3 months with a team of 2 developers and a budget of $20,000",
  "createdBy": "user@example.com",
  "analyzeNow": true
}
```

#### Create Decision — Response

```json
{
  "id": "3fa85f64-...",
  "naturalLanguageInput": "I want to launch a mobile app...",
  "inferredAttributes": { "timeline": "3 months", "budget": 20000 },
  "domainType": "Software Development",
  "status": "Analyzed",
  "outcome": "RiskyButPossible",
  "createdAt": "2026-03-01T06:00:00Z",
  "analysis": {
    "feasibilityScore": 55,
    "feasibilityVerdict": "RISKY_BUT_POSSIBLE",
    "executiveSummary": "...",
    "pros": ["..."],
    "cons": ["..."],
    "risks": [{ "description": "...", "impact": "HIGH", "mitigation": "..." }],
    "recommendations": ["..."],
    "confidenceLevel": 0.82
  }
}
```

#### Replay Decision — Request Body

```json
{
  "updatedInput": "I want to launch a mobile app in 5 months with a team of 3 developers and a budget of $35,000"
}
```

### Health — `/api/health`

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| `GET` | `/api/health` | No | Service health check |

---

## Domain Model

### `DecisionV2`

The core aggregate root. All decisions are stored in a domain-agnostic format.

| Field | Type | Description |
|---|---|---|
| `Id` | `Guid` | Unique identifier |
| `Context` | `DecisionContext` | Natural language input + AI-inferred attributes |
| `Schema` | `DecisionSchema?` | Dynamically generated field schema |
| `Analysis` | `DecisionAnalysis?` | AI analysis result |
| `Status` | `DecisionStatus` | `Draft` → `Analyzed` → `Finalized` |
| `Outcome` | `DecisionOutcome` | `Draft` / `Feasible` / `RiskyButPossible` / `NeedsAdjustment` / `Committed` |
| `DomainType` | `string?` | AI-detected domain (e.g. `Software Development`) |
| `CreatedBy` | `string` | User identifier |
| `CreatedAt` | `DateTime` | Creation timestamp |

### `DecisionAnalysis`

| Field | Type | Description |
|---|---|---|
| `FeasibilityScore` | `double` | 0–100 feasibility rating |
| `FeasibilityVerdict` | `string` | `FEASIBLE` / `RISKY_BUT_POSSIBLE` / `NEEDS_ADJUSTMENT` / `NOT_FEASIBLE` |
| `ExecutiveSummary` | `string` | High-level summary |
| `CurrentPlanAnalysis` | object | Timeline, scope, budget, and resource assessments |
| `OptimizedSolution` | object | AI-recommended improved plan |
| `Pros` / `Cons` | `List<string>` | Advantages and drawbacks |
| `Risks` | `List<RiskFactor>` | Identified risks with impact and mitigations |
| `Recommendations` | `List<string>` | Actionable suggestions |
| `ConfidenceLevel` | `double` | 0.0–1.0 AI confidence rating |

---

## How Decision Replay Works

```
User submits natural language input
           │
           ▼
  GeminiIntentParser
  (extracts structured attributes from text)
           │
           ▼
  DecisionContext created
  (stores raw input + inferred attributes)
           │
           ▼
  GeminiReasoningServiceV2
  (generates full feasibility analysis via Gemini AI)
           │
           ▼
  DecisionAnalysis stored
  (feasibility score, risks, pros/cons, optimized plan)
           │
     User updates constraints
           │
           ▼
  DecisionReplayEngine.ReplayDecisionAsync()
  ├─ Compares original vs updated DecisionContext
  ├─ Identifies changed fields (ContextChange list)
  ├─ Runs new analysis on updated context
  └─ Returns DecisionReplayResult (optimized plan comparison)
         ├─ FeasibilityDelta  (score difference)
         ├─ HasSignificantChanges
         ├─ ImpactSummary
         └─ VisualizationData (charts)
```

Feasibility score thresholds:

| Score | Outcome |
|---|---|
| ≥ 70 | `Feasible` |
| 50–69 | `RiskyButPossible` |
| < 50 | `NeedsAdjustment` |

---

## License

This project is licensed under the terms of the [LICENSE](LICENSE) file included in this repository.

