"use client"

import { useState, useEffect } from "react"
import { useParams, useRouter } from "next/navigation"
import Link from "next/link"
import { ChevronLeft, Sparkles, Send, BarChart3, AlertCircle, CheckCircle2, TrendingUp } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Card } from "@/components/ui/card"
import { useToast } from "@/hooks/use-toast"

export default function DecisionAnalysisPage() {
  const params = useParams()
  const router = useRouter()
  const { toast } = useToast()
  const decisionId = params.id as string

  const [decision, setDecision] = useState<any>(null)
  const [analysis, setAnalysis] = useState<any>(null)
  const [loading, setLoading] = useState(true)
  const [analyzing, setAnalyzing] = useState(false)
  const [question, setQuestion] = useState("")
  const [queryResponse, setQueryResponse] = useState<string | null>(null)
  const [querying, setQuerying] = useState(false)

  useEffect(() => {
    fetchDecision()
  }, [decisionId])

  async function fetchDecision() {
    try {
      const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/v2/decisions/${decisionId}`, {
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('token')}`
        }
      })
      if (response.ok) {
        const data = await response.json()
        setDecision(data)
        
        // Check if analysis exists
        if (data.status === 'Analyzed' || data.status === 'Completed') {
          // Analysis might already be in the decision object
          // For now, trigger analysis if not already done
          if (!analysis) {
            await analyzeDecision()
          }
        }
      } else {
        throw new Error('Failed to fetch decision')
      }
    } catch (error) {
      console.error('Failed to fetch decision:', error)
      
      // Extract user-friendly error message
      let errorMessage = 'Failed to load decision.'
      if (error instanceof Error) {
        errorMessage = error.message
      }
      
      toast({
        title: "Unable to Load Decision",
        description: errorMessage,
        variant: "destructive",
      })
      
      // Redirect after showing error
      setTimeout(() => router.push('/decisions'), 2000)
    } finally {
      setLoading(false)
    }
  }

  async function analyzeDecision() {
    if (analyzing) return
    
    setAnalyzing(true)
    try {
      const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/v2/decisions/${decisionId}/analyze`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('token')}`
        }
      })

      if (!response.ok) {
        const error = await response.json()
        throw new Error(error.message || 'Analysis failed')
      }

      const result = await response.json()
      setAnalysis(result)
    } catch (error) {
      console.error('Analysis failed:', error)
      
      // Extract user-friendly error message  
      let errorMessage = 'Failed to analyze decision'
      if (error instanceof Error) {
        errorMessage = error.message
      }
      
      toast({
        title: "Analysis Failed",
        description: errorMessage,
        variant: "destructive",
      })
    } finally {
      setAnalyzing(false)
    }
  }

  async function askQuestion() {
    if (!question.trim() || querying) return

    setQuerying(true)
    setQueryResponse(null)

    try {
      const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/v2/decisions/${decisionId}/query`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${localStorage.getItem('token')}`
        },
        body: JSON.stringify({ question })
      })

      if (!response.ok) {
        throw new Error('Query failed')
      }

      const result = await response.json()
      setQueryResponse(result.answer)
      setQuestion("")
    } catch (error) {
      console.error('Query failed:', error)
      setQueryResponse("Failed to get answer. Please try again.")
    } finally {
      setQuerying(false)
    }
  }

  if (loading) {
    return (
      <main className="min-h-screen bg-gradient-to-br from-white to-green-50 dark:from-slate-950 dark:to-slate-900">
        <div className="max-w-5xl mx-auto px-6 py-12">
          <div className="text-center py-20">
            <div className="w-16 h-16 border-4 border-green-500 border-t-transparent rounded-full animate-spin mx-auto mb-4"></div>
            <p className="text-foreground/60">Loading decision...</p>
          </div>
        </div>
      </main>
    )
  }

  if (!decision) {
    return null
  }

  return (
    <main className="min-h-screen bg-gradient-to-br from-white to-green-50 dark:from-slate-950 dark:to-slate-900">
      {/* Header */}
      <div className="bg-white/80 dark:bg-slate-950/80 backdrop-blur border-b border-green-100 dark:border-slate-800">
        <div className="max-w-5xl mx-auto px-6 py-4 flex items-center justify-between">
          <div className="flex items-center gap-4">
            <Link href="/decisions">
              <Button variant="ghost" size="sm">
                <ChevronLeft className="w-4 h-4 mr-2" />
                Back
              </Button>
            </Link>
            <h1 className="text-2xl font-bold">Decision Analysis</h1>
          </div>
          <Link href={`/decisions/${decisionId}/analytics`}>
            <Button variant="outline">
              <BarChart3 className="w-4 h-4 mr-2" />
              View Analytics
            </Button>
          </Link>
        </div>
      </div>

      <div className="max-w-5xl mx-auto px-6 py-8 space-y-6">
        {/* Decision Context */}
        <Card className="p-6 border-green-100 dark:border-slate-700 bg-white/50 dark:bg-slate-900/50 backdrop-blur">
          <h2 className="text-lg font-bold mb-3">Your Decision</h2>
          <p className="text-foreground/70">{decision.naturalLanguageInput}</p>
          {decision.inferredAttributes && Object.keys(decision.inferredAttributes).length > 0 && (
            <div className="mt-4 pt-4 border-t border-green-100 dark:border-slate-700">
              <h3 className="text-sm font-semibold mb-2">Inferred Attributes:</h3>
              <div className="grid grid-cols-2 gap-3 text-sm">
                {Object.entries(decision.inferredAttributes).map(([key, value]) => (
                  <div key={key}>
                    <span className="text-foreground/60">{key}:</span>{" "}
                    <span className="font-medium">{String(value)}</span>
                  </div>
                ))}
              </div>
            </div>
          )}
        </Card>

        {/* Analysis Results */}
        {!analysis && !analyzing && (
          <Card className="p-8 border-green-100 dark:border-slate-700 bg-white/50 dark:bg-slate-900/50 backdrop-blur text-center">
            <Sparkles className="w-12 h-12 text-green-600 dark:text-green-400 mx-auto mb-4" />
            <h2 className="text-xl font-bold mb-2">Ready to Analyze</h2>
            <p className="text-foreground/60 mb-4">
              Click below to get AI-powered feasibility analysis, risk identification, and recommendations.
            </p>
            <Button 
              onClick={analyzeDecision}
              className="bg-gradient-to-r from-green-500 to-emerald-600 hover:from-green-600 hover:to-emerald-700"
            >
              <Sparkles className="w-4 h-4 mr-2" />
              Analyze Decision
            </Button>
          </Card>
        )}

        {analyzing && (
          <Card className="p-8 border-green-100 dark:border-slate-700 bg-white/50 dark:bg-slate-900/50 backdrop-blur text-center">
            <div className="w-16 h-16 border-4 border-green-500 border-t-transparent rounded-full animate-spin mx-auto mb-4"></div>
            <h2 className="text-xl font-bold mb-2">Analyzing Decision...</h2>
            <p className="text-foreground/60">Gemini AI is evaluating feasibility, risks, and recommendations.</p>
          </Card>
        )}

        {analysis && (
          <>
            {/* Feasibility Score */}
            <Card className="p-6 border-green-100 dark:border-slate-700 bg-white/50 dark:bg-slate-900/50 backdrop-blur">
              <div className="flex items-center justify-between mb-4">
                <h2 className="text-lg font-bold">Feasibility Assessment</h2>
                <div className="flex items-center gap-2">
                  {analysis.feasibilityScore >= 70 ? (
                    <CheckCircle2 className="w-5 h-5 text-green-600 dark:text-green-400" />
                  ) : analysis.feasibilityScore >= 40 ? (
                    <AlertCircle className="w-5 h-5 text-yellow-600 dark:text-yellow-400" />
                  ) : (
                    <AlertCircle className="w-5 h-5 text-red-600 dark:text-red-400" />
                  )}
                  <span className="text-2xl font-bold">{Math.round(analysis.feasibilityScore)}/100</span>
                </div>
              </div>
              <div className="mb-3">
                <div className="text-sm font-semibold mb-1">Verdict: {analysis.feasibilityVerdict}</div>
                <div className="w-full bg-gray-200 dark:bg-slate-700 rounded-full h-2">
                  <div 
                    className={`h-2 rounded-full transition-all ${
                      analysis.feasibilityScore >= 70 
                        ? 'bg-green-500' 
                        : analysis.feasibilityScore >= 40 
                        ? 'bg-yellow-500' 
                        : 'bg-red-500'
                    }`}
                    style={{ width: `${analysis.feasibilityScore}%` }}
                  />
                </div>
              </div>
              <p className="text-foreground/70">{analysis.executiveSummary}</p>
            </Card>

            {/* Pros & Cons */}
            <div className="grid md:grid-cols-2 gap-6">
              <Card className="p-6 border-green-100 dark:border-slate-700 bg-white/50 dark:bg-slate-900/50 backdrop-blur">
                <h3 className="text-lg font-bold mb-3 flex items-center gap-2">
                  <CheckCircle2 className="w-5 h-5 text-green-600 dark:text-green-400" />
                  Pros
                </h3>
                <ul className="space-y-2">
                  {analysis.pros?.map((pro: string, idx: number) => (
                    <li key={idx} className="text-sm text-foreground/70 flex items-start gap-2">
                      <span className="text-green-600 dark:text-green-400 mt-0.5">•</span>
                      {pro}
                    </li>
                  ))}
                </ul>
              </Card>

              <Card className="p-6 border-green-100 dark:border-slate-700 bg-white/50 dark:bg-slate-900/50 backdrop-blur">
                <h3 className="text-lg font-bold mb-3 flex items-center gap-2">
                  <AlertCircle className="w-5 h-5 text-red-600 dark:text-red-400" />
                  Cons
                </h3>
                <ul className="space-y-2">
                  {analysis.cons?.map((con: string, idx: number) => (
                    <li key={idx} className="text-sm text-foreground/70 flex items-start gap-2">
                      <span className="text-red-600 dark:text-red-400 mt-0.5">•</span>
                      {con}
                    </li>
                  ))}
                </ul>
              </Card>
            </div>

            {/* Risks */}
            {analysis.risks && analysis.risks.length > 0 && (
              <Card className="p-6 border-green-100 dark:border-slate-700 bg-white/50 dark:bg-slate-900/50 backdrop-blur">
                <h3 className="text-lg font-bold mb-3">Risk Factors</h3>
                <div className="space-y-3">
                  {analysis.risks.map((risk: any, idx: number) => (
                    <div key={idx} className="p-3 bg-red-50 dark:bg-red-900/20 rounded-lg border border-red-100 dark:border-red-900/50">
                      <div className="flex items-start justify-between mb-1">
                        <span className="font-semibold text-sm">{risk.description}</span>
                        <span className={`text-xs px-2 py-0.5 rounded-full ${
                          risk.impact === 'HIGH' ? 'bg-red-200 dark:bg-red-900 text-red-900 dark:text-red-200' :
                          risk.impact === 'MEDIUM' ? 'bg-yellow-200 dark:bg-yellow-900 text-yellow-900 dark:text-yellow-200' :
                          'bg-green-200 dark:bg-green-900 text-green-900 dark:text-green-200'
                        }`}>
                          {risk.impact} Impact
                        </span>
                      </div>
                      {risk.mitigation && (
                        <p className="text-xs text-foreground/60">Mitigation: {risk.mitigation}</p>
                      )}
                    </div>
                  ))}
                </div>
              </Card>
            )}

            {/* Recommendations */}
            {analysis.recommendations && analysis.recommendations.length > 0 && (
              <Card className="p-6 border-green-100 dark:border-slate-700 bg-white/50 dark:bg-slate-900/50 backdrop-blur">
                <h3 className="text-lg font-bold mb-3 flex items-center gap-2">
                  <TrendingUp className="w-5 h-5 text-green-600 dark:text-green-400" />
                  Recommendations
                </h3>
                <ul className="space-y-2">
                  {analysis.recommendations.map((rec: string, idx: number) => (
                    <li key={idx} className="text-sm text-foreground/70 flex items-start gap-2">
                      <span className="text-green-600 dark:text-green-400 mt-0.5">→</span>
                      {rec}
                    </li>
                  ))}
                </ul>
              </Card>
            )}

            {/* Q&A Section */}
            <Card className="p-6 border-green-100 dark:border-slate-700 bg-white/50 dark:bg-slate-900/50 backdrop-blur">
              <h3 className="text-lg font-bold mb-3">Ask Questions About This Decision</h3>
              <p className="text-sm text-foreground/60 mb-4">
                Ask decision-related questions only. Off-topic queries will be rejected.
              </p>
              <div className="flex gap-2">
                <input
                  type="text"
                  value={question}
                  onChange={(e) => setQuestion(e.target.value)}
                  onKeyPress={(e) => e.key === 'Enter' && askQuestion()}
                  placeholder="e.g., What if we extend the timeline by 2 months?"
                  className="flex-1 px-4 py-2 text-sm bg-white dark:bg-slate-800 border border-green-200 dark:border-slate-700 rounded-lg focus:ring-2 focus:ring-green-500 focus:border-transparent"
                  disabled={querying}
                />
                <Button 
                  onClick={askQuestion}
                  disabled={querying || !question.trim()}
                  className="bg-gradient-to-r from-green-500 to-emerald-600 hover:from-green-600 hover:to-emerald-700"
                >
                  {querying ? (
                    <div className="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></div>
                  ) : (
                    <Send className="w-4 h-4" />
                  )}
                </Button>
              </div>
              {queryResponse && (
                <div className="mt-4 p-4 bg-green-50 dark:bg-green-900/20 rounded-lg border border-green-100 dark:border-green-900/50">
                  <p className="text-sm">{queryResponse}</p>
                </div>
              )}
            </Card>
          </>
        )}
      </div>
    </main>
  )
}
