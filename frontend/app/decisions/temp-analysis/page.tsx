"use client"

import { useEffect, useState, useRef } from "react"
import { useRouter } from "next/navigation"
import Link from "next/link"
import { 
  ChevronLeft, Sparkles, Save, AlertTriangle, CheckCircle, XCircle, 
  Download, TrendingUp, TrendingDown, Clock, Target, Users, Zap,
  FileText, BarChart3, Calendar, ArrowRight, Lightbulb, Shield, Eye, Star
} from "lucide-react"
import { Button } from "@/components/ui/button"
import { Card, CardHeader, CardContent } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { useToast } from "@/hooks/use-toast"
import { useAuth } from "@/lib/auth-context"
import { Progress } from "@/components/ui/progress"
import jsPDF from 'jspdf'
import html2canvas from 'html2canvas'

interface TempAnalysisData {
  input: string
  analysis: {
    // Basic metrics
    feasibilityScore: number
    feasibilityVerdict?: string
    confidence: number
    domainType?: string
    
    // Main content
    reasoning: string
    executiveSummary?: string
    
    // Analysis sections
    currentPlanAnalysis?: {
      timelineAssessment?: string
      scopeAssessment?: string
      budgetAssessment?: string
      resourceAssessment?: string
    }
    
    // Pros and Cons
    pros?: string[]
    cons?: string[]
    
    // Optimized solution
    optimizedSolution?: {
      improvedTimeline?: string
      clarifiedScope?: string
      budgetOptimization?: string
      resourceStrategy?: string
      successProbability?: number
    }
    
    optimizedPros?: string[]
    optimizedCons?: string[]
    
    // Risks with full detail
    risks?: Array<{
      description: string
      impact: string
      mitigation: string
    }>
    
    // Additional data
    assumptions?: string[]
    recommendations?: string[]
    
    // Metadata
    timestamp: string
    modelUsed?: string
  }
  timestamp: number
}

