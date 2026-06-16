"use client"

import { useState } from "react"
import axios from "axios"
import Link from "next/link"
import { useRouter } from "next/navigation"
import {
  ArrowLeft,
  ArrowRight,
  Braces,
  CheckCircle2,
  Compass,
  FileText,
  Gauge,
  Loader2,
  Route,
} from "lucide-react"
import { Button } from "@/components/ui/button"
import { Card, CardContent } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { decisionApi } from "@/lib/api"
import { useToast } from "@/hooks/use-toast"

const EXAMPLE =
  "I want to launch a subscription meal-planning app for busy professionals. My budget is $35,000, the timeline is 6 months, and I have a team of 3. The first release needs onboarding, weekly plans, payments, and basic analytics. I need to validate demand before committing the full budget."

const PIPELINE = [
  ["Describe", "Share the decision, constraints, and uncertainty in your own words."],
  ["Clarify", "Decision Replay highlights what is known, missing, and assumed."],
  ["Evaluate", "The system turns the situation into a practical decision profile."],
  ["Act", "You receive a plan you can compare, revisit, and improve."],
]

export default function NewDecisionPage() {
  const router = useRouter()
  const { toast } = useToast()
  const [input, setInput] = useState("")
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function handleAnalyze() {
    if (input.trim().length < 10) {
      setError("Describe the decision in at least 10 characters.")
      return
    }

    setSubmitting(true)
    setError(null)
    try {
      const result = await decisionApi.analyze(input.trim())
      toast({
        title: "Decision analyzed",
        description: `Version ${result.version} scored ${Math.round(result.feasibilityScore)}/100.`,
      })
      router.push(`/decisions/${result.decisionId}/analytics`)
    } catch (requestError) {
      const message = axios.isAxiosError(requestError)
        ? requestError.response?.data?.message ?? "The decision could not be analyzed."
        : "The decision could not be analyzed."
      setError(message)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <main className="min-h-screen bg-[#080b0d] text-slate-100">
      <div
        className="pointer-events-none fixed inset-0 opacity-80"
        style={{
          background:
            "radial-gradient(circle at 15% 10%, rgba(34,197,94,.15), transparent 30%), radial-gradient(circle at 88% 20%, rgba(14,165,233,.1), transparent 26%), repeating-linear-gradient(90deg, rgba(255,255,255,.02) 0, rgba(255,255,255,.02) 1px, transparent 1px, transparent 80px)",
        }}
      />
      <div className="relative mx-auto max-w-6xl px-5 py-10 lg:py-16">
        <Link
          href="/decisions"
          className="inline-flex items-center gap-2 text-xs font-semibold uppercase tracking-[0.18em] text-slate-500 transition hover:text-white"
        >
          <ArrowLeft className="h-4 w-4" />
          Decision register
        </Link>

        <div className="mt-8 grid gap-10 lg:grid-cols-[1.1fr_0.9fr]">
          <section>
            <Badge className="border-green-400/20 bg-green-400/10 text-green-300">
              <Compass className="mr-1 h-3 w-3" />
              Built for confident commitments
            </Badge>
            <h1 className="mt-5 max-w-3xl text-4xl font-semibold tracking-[-0.045em] sm:text-5xl">
              Turn a messy decision into an auditable plan.
            </h1>
            <p className="mt-4 max-w-2xl text-sm leading-7 text-slate-400">
              Include the goal, budget, timeline, resources, scope, and constraints you know. Decision Replay helps
              you understand the opportunity, expose weak spots, and move forward with a clearer action plan.
            </p>

            <Card className="mt-8 overflow-hidden border-white/10 bg-slate-950/80 shadow-2xl shadow-black/30">
              <div className="h-1 bg-gradient-to-r from-green-400 via-emerald-400 to-sky-400" />
              <CardContent className="p-5 sm:p-7">
                <div className="mb-3 flex items-center justify-between gap-4">
                  <label htmlFor="decision-input" className="text-sm font-semibold">
                    Describe the decision
                  </label>
                  <span className="text-xs text-slate-600">{input.length}/20,000</span>
                </div>
                <textarea
                  id="decision-input"
                  value={input}
                  maxLength={20000}
                  onChange={(event) => setInput(event.target.value)}
                  placeholder="What are you deciding, and what constraints make it difficult?"
                  className="min-h-64 w-full resize-y rounded-xl border border-white/10 bg-black/25 p-4 text-sm leading-7 text-slate-200 outline-none transition placeholder:text-slate-700 focus:border-green-400/50 focus:ring-2 focus:ring-green-400/10"
                />
                {error && (
                  <div role="alert" className="mt-3 rounded-lg border border-red-400/20 bg-red-400/10 p-3 text-sm text-red-300">
                    {error}
                  </div>
                )}
                <div className="mt-4 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                  <button
                    type="button"
                    onClick={() => setInput(EXAMPLE)}
                    className="text-left text-xs font-medium text-slate-500 transition hover:text-sky-300"
                  >
                    Use an example decision
                  </button>
                  <Button
                    onClick={handleAnalyze}
                    disabled={submitting}
                    className="bg-green-400 text-black hover:bg-green-300"
                  >
                    {submitting ? (
                      <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                    ) : (
                      <Gauge className="mr-2 h-4 w-4" />
                    )}
                    Analyze and build plan
                    {!submitting && <ArrowRight className="ml-2 h-4 w-4" />}
                  </Button>
                </div>
              </CardContent>
            </Card>
          </section>

          <aside className="lg:pt-20">
            <div className="rounded-2xl border border-white/10 bg-white/[0.025] p-6">
              <div className="flex items-center gap-2 text-sm font-semibold">
                <Route className="h-4 w-4 text-sky-300" />
                What you get
              </div>
              <div className="mt-6 space-y-0">
                {PIPELINE.map(([title, description], index) => (
                  <div key={title} className="grid grid-cols-[32px_1fr] gap-3">
                    <div className="relative flex justify-center">
                      <div className="z-10 grid h-7 w-7 place-items-center rounded-full border border-white/15 bg-slate-950 text-[10px] font-semibold text-green-300">
                        {index + 1}
                      </div>
                      {index < PIPELINE.length - 1 && <div className="absolute bottom-0 top-7 w-px bg-white/10" />}
                    </div>
                    <div className="pb-6">
                      <h2 className="text-sm font-medium">{title}</h2>
                      <p className="mt-1 text-xs leading-5 text-slate-500">{description}</p>
                    </div>
                  </div>
                ))}
              </div>
            </div>

            <div className="mt-4 grid grid-cols-3 gap-3">
              {[
                [Braces, "Structured"],
                [CheckCircle2, "Testable"],
                [FileText, "Exportable"],
              ].map(([Icon, label]) => {
                const IconComponent = Icon as typeof Braces
                return (
                  <div key={label as string} className="rounded-xl border border-white/10 bg-white/[0.025] p-4 text-center">
                    <IconComponent className="mx-auto h-4 w-4 text-slate-400" />
                    <div className="mt-2 text-[10px] uppercase tracking-wider text-slate-600">{label as string}</div>
                  </div>
                )
              })}
            </div>
          </aside>
        </div>
      </div>
    </main>
  )
}
