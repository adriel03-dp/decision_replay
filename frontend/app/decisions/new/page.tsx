"use client"

import { useState } from "react"
import { useRouter } from "next/navigation"
import Link from "next/link"
import { ChevronLeft, Sparkles, Clock, DollarSign, Target, Save, FileText } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Card } from "@/components/ui/card"
import { useAuth } from "@/lib/auth-context"
import { useToast } from "@/hooks/use-toast"
import { decisionApi } from "@/lib/api"

export default function NewDecisionPage() {
  const router = useRouter()
  const { user } = useAuth()
  const { toast } = useToast()
  const [decisionInput, setDecisionInput] = useState("")
  const [submitting, setSubmitting] = useState(false)
  const [analyzing, setAnalyzing] = useState(false)

  const validateInput = () => {
    const trimmedInput = decisionInput.trim()

    if (!trimmedInput) {
      toast({
        title: "Input Required",
        description: "Please describe your decision before proceeding.",
        variant: "destructive"
      })
      return false
    }

    if (trimmedInput.length < 20) {
      toast({
        title: "More Details Needed",
        description: "Please provide more details about your decision (at least 20 characters).",
        variant: "destructive"
      })
      return false
    }

    if (!user) {
      toast({
        title: "Authentication Required",
        description: "Please log in to create a decision.",
        variant: "destructive"
      })
      router.push('/login')
      return false
    }

    return true
  }

  const handleCreateDecision = async (analyzeNow: boolean) => {
    if (!validateInput()) return

    if (analyzeNow) setAnalyzing(true)
    else setSubmitting(true)

    try {
      const decision = await decisionApi.createDecision({
        input: decisionInput.trim(),
        createdBy: user?.email || 'user@example.com',
        analyzeNow: analyzeNow
      })

      if (analyzeNow) {
        toast({
          title: "Analysis Complete!",
          description: "Your decision has been analyzed and saved.",
          variant: "default"
        })
      } else {
        toast({
          title: "Draft Saved",
          description: "Your decision has been saved as a draft.",
          variant: "default"
        })
      }

      router.push(`/decisions/${decision.id}`)
    } catch (error) {
      console.error('Failed to create decision:', error)
      toast({
        title: "Operation Failed",
        description: "Failed to create decision. Please try again.",
        variant: "destructive"
      })
    } finally {
      setAnalyzing(false)
      setSubmitting(false)
    }
  }

  const examplePrompts = [
    "Launch a mobile app with authentication and chat features in 3 months. Team: 2 iOS devs, 1 backend dev. Budget: $75k.",
    "Build a 2-story house with 4 bedrooms in 8 months. Budget: $350k. Location: suburban area with zoning restrictions.",
    "Implement microservices migration for a monolithic e-commerce platform. Timeline: 6 months. Team: 5 engineers, 1 architect.",
    "Create an AI-powered customer support chatbot with multi-language support. Timeline: 4 months. Budget: $90k. Must integrate with existing CRM."
  ]

  return (
    <main className="min-h-screen bg-gradient-to-br from-white to-green-50 dark:from-slate-950 dark:to-slate-900">
      <div className="bg-white/80 dark:bg-slate-950/80 backdrop-blur border-b border-green-100 dark:border-slate-800">
        <div className="max-w-4xl mx-auto px-6 py-4 flex items-center gap-4">
          <Link href="/decisions">
            <Button variant="ghost" size="sm">
              <ChevronLeft className="w-4 h-4 mr-2" />
              Back
            </Button>
          </Link>
          <h1 className="text-2xl font-bold">Create New Decision</h1>
        </div>
      </div>

      <div className="max-w-4xl mx-auto px-6 py-8">
        {/* Header */}
        <div className="mb-8">
          <div className="flex items-center gap-2 mb-3">
            <Sparkles className="w-5 h-5 text-green-600 dark:text-green-400" />
            <h2 className="text-xl font-bold">Decision Workspace</h2>
          </div>
          <p className="text-foreground/60">
            Describe your decision in natural language. Gemini AI will analyze feasibility, identify risks, and provide structured recommendations.
          </p>
        </div>

        {/* Main Form */}
        <div className="space-y-6">
          <Card className="p-6 border-green-100 dark:border-slate-700 bg-white/50 dark:bg-slate-900/50 backdrop-blur">
            <label className="block text-sm font-semibold mb-3">
              Describe Your Decision <span className="text-red-600">*</span>
            </label>
            <textarea
              value={decisionInput}
              onChange={(e) => setDecisionInput(e.target.value)}
              placeholder="Example: Launch a mobile app with authentication and real-time chat in 3 months. Team: 2 iOS developers, 1 backend engineer. Budget: $75,000. Constraints: Must support 10K concurrent users."
              rows={8}
              className="w-full px-4 py-3 text-sm bg-white dark:bg-slate-800 border border-green-200 dark:border-slate-700 rounded-lg focus:ring-2 focus:ring-green-500 focus:border-transparent resize-none"
              disabled={submitting || analyzing}
            />
            <p className="text-xs text-foreground/50 mt-2">
              {decisionInput.length} characters (minimum 20 required)
            </p>
          </Card>

          {/* Context Hints */}
          <Card className="p-6 border-green-100 dark:border-slate-700 bg-white/50 dark:bg-slate-900/50 backdrop-blur">
            <h3 className="text-sm font-semibold mb-4">AI will automatically infer:</h3>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div className="flex items-start gap-3">
                <Clock className="w-5 h-5 text-green-600 dark:text-green-400 mt-0.5" />
                <div>
                  <div className="font-semibold text-sm">Timeline</div>
                  <div className="text-xs text-foreground/60">Start, end, milestones</div>
                </div>
              </div>
              <div className="flex items-start gap-3">
                <DollarSign className="w-5 h-5 text-green-600 dark:text-green-400 mt-0.5" />
                <div>
                  <div className="font-semibold text-sm">Budget</div>
                  <div className="text-xs text-foreground/60">Cost constraints</div>
                </div>
              </div>
              <div className="flex items-start gap-3">
                <Target className="w-5 h-5 text-green-600 dark:text-green-400 mt-0.5" />
                <div>
                  <div className="font-semibold text-sm">Scope</div>
                  <div className="text-xs text-foreground/60">Requirements, deliverables</div>
                </div>
              </div>
            </div>
          </Card>

          {/* Example Prompts */}
          <Card className="p-6 border-green-100 dark:border-slate-700 bg-white/50 dark:bg-slate-900/50 backdrop-blur">
            <h3 className="text-sm font-semibold mb-3">Example Decisions:</h3>
            <div className="space-y-2">
              {examplePrompts.map((prompt, idx) => (
                <button
                  key={idx}
                  type="button"
                  onClick={() => setDecisionInput(prompt)}
                  className="w-full text-left px-4 py-3 text-xs bg-green-50 dark:bg-slate-800 border border-green-100 dark:border-slate-700 rounded-lg hover:bg-green-100 dark:hover:bg-slate-700 transition-colors"
                  disabled={submitting || analyzing}
                >
                  {prompt}
                </button>
              ))}
            </div>
          </Card>

          {/* Action Buttons */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
            <Button
              onClick={() => handleCreateDecision(true)}
              className="bg-gradient-to-r from-blue-500 to-cyan-600 hover:from-blue-600 hover:to-cyan-700"
              disabled={submitting || analyzing}
            >
              {analyzing ? (
                <>
                  <div className="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin mr-2"></div>
                  Analyzing...
                </>
              ) : (
                <>
                  <Sparkles className="w-4 h-4 mr-2" />
                  Analyze & Save
                </>
              )}
            </Button>

            <Button
              onClick={() => handleCreateDecision(false)}
              variant="outline"
              className="border-green-200 dark:border-green-700 hover:border-green-400 dark:hover:border-green-500 hover:bg-green-50 dark:hover:bg-green-900/20"
              disabled={submitting || analyzing}
            >
              {submitting ? (
                <>
                  <div className="w-4 h-4 border-2 border-green-600 border-t-transparent rounded-full animate-spin mr-2"></div>
                  Saving...
                </>
              ) : (
                <>
                  <FileText className="w-4 h-4 mr-2" />
                  Save as Draft
                </>
              )}
            </Button>

            <Link href="/decisions" className="flex">
              <Button variant="outline" className="w-full" disabled={submitting || analyzing}>
                Cancel
              </Button>
            </Link>
          </div>

          {/* Info Cards */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4 pt-4">
            <Card className="p-4 border-blue-100 dark:border-blue-700 bg-blue-50/50 dark:bg-blue-900/20 backdrop-blur">
              <div className="flex items-start gap-3">
                <Sparkles className="w-5 h-5 text-blue-600 dark:text-blue-400 mt-0.5" />
                <div>
                  <h4 className="font-semibold text-sm text-blue-900 dark:text-blue-100">Analyze & Save</h4>
                  <p className="text-xs text-blue-700 dark:text-blue-300 mt-1">
                    Get instant AI analysis and save the decision. Best for complete inputs.
                  </p>
                </div>
              </div>
            </Card>

            <Card className="p-4 border-green-100 dark:border-green-700 bg-green-50/50 dark:bg-green-900/20 backdrop-blur">
              <div className="flex items-start gap-3">
                <FileText className="w-5 h-5 text-green-600 dark:text-green-400 mt-0.5" />
                <div>
                  <h4 className="font-semibold text-sm text-green-900 dark:text-green-100">Save as Draft</h4>
                  <p className="text-xs text-green-700 dark:text-green-300 mt-1">
                    Save without analysis. Useful for quick capture or when offline. You can analyze later.
                  </p>
                </div>
              </div>
            </Card>
          </div>
        </div>
      </div>
    </main>
  )
}