export default function TempAnalysisPage() {
  const router = useRouter()
  const { toast } = useToast()
  const { user } = useAuth()
  const [analysisData, setAnalysisData] = useState<TempAnalysisData | null>(null)
  const [saving, setSaving] = useState(false)
  const [exporting, setExporting] = useState(false)
  const analysisRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    let mounted = true
    
    // Load temporary analysis from localStorage
    const tempData = localStorage.getItem('tempAnalysis')
    if (tempData) {
      try {
        const parsed = JSON.parse(tempData)
        // Check if data is not too old (1 hour)
        if (Date.now() - parsed.timestamp < 3600000) {
          if (mounted) {
            setAnalysisData(parsed)
          }
        } else {
          // Data is too old, clean up and redirect
          localStorage.removeItem('tempAnalysis')
          if (mounted) {
            toast({
              title: "Session Expired",
              description: "The temporary analysis has expired. Please create a new one.",
              variant: "destructive"
            })
            router.push('/decisions/new')
          }
        }
      } catch (error) {
        console.error('Failed to parse temp analysis:', error)
        localStorage.removeItem('tempAnalysis')
        if (mounted) {
          toast({
            title: "Invalid Data",
            description: "The temporary analysis data is corrupted. Please create a new analysis.",
            variant: "destructive"
          })
          router.push('/decisions/new')
        }
      }
    } else {
      if (mounted) {
        toast({
          title: "No Data Found",
          description: "No temporary analysis found. Please analyze a decision first.",
          variant: "destructive"
        })
        router.push('/decisions/new')
      }
    }
    
    return () => {
      mounted = false
    }
  }, [router, toast])

  const handleSaveDecision = async () => {
    if (!analysisData || !user) {
      return
    }

    setSaving(true)
    try {
      // Save the decision with analysis
      const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/v2/decisions`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${localStorage.getItem('token')}`
        },
        body: JSON.stringify({
          input: analysisData.input,
          createdBy: user?.email || 'user@example.com'
        })
      })

      if (!response.ok) {
        throw new Error('Failed to save decision')
      }

      const result = await response.json()
      
      // Clear temporary data
      localStorage.removeItem('tempAnalysis')
      
      toast({
        title: "Decision Saved!",
        description: "Your decision and analysis have been saved to your history.",
        variant: "default"
      })
      
      router.push(`/decisions/${result.id}/analysis`)
    } catch (error) {
      console.error('Failed to save decision:', error)
      toast({
        title: "Save Failed",
        description: "Failed to save decision. Please try again.",
        variant: "destructive"
      })
    } finally {
      setSaving(false)
    }
  }

  const handleDiscardAndNew = () => {
    try {
      localStorage.removeItem('tempAnalysis')
      router.push('/decisions/new')
    } catch (error) {
      console.error('Failed to clear temp data:', error)
      // Still redirect even if localStorage cleanup fails
      router.push('/decisions/new')
    }
  }

  const exportToPDF = async () => {
    if (!analysisData || !analysisRef.current) {
      toast({
        title: "Export Failed", 
        description: "No analysis data to export.",
        variant: "destructive"
      })
      return
    }

    setExporting(true)
    try {
      const element = analysisRef.current
      
      // Ensure the element is visible and has content
      if (element.offsetHeight === 0) {
        throw new Error('Analysis content is not visible or empty')
      }
      
      const canvas = await html2canvas(element, {
        scale: 1.5, // Reduced scale for better performance
        useCORS: true,
        allowTaint: true,
        backgroundColor: '#ffffff',
        logging: false, // Disable logging for cleaner output
        foreignObjectRendering: true
      })
      
      if (canvas.width === 0 || canvas.height === 0) {
        throw new Error('Failed to capture content - canvas is empty')
      }
      
      const imgData = canvas.toDataURL('image/png')
      const pdf = new jsPDF('p', 'mm', 'a4')
      
      // Calculate dimensions to fit the page
      const imgWidth = 210 - 20 // A4 width minus margins
      const pageHeight = 295 - 20 // A4 height minus margins
      const imgHeight = (canvas.height * imgWidth) / canvas.width
      let heightLeft = imgHeight

      let position = 10

      // Add first page
      pdf.addImage(imgData, 'PNG', 10, position, imgWidth, imgHeight)
      heightLeft -= pageHeight

      // Add additional pages if needed
      while (heightLeft >= 0) {
        position = heightLeft - imgHeight + 10
        pdf.addPage()
        pdf.addImage(imgData, 'PNG', 10, position, imgWidth, imgHeight)
        heightLeft -= pageHeight
      }

      // Add header with decision info
      pdf.setPage(1)
      pdf.setFontSize(16)
      pdf.setTextColor(0, 0, 0)
      pdf.text('Decision Analysis Report', 10, 5)
      
      pdf.setFontSize(10)
      pdf.text(`Generated: ${new Date().toLocaleDateString()}`, 150, 5)
      pdf.text(`User: ${user?.name || 'Unknown'}`, 10, 8)
      
      // Save the PDF
      pdf.save(`decision-analysis-${new Date().toISOString().split('T')[0]}.pdf`)
      
      toast({
        title: "PDF Exported!",
        description: "Your decision analysis has been downloaded.",
        variant: "default"
      })
    } catch (error) {
      console.error('PDF export failed:', error)
      toast({
        title: "Export Failed",
        description: "Unable to export PDF. Please try again.",
        variant: "destructive"
      })
    } finally {
      setExporting(false)
    }
  }

  // Timeline visualization component
  const TimelineVisualization = ({ 
    currentScore, 
    optimizedScore, 
    timeline, 
    successStages 
  }: { 
    currentScore: number
    optimizedScore?: number
    timeline?: string
    successStages?: Array<{ phase: string; success: number; duration: string }>
  }) => {
    const stages = successStages || [
      { phase: "Current Plan", success: currentScore, duration: "Now" },
      { phase: "Initial Phase", success: Math.min(currentScore + 20, 100), duration: "Month 1-2" },
      { phase: "Implementation", success: Math.min(currentScore + 40, 100), duration: "Month 3-4" },
      { phase: "Optimization", success: optimizedScore || 85, duration: "Month 5-6" },
      { phase: "Success Target", success: 95, duration: "Final Goal" }
    ]

    return (
      <Card className="border-blue-200 dark:border-blue-800">
        <CardHeader>
          <h3 className="font-semibold flex items-center gap-2 text-blue-700 dark:text-blue-300">
            <BarChart3 className="w-5 h-5" />
            Success Timeline Projection
          </h3>
        </CardHeader>
        <CardContent>
          <div className="space-y-6">
            {stages.map((stage, idx) => (
              <div key={idx} className="relative">
                <div className="flex items-center justify-between mb-2">
                  <div className="flex items-center gap-3">
                    <div className={`w-3 h-3 rounded-full ${
                      idx === 0 ? 'bg-red-500' :
                      idx === stages.length - 1 ? 'bg-green-500' : 'bg-blue-500'
                    }`} />
                    <span className="font-medium text-sm">{stage.phase}</span>
                    <span className="text-xs text-foreground/60">{stage.duration}</span>
                  </div>
                  <Badge variant={stage.success >= 80 ? "default" : stage.success >= 60 ? "secondary" : "destructive"}>
                    {stage.success}%
                  </Badge>
                </div>
                <div className="ml-6 mb-4">
                  <Progress value={stage.success} className="h-3" />
                </div>
                {idx < stages.length - 1 && (
                  <div className="absolute left-1.5 top-8 w-0.5 h-8 bg-gradient-to-b from-gray-300 to-transparent" />
                )}
              </div>
            ))}
          </div>
        </CardContent>
      </Card>
    )
  }

  if (!analysisData) {
    return (
      <main className="min-h-screen bg-gradient-to-br from-white to-green-50 dark:from-slate-950 dark:to-slate-900">
        <div className="flex items-center justify-center h-screen">
          <div className="text-center">
            <div className="w-8 h-8 border-4 border-green-600 border-t-transparent rounded-full animate-spin mx-auto mb-4"></div>
            <p className="text-foreground/60">Loading analysis...</p>
          </div>
        </div>
      </main>
    )
  }

  return (
    <main className="min-h-screen bg-gradient-to-br from-white to-green-50 dark:from-slate-950 dark:to-slate-900">
      {/* Header */}
      <div className="bg-white/80 dark:bg-slate-950/80 backdrop-blur border-b border-green-100 dark:border-slate-800">
        <div className="max-w-4xl mx-auto px-6 py-4 flex items-center gap-4">
          <Link href="/decisions/new">
            <Button variant="ghost" size="sm">
              <ChevronLeft className="w-4 h-4 mr-2" />
              Back to New Decision
            </Button>
          </Link>
          <div className="flex-1">
            <h1 className="text-2xl font-bold">Decision Analysis</h1>
            <p className="text-sm text-foreground/60">Temporary analysis - save to keep in your decision history</p>
          </div>
          <div className="flex items-center gap-2 text-sm text-amber-600 dark:text-amber-400">
            <AlertTriangle className="w-4 h-4" />
            <span>Temporary</span>
          </div>
        </div>
      </div>

      <div className="max-w-4xl mx-auto px-6 py-8">
        {/* Action Buttons */}
        <div className="flex flex-wrap gap-3 mb-8">
          <Button 
            onClick={handleSaveDecision}
            disabled={saving}
            className="bg-gradient-to-r from-green-500 to-emerald-600 hover:from-green-600 hover:to-emerald-700 text-white font-medium"
          >
            {saving ? (
              <>
                <div className="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin mr-2"></div>
                Saving...
              </>
            ) : (
              <>
                <Save className="w-4 h-4 mr-2" />
                Save to History
              </>
            )}
          </Button>
          
          <Button 
            onClick={exportToPDF}
            disabled={exporting}
            variant="outline"
            className="border-blue-500 text-blue-600 hover:bg-blue-50 dark:hover:bg-blue-950 dark:border-blue-400 dark:text-blue-400"
          >
            {exporting ? (
              <>
                <div className="w-4 h-4 border-2 border-blue-600 border-t-transparent rounded-full animate-spin mr-2"></div>
                Exporting...
              </>
            ) : (
              <>
                <Download className="w-4 h-4 mr-2" />
                Export PDF
              </>
            )}
          </Button>
          
          <Button 
            onClick={handleDiscardAndNew}
            variant="outline"
            className="border-gray-300 text-gray-700 hover:bg-gray-50 dark:border-gray-600 dark:text-gray-300 dark:hover:bg-gray-800"
          >
            Discard & Create New
          </Button>
          <Link href="/decisions">
            <Button variant="outline" className="border-gray-300 text-gray-700 hover:bg-gray-50 dark:border-gray-600 dark:text-gray-300 dark:hover:bg-gray-800">
              View All Decisions
            </Button>
          </Link>
        </div>

        {/* Decision Input */}
        <Card className="p-6 mb-6 border-green-100 dark:border-slate-700 bg-white/50 dark:bg-slate-900/50 backdrop-blur">
          <h2 className="text-lg font-semibold mb-3">Your Decision</h2>
          <p className="text-foreground/80 text-sm leading-relaxed whitespace-pre-wrap">
            {analysisData.input}
          </p>
        </Card>

        {/* Analysis Content - Exported to PDF */}
        <div ref={analysisRef} className="space-y-8 bg-white dark:bg-slate-900 p-6 rounded-lg">
          
          {/* Header Section */}
          <div className="text-center border-b pb-6">
            <h1 className="text-3xl font-bold bg-gradient-to-r from-blue-600 to-purple-600 bg-clip-text text-transparent">
              AI Decision Analysis Report
            </h1>
            <p className="text-foreground/60 mt-2">
              Comprehensive analysis powered by Gemini AI
            </p>
            <div className="flex justify-center gap-4 mt-4 text-sm">
              <span>Generated: {new Date().toLocaleDateString()}</span>
              {analysisData.analysis.domainType && (
                <span>Domain: {analysisData.analysis.domainType}</span>
              )}
            </div>
          </div>

          {/* Decision Input */}
          <Card className="border-slate-200 dark:border-slate-700">
            <CardHeader>
              <h2 className="text-xl font-semibold flex items-center gap-2">
                <FileText className="w-5 h-5" />
                Your Original Decision
              </h2>
            </CardHeader>
            <CardContent>
              <div className="bg-slate-50 dark:bg-slate-800 p-4 rounded-lg border-l-4 border-blue-500">
                <p className="text-foreground/80 leading-relaxed whitespace-pre-wrap">
                  {analysisData.input}
                </p>
              </div>
            </CardContent>
          </Card>

          {/* Key Metrics Dashboard */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            <Card className="text-center bg-gradient-to-br from-red-50 to-orange-50 dark:from-red-950/30 dark:to-orange-950/30 border-red-200 dark:border-red-800">
              <CardContent className="pt-6">
                <div className="text-4xl font-bold text-red-600 dark:text-red-400">
                  {Math.round(analysisData.analysis.feasibilityScore)}%
                </div>
                <p className="text-sm font-medium text-red-700 dark:text-red-300">Current Plan Feasibility</p>
                <p className="text-xs text-foreground/60 mt-1">
                  {analysisData.analysis.feasibilityVerdict?.replace('_', ' ') || 'Needs Improvement'}
                </p>
              </CardContent>
            </Card>

            {analysisData.analysis.optimizedSolution?.successProbability && (
              <Card className="text-center bg-gradient-to-br from-green-50 to-emerald-50 dark:from-green-950/30 dark:to-emerald-950/30 border-green-200 dark:border-green-800">
                <CardContent className="pt-6">
                  <div className="text-4xl font-bold text-green-600 dark:text-green-400">
                    {analysisData.analysis.optimizedSolution.successProbability}%
                  </div>
                  <p className="text-sm font-medium text-green-700 dark:text-green-300">Optimized Plan Success Rate</p>
                  <div className="flex items-center justify-center mt-2">
                    <TrendingUp className="w-4 h-4 text-green-600 mr-1" />
                    <span className="text-xs text-green-600">
                      +{analysisData.analysis.optimizedSolution.successProbability - Math.round(analysisData.analysis.feasibilityScore)}% improvement
                    </span>
                  </div>
                </CardContent>
              </Card>
            )}

            <Card className="text-center bg-gradient-to-br from-blue-50 to-indigo-50 dark:from-blue-950/30 dark:to-indigo-950/30 border-blue-200 dark:border-blue-800">
              <CardContent className="pt-6">
                <div className="text-4xl font-bold text-blue-600 dark:text-blue-400">
                  {Math.round(analysisData.analysis.confidence * 100)}%
                </div>
                <p className="text-sm font-medium text-blue-700 dark:text-blue-300">AI Confidence Level</p>
                <p className="text-xs text-foreground/60 mt-1">
                  {analysisData.analysis.modelUsed || 'Gemini 2.5 Flash'}
                </p>
              </CardContent>
            </Card>
          </div>

          {/* Timeline Visualization */}
          <TimelineVisualization 
            currentScore={analysisData.analysis.feasibilityScore}
            optimizedScore={analysisData.analysis.optimizedSolution?.successProbability}
            timeline={analysisData.analysis.optimizedSolution?.improvedTimeline}
          />

          {/* Executive Summary */}
          {(analysisData.analysis.executiveSummary || analysisData.analysis.reasoning) ? (
            <Card className="border-indigo-200 dark:border-indigo-800">
              <CardHeader>
                <h2 className="text-xl font-semibold flex items-center gap-2 text-indigo-700 dark:text-indigo-300">
                  <Sparkles className="w-5 h-5" />
                  Executive Summary
                </h2>
              </CardHeader>
              <CardContent>
                <div className="bg-indigo-50 dark:bg-indigo-950/30 p-6 rounded-lg border-l-4 border-indigo-500">
                  <p className="text-foreground/80 leading-relaxed">
                    {analysisData.analysis.executiveSummary || analysisData.analysis.reasoning}
                  </p>
                </div>
              </CardContent>
            </Card>
          ) : (
            <Card className="border-yellow-200 dark:border-yellow-800">
              <CardHeader>
                <h2 className="text-xl font-semibold flex items-center gap-2 text-yellow-700 dark:text-yellow-300">
                  <AlertTriangle className="w-5 h-5" />
                  Gemini API Status
                </h2>
              </CardHeader>
              <CardContent>
                <div className="bg-yellow-50 dark:bg-yellow-950/30 p-6 rounded-lg border-l-4 border-yellow-500">
                  <p className="text-foreground/80 leading-relaxed mb-3">
                    ⚠️ <strong>Gemini API is currently overloaded (HTTP 503)</strong>
                  </p>
                  <p className="text-sm text-foreground/70 mb-3">
                    Google's Gemini service is temporarily unavailable due to high demand. This typically resolves within 10-15 minutes.
                  </p>
                  <div className="bg-white dark:bg-slate-800 p-4 rounded border-l-2 border-blue-500">
                    <p className="text-sm font-medium text-blue-700 dark:text-blue-300 mb-2">✅ What's working:</p>
                    <ul className="list-disc list-inside text-sm text-foreground/70 space-y-1">
                      <li>Save to History functionality</li>
                      <li>PDF Export (will export current content)</li>
                      <li>Backend API and authentication</li>
                    </ul>
                  </div>
                  <p className="text-sm text-foreground/60 mt-3">
                    💡 <strong>Next steps:</strong> Try creating a new analysis in 10-15 minutes, or save this decision and analyze it later.
                  </p>
                </div>
              </CardContent>
            </Card>
          )}

          {/* SECTION 1: Current Plan Analysis */}
          <Card className="border-red-200 dark:border-red-800">
            <CardHeader>
              <h2 className="text-xl font-semibold flex items-center gap-2 text-red-700 dark:text-red-300">
                <AlertTriangle className="w-6 h-6" />
                Current Plan Assessment
              </h2>
              <p className="text-sm text-foreground/60">Analysis of your original decision approach</p>
            </CardHeader>
            <CardContent className="space-y-6">
              
              {/* Current Plan Detailed Analysis */}
              {analysisData.analysis.currentPlanAnalysis && (
                <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                  {analysisData.analysis.currentPlanAnalysis.timelineAssessment && (
                    <div className="bg-red-50 dark:bg-red-950/30 p-4 rounded-lg">
                      <h4 className="font-medium text-red-700 dark:text-red-300 mb-2 flex items-center gap-2">
                        <Clock className="w-4 h-4" /> Timeline Assessment
                      </h4>
                      <p className="text-sm text-foreground/80">{analysisData.analysis.currentPlanAnalysis.timelineAssessment}</p>
                    </div>
                  )}
                  {analysisData.analysis.currentPlanAnalysis.scopeAssessment && (
                    <div className="bg-red-50 dark:bg-red-950/30 p-4 rounded-lg">
                      <h4 className="font-medium text-red-700 dark:text-red-300 mb-2 flex items-center gap-2">
                        <Target className="w-4 h-4" /> Scope Assessment
                      </h4>
                      <p className="text-sm text-foreground/80">{analysisData.analysis.currentPlanAnalysis.scopeAssessment}</p>
                    </div>
                  )}
                  {analysisData.analysis.currentPlanAnalysis.budgetAssessment && (
                    <div className="bg-red-50 dark:bg-red-950/30 p-4 rounded-lg">
                      <h4 className="font-medium text-red-700 dark:text-red-300 mb-2 flex items-center gap-2">
                        <BarChart3 className="w-4 h-4" /> Budget Assessment
                      </h4>
                      <p className="text-sm text-foreground/80">{analysisData.analysis.currentPlanAnalysis.budgetAssessment}</p>
                    </div>
                  )}
                  {analysisData.analysis.currentPlanAnalysis.resourceAssessment && (
                    <div className="bg-red-50 dark:bg-red-950/30 p-4 rounded-lg">
                      <h4 className="font-medium text-red-700 dark:text-red-300 mb-2 flex items-center gap-2">
                        <Users className="w-4 h-4" /> Resource Assessment
                      </h4>
                      <p className="text-sm text-foreground/80">{analysisData.analysis.currentPlanAnalysis.resourceAssessment}</p>
                    </div>
                  )}
                </div>
              )}

              {/* Current Plan Pros and Cons */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                {analysisData.analysis.pros && analysisData.analysis.pros.length > 0 && (
                  <div>
                    <h3 className="font-semibold text-green-700 dark:text-green-300 mb-3 flex items-center gap-2">
                      <CheckCircle className="w-5 h-5" />
                      Current Plan Advantages
                    </h3>
                    <div className="space-y-2">
                      {analysisData.analysis.pros.map((pro: string, idx: number) => (
                        <div key={idx} className="flex items-start gap-2 bg-green-50 dark:bg-green-950/30 p-3 rounded-lg">
                          <div className="w-1.5 h-1.5 bg-green-500 rounded-full mt-2 flex-shrink-0" />
                          <p className="text-sm text-foreground/80">{pro}</p>
                        </div>
                      ))}
                    </div>
                  </div>
                )}
                
                {analysisData.analysis.cons && analysisData.analysis.cons.length > 0 && (
                  <div>
                    <h3 className="font-semibold text-red-700 dark:text-red-300 mb-3 flex items-center gap-2">
                      <XCircle className="w-5 h-5" />
                      Current Plan Issues
                    </h3>
                    <div className="space-y-2">
                      {analysisData.analysis.cons.map((con: string, idx: number) => (
                        <div key={idx} className="flex items-start gap-2 bg-red-50 dark:bg-red-950/30 p-3 rounded-lg">
                          <div className="w-1.5 h-1.5 bg-red-500 rounded-full mt-2 flex-shrink-0" />
                          <p className="text-sm text-foreground/80">{con}</p>
                        </div>
                      ))}
                    </div>
                  </div>
                )}
              </div>
            </CardContent>
          </Card>

          {/* SECTION 2: Path to 100% Success */}
          <Card className="border-green-200 dark:border-green-800">
            <CardHeader>
              <h2 className="text-xl font-semibold flex items-center gap-2 text-green-700 dark:text-green-300">
                <Zap className="w-6 h-6" />
                Path to 100% Success
              </h2>
              <p className="text-sm text-foreground/60">Optimized strategy for maximum success probability</p>
            </CardHeader>
            <CardContent className="space-y-6">
              
              {/* Optimized Solution Details */}
              {analysisData.analysis.optimizedSolution && (
                <div className="bg-green-50 dark:bg-green-950/30 p-6 rounded-lg border-l-4 border-green-500">
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                    {analysisData.analysis.optimizedSolution.improvedTimeline && (
                      <div>
                        <h4 className="font-medium text-green-700 dark:text-green-300 mb-2 flex items-center gap-2">
                          <Calendar className="w-4 h-4" /> Optimized Timeline
                        </h4>
                        <p className="text-sm text-foreground/80">{analysisData.analysis.optimizedSolution.improvedTimeline}</p>
                      </div>
                    )}
                    {analysisData.analysis.optimizedSolution.clarifiedScope && (
                      <div>
                        <h4 className="font-medium text-green-700 dark:text-green-300 mb-2 flex items-center gap-2">
                          <Target className="w-4 h-4" /> Clarified Scope
                        </h4>
                        <p className="text-sm text-foreground/80">{analysisData.analysis.optimizedSolution.clarifiedScope}</p>
                      </div>
                    )}
                    {analysisData.analysis.optimizedSolution.budgetOptimization && (
                      <div>
                        <h4 className="font-medium text-green-700 dark:text-green-300 mb-2 flex items-center gap-2">
                          <BarChart3 className="w-4 h-4" /> Budget Strategy
                        </h4>
                        <p className="text-sm text-foreground/80">{analysisData.analysis.optimizedSolution.budgetOptimization}</p>
                      </div>
                    )}
                    {analysisData.analysis.optimizedSolution.resourceStrategy && (
                      <div>
                        <h4 className="font-medium text-green-700 dark:text-green-300 mb-2 flex items-center gap-2">
                          <Users className="w-4 h-4" /> Resource Strategy
                        </h4>
                        <p className="text-sm text-foreground/80">{analysisData.analysis.optimizedSolution.resourceStrategy}</p>
                      </div>
                    )}
                  </div>
                </div>
              )}

              {/* Optimized Plan Pros and Cons */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                {analysisData.analysis.optimizedPros && analysisData.analysis.optimizedPros.length > 0 && (
                  <div>
                    <h3 className="font-semibold text-emerald-700 dark:text-emerald-300 mb-3 flex items-center gap-2">
                      <CheckCircle className="w-5 h-5" />
                      Optimized Plan Benefits
                    </h3>
                    <div className="space-y-2">
                      {analysisData.analysis.optimizedPros.map((pro: string, idx: number) => (
                        <div key={idx} className="flex items-start gap-2 bg-emerald-50 dark:bg-emerald-950/30 p-3 rounded-lg">
                          <div className="w-1.5 h-1.5 bg-emerald-500 rounded-full mt-2 flex-shrink-0" />
                          <p className="text-sm text-foreground/80">{pro}</p>
                        </div>
                      ))}
                    </div>
                  </div>
                )}
                
                {analysisData.analysis.optimizedCons && analysisData.analysis.optimizedCons.length > 0 && (
                  <div>
                    <h3 className="font-semibold text-orange-700 dark:text-orange-300 mb-3 flex items-center gap-2">
                      <AlertTriangle className="w-5 h-5" />
                      Trade-offs & Considerations
                    </h3>
                    <div className="space-y-2">
                      {analysisData.analysis.optimizedCons.map((con: string, idx: number) => (
                        <div key={idx} className="flex items-start gap-2 bg-orange-50 dark:bg-orange-950/30 p-3 rounded-lg">
                          <div className="w-1.5 h-1.5 bg-orange-500 rounded-full mt-2 flex-shrink-0" />
                          <p className="text-sm text-foreground/80">{con}</p>
                        </div>
                      ))}
                    </div>
                  </div>
                )}
              </div>
            </CardContent>
          </Card>

          {/* Implementation Recommendations */}
          {analysisData.analysis.recommendations && analysisData.analysis.recommendations.length > 0 && (
            <Card className="border-blue-200 dark:border-blue-800">
              <CardHeader>
                <h2 className="text-xl font-semibold flex items-center gap-2 text-blue-700 dark:text-blue-300">
                  <Lightbulb className="w-6 h-6" />
                  Implementation Recommendations
                </h2>
                <p className="text-sm text-foreground/60">Actionable steps to achieve success</p>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  {analysisData.analysis.recommendations.map((rec: string, idx: number) => (
                    <div key={idx} className="flex items-start gap-4 p-4 bg-blue-50 dark:bg-blue-950/30 rounded-lg border border-blue-200 dark:border-blue-800">
                      <div className="w-8 h-8 bg-blue-100 dark:bg-blue-900 text-blue-700 dark:text-blue-300 rounded-full flex items-center justify-center text-sm font-bold flex-shrink-0">
                        {idx + 1}
                      </div>
                      <p className="text-sm text-foreground/80 leading-relaxed">{rec}</p>
                    </div>
                  ))}
                </div>
              </CardContent>
            </Card>
          )}

          {/* Risk Analysis & Mitigation */}
          {analysisData.analysis.risks && analysisData.analysis.risks.length > 0 && (
            <Card className="border-amber-200 dark:border-amber-800">
              <CardHeader>
                <h2 className="text-xl font-semibold flex items-center gap-2 text-amber-700 dark:text-amber-300">
                  <Shield className="w-6 h-6" />
                  Risk Analysis & Mitigation
                </h2>
                <p className="text-sm text-foreground/60">Potential challenges and how to address them</p>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  {analysisData.analysis.risks.map((risk: any, idx: number) => {
                    const riskData = typeof risk === 'string' ? 
                      { description: risk, impact: 'MEDIUM', mitigation: null } : 
                      risk;
                    
                    return (
                      <div key={idx} className="p-4 bg-amber-50 dark:bg-amber-950/30 rounded-lg border border-amber-200 dark:border-amber-800">
                        <div className="flex items-start gap-3">
                          <div className="flex-shrink-0">
                            <div className={`w-8 h-8 rounded-full flex items-center justify-center text-sm font-bold ${
                              riskData.impact === 'HIGH' ? 'bg-red-100 text-red-700 dark:bg-red-900 dark:text-red-300' :
                              riskData.impact === 'MEDIUM' ? 'bg-amber-100 text-amber-700 dark:bg-amber-900 dark:text-amber-300' :
                              'bg-green-100 text-green-700 dark:bg-green-900 dark:text-green-300'
                            }`}>
                              {idx + 1}
                            </div>
                          </div>
                          <div className="flex-1">
                            <div className="flex items-center gap-3 mb-3">
                              <h4 className="font-semibold text-amber-700 dark:text-amber-300">
                                Risk #{idx + 1}
                              </h4>
                              <Badge 
                                variant={riskData.impact === 'HIGH' ? 'destructive' : riskData.impact === 'MEDIUM' ? 'secondary' : 'default'}
                                className="text-xs"
                              >
                                {riskData.impact} IMPACT
                              </Badge>
                            </div>
                            <p className="text-sm text-foreground/80 mb-3 leading-relaxed">{riskData.description}</p>
                            {riskData.mitigation && (
                              <div className="bg-white/70 dark:bg-slate-800/50 p-3 rounded border border-amber-300 dark:border-amber-700">
                                <h5 className="font-medium text-xs text-amber-700 dark:text-amber-300 mb-2 flex items-center gap-2">
                                  <Shield className="w-3 h-3" />
                                  Mitigation Strategy
                                </h5>
                                <p className="text-xs text-foreground/70">{riskData.mitigation}</p>
                              </div>
                            )}
                          </div>
                        </div>
                      </div>
                    );
                  })}
                </div>
              </CardContent>
            </Card>
          )}

          {/* Key Assumptions */}
          {analysisData.analysis.assumptions && analysisData.analysis.assumptions.length > 0 && (
            <Card className="border-slate-200 dark:border-slate-700">
              <CardHeader>
                <h2 className="text-xl font-semibold flex items-center gap-2 text-slate-700 dark:text-slate-300">
                  <Eye className="w-6 h-6" />
                  Key Assumptions
                </h2>
                <p className="text-sm text-foreground/60">Important assumptions underlying this analysis</p>
              </CardHeader>
              <CardContent>
                <div className="space-y-3">
                  {analysisData.analysis.assumptions.map((assumption: string, idx: number) => (
                    <div key={idx} className="flex items-start gap-3 p-4 bg-slate-50 dark:bg-slate-800/50 rounded-lg border border-slate-200 dark:border-slate-700">
                      <div className="w-6 h-6 bg-slate-100 dark:bg-slate-700 text-slate-700 dark:text-slate-300 rounded-full flex items-center justify-center text-xs font-bold flex-shrink-0 mt-0.5">
                        {idx + 1}
                      </div>
                      <p className="text-sm text-foreground/80 leading-relaxed">{assumption}</p>
                    </div>
                  ))}
                </div>
              </CardContent>
            </Card>
          )}

          {/* Final Analysis Summary */}
          <Card className="border-purple-200 dark:border-purple-800">
            <CardHeader>
              <h2 className="text-xl font-semibold flex items-center gap-2 text-purple-700 dark:text-purple-300">
                <Star className="w-6 h-6" />
                Final Analysis Summary
              </h2>
            </CardHeader>
            <CardContent className="space-y-6">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                <div className="bg-purple-50 dark:bg-purple-950/30 p-4 rounded-lg">
                  <h4 className="font-semibold text-purple-700 dark:text-purple-300 mb-3">Analysis Confidence</h4>
                  <div className="flex items-center gap-3 mb-2">
                    <div className="flex-1">
                      <div className="h-3 bg-secondary rounded-full overflow-hidden">
                        <div
                          className="h-full bg-gradient-to-r from-purple-500 to-blue-500 transition-all duration-500"
                          style={{ width: `${Math.round(analysisData.analysis.confidence * 100)}%` }}
                        />
                      </div>
                    </div>
                    <span className="text-lg font-bold text-purple-600 dark:text-purple-400">
                      {Math.round(analysisData.analysis.confidence * 100)}%
                    </span>
                  </div>
                  <p className="text-xs text-foreground/60">
                    {Math.round(analysisData.analysis.confidence * 100) >= 90 ? 'Very High Confidence' :
                     Math.round(analysisData.analysis.confidence * 100) >= 70 ? 'High Confidence' :
                     Math.round(analysisData.analysis.confidence * 100) >= 50 ? 'Moderate Confidence' : 'Lower Confidence'}
                  </p>
                </div>
                
                <div className="bg-purple-50 dark:bg-purple-950/30 p-4 rounded-lg">
                  <h4 className="font-semibold text-purple-700 dark:text-purple-300 mb-3">Analysis Metadata</h4>
                  <div className="space-y-2 text-sm">
                    {analysisData.analysis.feasibilityVerdict && (
                      <div className="flex justify-between">
                        <span className="text-foreground/60">Overall Verdict:</span>
                        <Badge variant={analysisData.analysis.feasibilityVerdict === 'FEASIBLE' ? 'default' : 'secondary'}>
                          {analysisData.analysis.feasibilityVerdict.replace('_', ' ')}
                        </Badge>
                      </div>
                    )}
                    {analysisData.analysis.modelUsed && (
                      <div className="flex justify-between">
                        <span className="text-foreground/60">AI Model:</span>
                        <span className="font-medium text-foreground">{analysisData.analysis.modelUsed}</span>
                      </div>
                    )}
                    <div className="flex justify-between">
                      <span className="text-foreground/60">Generated:</span>
                      <span className="font-medium text-foreground text-xs">
                        {new Date().toLocaleDateString()} {new Date().toLocaleTimeString()}
                      </span>
                    </div>
                  </div>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Save Reminder */}
        <Card className="p-4 mt-6 border-amber-200 dark:border-amber-700 bg-amber-50/50 dark:bg-amber-900/20 backdrop-blur">
          <div className="flex items-center gap-3">
            <AlertTriangle className="w-5 h-5 text-amber-600 dark:text-amber-400" />
            <div>
              <h4 className="font-semibold text-sm text-amber-900 dark:text-amber-100">Temporary Analysis</h4>
              <p className="text-xs text-amber-700 dark:text-amber-300 mt-1">
                This analysis is temporary and will be lost when you close the browser. Click "Save to History" to permanently store this decision.
              </p>
            </div>
          </div>
        </Card>
      </div>
    </main>
  )
}