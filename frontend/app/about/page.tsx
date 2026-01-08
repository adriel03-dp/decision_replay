"use client"

import { Button } from "@/components/ui/button"
import { ArrowRight, Zap, Users, Target } from "lucide-react"
import Link from "next/link"

export default function AboutPage() {
  return (
    <main className="gradient-bg min-h-screen">
      {/* Hero Section */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-20 md:py-32">
        <div className="max-w-3xl mx-auto text-center slide-up">
          <h1 className="text-5xl md:text-6xl font-bold leading-tight mb-6">
            Better Planning Decisions, <span className="gradient-text">Powered by AI</span>
          </h1>
          <p className="text-xl text-foreground/70 mb-8">
            Decision Replay was built to help product and project decision-makers validate their plans, identify risks early, 
            and learn from past decisions. We bring AI-powered feasibility analysis to human planning decisions.
          </p>
        </div>
      </section>

      {/* Mission Section */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-20">
        <div className="grid md:grid-cols-2 gap-12 items-center">
          <div className="slide-up">
            <h2 className="text-4xl font-bold mb-6">Our Mission</h2>
            <p className="text-lg text-foreground/70 mb-6">
              We believe that better planning starts with honest feasibility analysis. Too many projects fail because 
              timelines were unrealistic, resources insufficient, or constraints overlooked. AI can help identify these 
              issues before they become problems.
            </p>
            <p className="text-lg text-foreground/70 mb-8">
              Decision Replay provides AI-assisted feasibility analysis for your planning decisions. Submit your project plans 
              (scope, timeline, resources, constraints) and get actionable insights on risks, bottlenecks and alternatives. 
              Learn from past decisions to improve future planning.
            </p>
            <Link href="/signup">
              <Button className="gradient-button group">
                Join Our Mission
                <ArrowRight className="w-4 h-4 ml-2 group-hover:translate-x-1 smooth-transition" />
              </Button>
            </Link>
          </div>
          <div className="relative hidden md:flex items-center justify-center">
            <div className="absolute inset-0 bg-gradient-to-br from-green-400/20 to-emerald-400/10 rounded-3xl blur-3xl" />
            <div className="relative w-full h-96 bg-gradient-to-br from-green-100 to-white dark:from-slate-800 dark:to-slate-900 rounded-3xl border border-green-200 dark:border-slate-700 flex items-center justify-center">
              <div className="absolute inset-0 opacity-20">
                <div className="absolute top-10 right-10 w-24 h-24 bg-gradient-to-br from-green-400 to-emerald-500 rounded-full blur-2xl" />
                <div className="absolute bottom-10 left-10 w-40 h-40 bg-gradient-to-br from-green-300 to-emerald-400 rounded-full blur-3xl" />
              </div>
              <Zap className="relative w-24 h-24 text-green-600 dark:text-green-400" />
            </div>
          </div>
        </div>
      </section>

      {/* Values Section */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-20">
        <div className="text-center mb-16 slide-up">
          <h2 className="text-4xl font-bold mb-4">Our Core Values</h2>
          <p className="text-foreground/70 text-lg">Principles that guide everything we build</p>
        </div>

        <div className="grid md:grid-cols-3 gap-8">
          {[
            {
              icon: Target,
              title: "Honest Feasibility",
              description:
                "AI that tells you the truth about your plans. No sugar-coating. Clear insights on what's realistic and what needs adjustment.",
            },
            {
              icon: Users,
              title: "Decision-Maker Focused",
              description:
                "Built for product managers, project leads, and team planners who need data-driven insights to make better decisions.",
            },
            {
              icon: Zap,
              title: "Learn from History",
              description:
                "Review past planning decisions to understand patterns. What worked? What didn't? Apply those lessons to future plans.",
            },
          ].map((value, i) => (
            <div
              key={i}
              className="p-8 rounded-2xl border border-green-100 dark:border-slate-700 bg-white/50 dark:bg-slate-900/50 backdrop-blur hover:bg-white dark:hover:bg-slate-800 hover:shadow-lg hover:shadow-green-500/10 smooth-transition group slide-up"
              style={{ animationDelay: `${i * 100}ms` }}
            >
              <value.icon className="w-12 h-12 text-green-600 dark:text-green-400 mb-4 group-hover:scale-110 smooth-transition" />
              <h3 className="text-xl font-semibold mb-3">{value.title}</h3>
              <p className="text-foreground/70">{value.description}</p>
            </div>
          ))}
        </div>
      </section>

      {/* Story Section */}
      <section className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-20">
        <div className="slide-up">
          <h2 className="text-4xl font-bold mb-8">About This Project</h2>
          <div className="space-y-6 text-lg text-foreground/70">
            <p>
              <strong>Decision Replay</strong> is a portfolio project created by <strong>Adriel Perera</strong>, 
              a Software Engineering undergraduate passionate about building production-ready systems that demonstrate 
              real-world software architecture principles.
            </p>
            <p>
              This project was built to showcase mastery of <strong>.NET Core</strong>, <strong>Clean Architecture</strong>, 
              <strong>SOLID principles</strong> and <strong>AI integration</strong> using Google's Gemini API. 
              The system uses advanced AI reasoning to analyze decision feasibility, identify risks, and provide 
              structured recommendations—going beyond simple prompting to deliver actionable intelligence.
            </p>
            <p>
              <strong>Why Decision Replay?</strong> Most decision-making tools are domain-specific and rigid. 
              This platform accepts natural language input and works across any domain which includes software projects, construction 
              planning, logistics, personal decisions, making it a truly generalized decision reasoning engine.
            </p>
            <p>
              The core innovation is the <strong>Decision Replay</strong> feature: users can update their constraints 
              (timeline, budget, resources) and instantly see how those changes impact feasibility. It's like a 
              time machine for decision-making, helping users explore "what-if" scenarios before committing.
            </p>
            <p>
              <strong>Target Users:</strong> Anyone making constrained decisions:project managers validating timelines, 
              founders planning product launches, team leads assessing resource allocation or individuals navigating 
              complex personal decisions. If your decision has constraints, this tool helps you understand what's realistic.
            </p>
            <p>
              <strong>Technical Highlights:</strong> Domain-agnostic architecture, intent parsing system, dynamic schema 
              generation, structured AI analysis with pros/cons/risks, decision replay engine, chart-agnostic visualization 
              data, and production-ready rate limiting and error handling.
            </p>
          </div>
        </div>
      </section>

      {/* CTA Section */}
      <section className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-20">
        <div className="p-12 rounded-3xl bg-gradient-to-br from-green-50 to-white dark:from-slate-900 dark:to-slate-950 border border-green-200 dark:border-slate-800 text-center slide-up">
          <h2 className="text-4xl font-bold mb-4">Ready to Experience Decision Intelligence?</h2>
          <p className="text-foreground/70 text-lg mb-8">
            Create an account and submit your first decision for AI-powered feasibility analysis. 
            See how changing constraints impacts outcomes with Decision Replay.
          </p>
          <div className="flex flex-col sm:flex-row gap-4 justify-center">
            <Link href="/signup">
              <Button className="gradient-button group">
                Get Started Free
                <ArrowRight className="w-4 h-4 ml-2 group-hover:translate-x-1 smooth-transition" />
              </Button>
            </Link>
            <Link href="/login">
              <Button
                variant="outline"
                className="bg-white/50 dark:bg-slate-800/50 backdrop-blur border-green-200 dark:border-slate-700"
              >
                Sign In
              </Button>
            </Link>
          </div>
        </div>
      </section>
    </main>
  )
}
