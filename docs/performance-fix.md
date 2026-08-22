# Performance and startup fixes — 2026-09-18

User report: navigation and page responsiveness were slow, with no API errors visible in the screenshots. The displayed `?_rsc=` payloads are normal Next.js page data.

## Root causes and changes

- Port 3000 belongs to another local Docker frontend and 3001 to Langfuse. Decision Replay now defaults to its own port **3002** in npm scripts, Compose and configuration examples; the API permits this origin. The API remains on 5000. No external project's container was stopped or changed.
- Decision creation waited for two optional CPU-bound Ollama wording calls before persistence; text replay could add another. `AI_INTERACTIVE_WORDING=false` now returns the existing deterministic explanations/tasks without these calls. Groq extraction remains active. Explicit historical analysis/reflection/evaluation are still model-backed. `true` restores synchronous wording if deliberately wanted.
- The account's actual Groq model list did not include the previous model; extraction returned 404. The private backend environment and example now select the available `openai/gpt-oss-20b`. Groq model configuration is explicit rather than a stale code default. No API key was exposed or replaced.
- Groq returned valid JSON with numeric field scalars, while extraction assumed all values were strings. Extraction v2 validates a string-or-number scalar wire contract and normalizes numbers before business use; object/array/null/boolean values remain rejected. The v1 prompt is preserved.
- The desktop navigation contained too many controls to fit comfortably at tablet widths. It now uses the mobile menu below 1280px, bounds usernames and labels the menu toggle.
- Added a route loading boundary for immediate navigation feedback. `npm run preview` builds and starts production on 3002, avoiding first-visit development compilation during manual E2E.
- Native production uses standard Next output; Docker explicitly opts into standalone output. This avoids the standalone/`next start` mismatch.

## Actual verification

Backend: **47 tests passed**, including numeric/string extraction equivalence, nested-value rejection and optional-local-wording isolation. Frontend lint, typecheck and production build all passed after the final output-mode adjustment.

Live authenticated API/Mongo checks used a synthetic account and synthetic decisions. T0 context appended a version, reduced-budget scenario preserved the original budget (35000 versus simulated 15000), actual outcome persisted, and premature outcome reflection returned 400 as intended. Earlier diagnostic creates used deterministic extraction fallback; they are not claimed as successful Groq inference.

After extraction correction, real Groq extraction succeeded with `openai/gpt-oss-20b`. Decision creation/persistence measured **2457 ms**, including a **1959.85 ms** extraction execution. It launched no optional local wording calls. These are one observed sample, not statistical guarantees.

| Production page GET | Server response |
| --- | --- |
| /decisions | 64 ms |
| /decisions-dashboard | 14 ms |
| /analytics | 13 ms |
| /ai-lab | 13 ms |

These are HTTP response measurements, not browser rendering or Core Web Vitals. Authenticated browser sign-in, decision listing and AI Lab navigation were exercised. Mobile AI Lab at viewport 390px had content width 375px, so no document horizontal overflow was observed in that view. Further mobile/tablet results are appended when complete.

## Dedicated Ollama runtime

The user authorized a separate runtime on September 18. Compose now owns decision_replay-ollama-1 on host port 11435, using the already-present Ollama image. The installed model volume is mounted read-only at /installed-models; OLLAMA_MODELS points to its models directory. A separate Decision Replay volume holds writable server state. No model pull, model copy or sovereign-dev container changes were performed. Windows backend configuration now uses localhost:11435; the Compose backend uses ollama:11434. Storage is shared, while server ownership and inference endpoints are separate.

Dedicated-runtime verification: the actual Windows .NET backend checked /api/tags on localhost:11435 and found the installed qwen2.5-coder:3b digest unchanged. One validated replay-summary inference succeeded in 26.46 seconds with zero retries. This is a connectivity/structured-output smoke check, not a quality benchmark. Full Docker application startup was not performed.
