import Link from "next/link"
import { ArrowRight, Braces, History, Scale, ShieldCheck } from "lucide-react"
import { Button } from "@/components/ui/button"

const PRINCIPLES = [
  {
    icon: Scale,
    title: "Scores show their work",
    description: "Every feasibility score is a weighted result from a versioned domain template.",
  },
  {
    icon: Braces,
    title: "Language is an interface",
    description: "Groq extracts and explains structured data. It never owns scores, risks, or final recommendations.",
  },
  {
    icon: History,
    title: "Every change is replayable",
    description: "Budget, timeline, resources, and scope changes become comparable decision versions.",
  },
]

export default function AboutPage() {
  return (
    <main className="min-h-screen bg-[#080b0d] text-slate-100">
      <section className="mx-auto max-w-6xl px-5 py-20 lg:py-28">
        <div className="max-w-4xl">
          <div className="inline-flex items-center gap-2 rounded-full border border-green-400/20 bg-green-400/10 px-3 py-1 text-xs font-semibold text-green-300">
            <ShieldCheck className="h-3.5 w-3.5" />
            Backend-owned decision intelligence
          </div>
          <h1 className="mt-7 text-5xl font-semibold tracking-[-0.05em] sm:text-6xl">
            Decision support should be explainable before it is impressive.
          </h1>
          <p className="mt-6 max-w-3xl text-lg leading-8 text-slate-400">
            Decision Replay converts natural-language plans into validated fields, deterministic feasibility scores,
            explicit risks, versioned comparisons, and timeline-bound action plans.
          </p>
        </div>

        <div className="mt-14 grid gap-4 md:grid-cols-3">
          {PRINCIPLES.map(({ icon: Icon, title, description }) => (
            <article key={title} className="rounded-2xl border border-white/10 bg-white/[0.025] p-6">
              <Icon className="h-5 w-5 text-green-300" />
              <h2 className="mt-6 text-lg font-semibold">{title}</h2>
              <p className="mt-2 text-sm leading-6 text-slate-500">{description}</p>
            </article>
          ))}
        </div>

        <div className="mt-16 grid gap-10 border-t border-white/10 pt-12 lg:grid-cols-2">
          <div>
            <h2 className="text-2xl font-semibold">Architecture</h2>
            <p className="mt-4 text-sm leading-7 text-slate-400">
              The .NET backend owns validation, domain selection, scoring, risk classification, recommendations,
              plan structure, replay comparison, persistence, exports, and audit events. These services can be tested
              without a language-model call.
            </p>
          </div>
          <div>
            <h2 className="text-2xl font-semibold">Language service boundary</h2>
            <p className="mt-4 text-sm leading-7 text-slate-400">
              A provider-neutral <code className="text-green-300">IAiLanguageService</code> isolates extraction and
              wording. The Groq implementation receives immutable backend results when producing explanations and
              can only enhance descriptions for tasks the backend already created.
            </p>
          </div>
        </div>

        <div className="mt-14 rounded-2xl border border-white/10 bg-gradient-to-r from-green-400/[0.08] to-sky-400/[0.05] p-8 sm:flex sm:items-center sm:justify-between">
          <div>
            <h2 className="text-2xl font-semibold">Test a decision against real constraints.</h2>
            <p className="mt-2 text-sm text-slate-500">The engine will report what it knows, what it assumes, and what is missing.</p>
          </div>
          <Link href="/decisions/new">
            <Button className="mt-5 bg-green-400 text-black hover:bg-green-300 sm:mt-0">
              Analyze a decision
              <ArrowRight className="ml-2 h-4 w-4" />
            </Button>
          </Link>
        </div>
      </section>
    </main>
  )
}
