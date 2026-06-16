import Link from "next/link"
import { ArrowRight, Compass, History, Scale, ShieldCheck } from "lucide-react"
import { Button } from "@/components/ui/button"

const PRINCIPLES = [
  {
    icon: Scale,
    title: "Every idea gets graded",
    description: "Turn loose ideas, constraints, and unknowns into a clear view of what is strong and what needs work.",
  },
  {
    icon: Compass,
    title: "Flaws become next steps",
    description: "Use intelligent guidance to spot weak points and turn them into practical improvements.",
  },
  {
    icon: History,
    title: "Plans keep getting better",
    description: "Replay decisions as budgets, timelines, resources, and scope change, then compare which version is stronger.",
  },
]

export default function AboutPage() {
  return (
    <main className="min-h-screen bg-[#080b0d] text-slate-100">
      <section className="mx-auto max-w-6xl px-5 py-20 lg:py-28">
        <div className="max-w-4xl">
          <div className="inline-flex items-center gap-2 rounded-full border border-green-400/20 bg-green-400/10 px-3 py-1 text-xs font-semibold text-green-300">
            <ShieldCheck className="h-3.5 w-3.5" />
            AI-integrated decision intelligence
          </div>
          <h1 className="mt-7 text-5xl font-semibold tracking-[-0.05em] sm:text-6xl">
            Built to improve plans before they become commitments.
          </h1>
          <p className="mt-6 max-w-3xl text-lg leading-8 text-slate-400">
            Decision Replay helps people take an early idea, grade its feasibility, identify the flaws, and turn the
            basic version into a stronger plan. The goal is simple: help users understand what to improve and what to
            do next before spending time, money, or reputation on the wrong move.
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
            <h2 className="text-2xl font-semibold">Why we built it</h2>
            <p className="mt-4 text-sm leading-7 text-slate-400">
              Most important decisions start messy: a half-clear goal, a rough budget, a short timeline, and a lot of
              assumptions. Decision Replay was built to show where a plan is weak, explain how to improve it, and help
              users move toward a more feasible version before the real cost of a bad call shows up.
            </p>
          </div>
          <div>
            <h2 className="text-2xl font-semibold">Founder</h2>
            <p className="mt-4 text-sm leading-7 text-slate-400">
              Decision Replay is owned by Adriel Perera, a 3rd Year Software Engineering Undergraduate at the
              Sri Lanka Institute of Information Technology. The product grew from a simple belief: better decisions
              should be easier to revisit, explain, and trust.
            </p>
          </div>
        </div>

        <div className="mt-14 rounded-2xl border border-white/10 bg-gradient-to-r from-green-400/[0.08] to-sky-400/[0.05] p-8 sm:flex sm:items-center sm:justify-between">
          <div>
            <h2 className="text-2xl font-semibold">Start with the plan you have now.</h2>
            <p className="mt-2 text-sm text-slate-500">Bring the goal, constraints, and uncertainty. Decision Replay will help upgrade the next version.</p>
          </div>
          <Link href="/decisions/new">
            <Button className="mt-5 bg-green-400 text-black hover:bg-green-300 sm:mt-0">
              Improve a plan
              <ArrowRight className="ml-2 h-4 w-4" />
            </Button>
          </Link>
        </div>
      </section>
    </main>
  )
}
