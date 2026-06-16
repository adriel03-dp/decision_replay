"use client"

import Link from "next/link"
import { Button } from "@/components/ui/button"
import { ArrowRight, CheckCircle2, TrendingUp, Shield } from "lucide-react"

export default function HomePage() {
  return (
    <main className="gradient-bg min-h-screen">
      {/* Hero Section */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-20 md:py-32">
        <div className="grid md:grid-cols-2 gap-12 items-center">
          {/* Left Content */}
          <div className="flex flex-col gap-8 slide-up">
            <div className="space-y-4">
              <div className="inline-block">
                <span className="text-sm font-semibold text-green-600 dark:text-green-400 bg-green-100/50 dark:bg-green-900/30 px-3 py-1 rounded-full">
                  Plan Improvement Intelligence
                </span>
              </div>
              <h1 className="text-5xl md:text-6xl font-bold leading-tight">
                Upgrade rough ideas into <span className="gradient-text">high-feasibility plans</span>
              </h1>
              <p className="text-lg text-foreground/70 max-w-xl">
                Bring an early plan, business idea, project scope, budget, or timeline. Decision Replay grades it,
                finds the weak points, and shows exactly what to improve so the idea can move closer to a top-tier,
                execution-ready outcome.
              </p>
            </div>

            {/* CTA Buttons */}
            <div className="flex flex-col sm:flex-row gap-4">
              <Link href="/signup">
                <Button className="gradient-button w-full sm:w-auto group">
                  Get Started Free
                  <ArrowRight className="w-4 h-4 ml-2 group-hover:translate-x-1 smooth-transition" />
                </Button>
              </Link>
              <Link href="/about">
                <Button
                  variant="outline"
                  className="w-full sm:w-auto bg-white/50 dark:bg-slate-800/50 backdrop-blur border-green-200 dark:border-slate-700 hover:border-green-400 dark:hover:border-green-500"
                >
                  Learn More
                </Button>
              </Link>
            </div>

            {/* Stats */}
            <div className="grid grid-cols-2 gap-4 pt-4">
              <div className="flex items-center gap-3">
                <CheckCircle2 className="w-5 h-5 text-green-600 dark:text-green-400" />
                <span className="text-sm font-medium">Idea Grading</span>
              </div>
              <div className="flex items-center gap-3">
                <TrendingUp className="w-5 h-5 text-green-600 dark:text-green-400" />
                <span className="text-sm font-medium">Plan Improvement</span>
              </div>
              <div className="flex items-center gap-3">
                <Shield className="w-5 h-5 text-green-600 dark:text-green-400" />
                <span className="text-sm font-medium">Risk Identification</span>
              </div>
              <div className="flex items-center gap-3">
                <CheckCircle2 className="w-5 h-5 text-green-600 dark:text-green-400" />
                <span className="text-sm font-medium">Execution Roadmaps</span>
              </div>
            </div>
          </div>

          {/* Right Gradient Showcase */}
          <div className="relative hidden md:flex items-center justify-center">
            <div className="absolute inset-0 bg-gradient-to-br from-green-400/20 to-emerald-400/10 rounded-3xl blur-3xl" />
            <div className="relative w-full h-96 bg-gradient-to-br from-green-100 to-white dark:from-slate-800 dark:to-slate-900 rounded-3xl border border-green-200 dark:border-slate-700 flex items-center justify-center overflow-hidden">
              <div className="absolute inset-0 opacity-20">
                <div className="absolute top-10 left-10 w-20 h-20 bg-gradient-to-br from-green-400 to-emerald-500 rounded-full blur-2xl" />
                <div className="absolute bottom-10 right-10 w-32 h-32 bg-gradient-to-br from-green-300 to-emerald-400 rounded-full blur-3xl" />
              </div>
              <div className="relative text-center px-6">
                <div className="text-4xl font-bold gradient-text mb-3">Decision Replay</div>
                <p className="text-sm text-foreground/60">Interactive decision analysis</p>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Features Section */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-20">
        <div className="text-center mb-16 slide-up">
          <h2 className="text-4xl font-bold mb-4">From basic plan to better outcome</h2>
          <p className="text-foreground/70 text-lg max-w-2xl mx-auto">
            AI-integrated decision intelligence that grades your idea, identifies the flaws, and gives you a stronger path forward.
          </p>
        </div>

        <div className="grid md:grid-cols-3 gap-8">
          {[
            {
              icon: CheckCircle2,
              title: "Start with the idea",
              description:
                "Describe the plan you have now, even if it is incomplete, messy, or still only partly thought through.",
            },
            {
              icon: TrendingUp,
              title: "Find what holds it back",
              description:
                "See the gaps in budget, time, resources, scope, assumptions, and risk before those gaps become expensive.",
            },
            {
              icon: Shield,
              title: "Build the upgraded plan",
              description:
                "Use the improvement advice and action roadmap to turn a basic version into a stronger, more feasible plan.",
            },
          ].map((feature, i) => (
            <div
              key={i}
              className="p-8 rounded-2xl border border-green-100 dark:border-slate-700 bg-white/50 dark:bg-slate-900/50 backdrop-blur hover:bg-white dark:hover:bg-slate-800 hover:shadow-lg hover:shadow-green-500/10 smooth-transition group"
            >
              <feature.icon className="w-10 h-10 text-green-600 dark:text-green-400 mb-4 group-hover:scale-110 smooth-transition" />
              <h3 className="text-xl font-semibold mb-3">{feature.title}</h3>
              <p className="text-foreground/70">{feature.description}</p>
            </div>
          ))}
        </div>
      </section>

      {/* CTA Section */}
      <section className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-20">
        <div className="p-12 rounded-3xl bg-gradient-to-br from-green-50 to-white dark:from-slate-900 dark:to-slate-950 border border-green-200 dark:border-slate-800 text-center slide-up">
          <h2 className="text-4xl font-bold mb-4">Ready to improve your next plan?</h2>
          <p className="text-foreground/70 text-lg mb-8 max-w-2xl mx-auto">
            Use Decision Replay to grade the idea, understand what is missing, and get the practical advice needed to make
            the plan stronger before you commit.
          </p>
          <Link href="/signup">
            <Button className="gradient-button group">
              Get Started Free
              <ArrowRight className="w-4 h-4 ml-2 group-hover:translate-x-1 smooth-transition" />
            </Button>
          </Link>
        </div>
      </section>

      {/* Footer */}
      <footer className="border-t border-green-100 dark:border-slate-800 mt-20 py-12 bg-white/50 dark:bg-slate-950/50">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="grid md:grid-cols-4 gap-8 mb-8">
            <div>
              <div className="font-bold text-lg gradient-text mb-4">Decision Replay</div>
              <p className="text-sm text-foreground/60">Planning decision intelligence platform</p>
            </div>
            <div>
              <h4 className="font-semibold mb-4">Product</h4>
              <ul className="space-y-2 text-sm text-foreground/70">
                <li>
                  <Link href="/decisions" className="hover:text-foreground smooth-transition">
                    Features
                  </Link>
                </li>
                <li>
                  <Link href="/about" className="hover:text-foreground smooth-transition">
                    About
                  </Link>
                </li>
              </ul>
            </div>
            <div>
              <h4 className="font-semibold mb-4">Legal</h4>
              <ul className="space-y-2 text-sm text-foreground/70">
                <li>
                  <Link href="#" className="hover:text-foreground smooth-transition">
                    Privacy
                  </Link>
                </li>
                <li>
                  <Link href="#" className="hover:text-foreground smooth-transition">
                    Terms
                  </Link>
                </li>
              </ul>
            </div>
            <div>
              <h4 className="font-semibold mb-4">Contact</h4>
              <p className="text-sm text-foreground/70">hello@decisionreplay.io</p>
            </div>
          </div>
          <div className="border-t border-green-100 dark:border-slate-800 pt-8 text-center text-sm text-foreground/60">
            <p>© 2026 Decision Replay. All rights reserved.</p>
          </div>
        </div>
      </footer>
    </main>
  )
}
