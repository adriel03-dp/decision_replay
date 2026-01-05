"use client"

import { useEffect, useState } from "react"
import { useRouter } from "next/navigation"
import Link from "next/link"
import { 
  BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer,
  PieChart, Pie, Cell, LineChart, Line, Legend, Area, AreaChart
} from 'recharts'
import { 
  TrendingUp, TrendingDown, BarChart3, PieChart as PieChartIcon,
  Activity, CheckCircle2, AlertTriangle, XCircle, Calendar,
  Target, Users, Brain, ArrowRight, Sparkles
} from "lucide-react"
import { Card, CardHeader, CardContent, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { useAuth } from "@/lib/auth-context"
import { useToast } from "@/hooks/use-toast"

interface DecisionV2 {
  id: string
  context: {
    summary: string
    intent: string
    priority: string
  }
  analysis?: {
    feasibilityScore: number
    confidenceLevel: number
    executiveSummary: string
    recommendations: string[]
    risks: Array<{ description: string; impact: string }>
  }
  status: string
  outcome: string
  createdAt: string
  domainType?: string
}

interface AnalyticsData {
  totalDecisions: number
  avgFeasibilityScore: number
  decisionsThisMonth: number
  topDomains: Array<{ name: string; count: number; color: string }>
  feasibilityDistribution: Array<{ range: string; count: number }>
  monthlyTrends: Array<{ month: string; decisions: number; avgScore: number }>
  statusBreakdown: Array<{ status: string; count: number; percentage: number }>
  riskAnalysis: Array<{ domain: string; avgRisks: number; highRiskCount: number }>
}

const DOMAIN_COLORS = {
  "Software Development": "#3b82f6",
  "Business Strategy": "#10b981", 
  "Finance": "#f59e0b",
  "Healthcare": "#ef4444",
  "Education": "#8b5cf6",
  "Construction": "#f97316",
  "Manufacturing": "#6b7280",
  "Personal Planning": "#ec4899",
  "General Planning": "#64748b"
}

export default function AnalyticsPage() {
  const router = useRouter()
  const { user } = useAuth()
  const { toast } = useToast()
  const [decisions, setDecisions] = useState<DecisionV2[]>([])
  const [analytics, setAnalytics] = useState<AnalyticsData | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    if (!user) {
      router.push('/login')
      return
    }
    fetchDecisions()
  }, [user, router])

  const fetchDecisions = async () => {
    try {
      const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/v2/decisions`, {
        method: 'GET',
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('token')}`,
          'Content-Type': 'application/json',
        }
      })

      if (!response.ok) {
        throw new Error('Failed to fetch decisions')
      }

      const data = await response.json()
      setDecisions(data)
      generateAnalytics(data)
    } catch (error) {
      console.error('Error fetching decisions:', error)
      toast({
        title: "Error",
        description: "Failed to load analytics data.",
        variant: "destructive"
      })
    } finally {
      setLoading(false)
    }
  }

  const generateAnalytics = (decisions: DecisionV2[]) => {
    // Basic stats
    const totalDecisions = decisions.length
    const decisionsWithScores = decisions.filter(d => d.analysis?.feasibilityScore)
    const avgFeasibilityScore = decisionsWithScores.length > 0
      ? Math.round(decisionsWithScores.reduce((sum, d) => sum + (d.analysis?.feasibilityScore || 0), 0) / decisionsWithScores.length)
      : 0

    // This month's decisions
    const thisMonth = new Date().toISOString().slice(0, 7) // YYYY-MM
    const decisionsThisMonth = decisions.filter(d => d.createdAt.startsWith(thisMonth)).length

    // Domain distribution
    const domainCounts: Record<string, number> = {}
    decisions.forEach(d => {
      const domain = d.domainType || 'General Planning'
      domainCounts[domain] = (domainCounts[domain] || 0) + 1
    })

    const topDomains = Object.entries(domainCounts)
      .map(([name, count]) => ({
        name,
        count,
        color: DOMAIN_COLORS[name as keyof typeof DOMAIN_COLORS] || '#64748b'
      }))
      .sort((a, b) => b.count - a.count)
      .slice(0, 6)

    // Feasibility distribution
    const feasibilityRanges = [
      { range: "0-20%", count: 0 },
      { range: "21-40%", count: 0 },
      { range: "41-60%", count: 0 },
      { range: "61-80%", count: 0 },
      { range: "81-100%", count: 0 }
    ]

    decisionsWithScores.forEach(d => {
      const score = d.analysis?.feasibilityScore || 0
      if (score <= 20) feasibilityRanges[0].count++
      else if (score <= 40) feasibilityRanges[1].count++
      else if (score <= 60) feasibilityRanges[2].count++
      else if (score <= 80) feasibilityRanges[3].count++
      else feasibilityRanges[4].count++
    })

    // Monthly trends (last 6 months)
    const monthlyTrends = []
    for (let i = 5; i >= 0; i--) {
      const date = new Date()
      date.setMonth(date.getMonth() - i)
      const month = date.toISOString().slice(0, 7)
      const monthDecisions = decisions.filter(d => d.createdAt.startsWith(month))
      const monthScores = monthDecisions.filter(d => d.analysis?.feasibilityScore)
      
      monthlyTrends.push({
        month: date.toLocaleDateString('en-US', { month: 'short', year: '2-digit' }),
        decisions: monthDecisions.length,
        avgScore: monthScores.length > 0 
          ? Math.round(monthScores.reduce((sum, d) => sum + (d.analysis?.feasibilityScore || 0), 0) / monthScores.length)
          : 0
      })
    }

    // Status breakdown
    const statusCounts: Record<string, number> = {}
    decisions.forEach(d => {
      statusCounts[d.status] = (statusCounts[d.status] || 0) + 1
    })

    const statusBreakdown = Object.entries(statusCounts).map(([status, count]) => ({
      status,
      count,
      percentage: Math.round((count / totalDecisions) * 100)
    }))

    // Risk analysis by domain
    const riskAnalysis = topDomains.map(domain => {
      const domainDecisions = decisions.filter(d => (d.domainType || 'General Planning') === domain.name)
      const decisionsWithRisks = domainDecisions.filter(d => d.analysis?.risks)
      const totalRisks = decisionsWithRisks.reduce((sum, d) => sum + (d.analysis?.risks?.length || 0), 0)
      const highRiskCount = decisionsWithRisks.filter(d => 
        d.analysis?.risks?.some(r => r.impact === 'HIGH')
      ).length

      return {
        domain: domain.name,
        avgRisks: decisionsWithRisks.length > 0 ? Math.round(totalRisks / decisionsWithRisks.length) : 0,
        highRiskCount
      }
    })

    setAnalytics({
      totalDecisions,
      avgFeasibilityScore,
      decisionsThisMonth,
      topDomains,
      feasibilityDistribution: feasibilityRanges,
      monthlyTrends,
      statusBreakdown,
      riskAnalysis
    })
  }

  if (loading) {
    return (
      <main className="min-h-screen bg-gradient-to-br from-white to-green-50 dark:from-slate-950 dark:to-slate-900">
        <div className="flex items-center justify-center h-screen">
          <div className="text-center">
            <div className="w-8 h-8 border-4 border-green-600 border-t-transparent rounded-full animate-spin mx-auto mb-4"></div>
            <p className="text-foreground/60">Loading analytics...</p>
          </div>
        </div>
      </main>
    )
  }

  if (!analytics) {
    return (
      <main className="min-h-screen bg-gradient-to-br from-white to-green-50 dark:from-slate-950 dark:to-slate-900 p-8">
        <div className="max-w-2xl mx-auto text-center">
          <h1 className="text-3xl font-bold mb-4">No Data Available</h1>
          <p className="text-foreground/60 mb-8">Create some decisions to see your analytics dashboard.</p>
          <Link href="/decisions/new">
            <Button className="bg-gradient-to-r from-green-500 to-emerald-600">
              Create Your First Decision
            </Button>
          </Link>
        </div>
      </main>
    )
  }

  return (
    <main className="min-h-screen bg-gradient-to-br from-white to-green-50 dark:from-slate-950 dark:to-slate-900">
      {/* Header */}
      <div className="bg-white/80 dark:bg-slate-950/80 backdrop-blur border-b border-green-100 dark:border-slate-800">
        <div className="max-w-7xl mx-auto px-6 py-6">
          <div className="flex items-center justify-between">
            <div>
              <h1 className="text-3xl font-bold flex items-center gap-3">
                <BarChart3 className="w-8 h-8 text-green-600" />
                Decision Analytics
              </h1>
              <p className="text-foreground/60 mt-1">Insights into your decision-making patterns</p>
            </div>
            <div className="flex gap-3">
              <Link href="/decisions">
                <Button variant="outline">View Decisions</Button>
              </Link>
              <Link href="/decisions/new">
                <Button className="bg-gradient-to-r from-green-500 to-emerald-600">
                  New Decision
                </Button>
              </Link>
            </div>
          </div>
        </div>
      </div>

      <div className="max-w-7xl mx-auto px-6 py-8">
        {/* Key Metrics */}
        <div className="grid grid-cols-1 md:grid-cols-4 gap-6 mb-8">
          <Card className="bg-gradient-to-br from-blue-500 to-blue-600 text-white border-0">
            <CardContent className="p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-blue-100 text-sm">Total Decisions</p>
                  <p className="text-3xl font-bold">{analytics.totalDecisions}</p>
                </div>
                <Target className="w-8 h-8 text-blue-200" />
              </div>
            </CardContent>
          </Card>

          <Card className="bg-gradient-to-br from-green-500 to-green-600 text-white border-0">
            <CardContent className="p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-green-100 text-sm">Avg Feasibility</p>
                  <p className="text-3xl font-bold">{analytics.avgFeasibilityScore}%</p>
                </div>
                <TrendingUp className="w-8 h-8 text-green-200" />
              </div>
            </CardContent>
          </Card>

          <Card className="bg-gradient-to-br from-purple-500 to-purple-600 text-white border-0">
            <CardContent className="p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-purple-100 text-sm">This Month</p>
                  <p className="text-3xl font-bold">{analytics.decisionsThisMonth}</p>
                </div>
                <Calendar className="w-8 h-8 text-purple-200" />
              </div>
            </CardContent>
          </Card>

          <Card className="bg-gradient-to-br from-orange-500 to-orange-600 text-white border-0">
            <CardContent className="p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-orange-100 text-sm">Top Domain</p>
                  <p className="text-lg font-bold truncate">{analytics.topDomains[0]?.name || 'None'}</p>
                </div>
                <Brain className="w-8 h-8 text-orange-200" />
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Charts Row 1 */}
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-8 mb-8">
          {/* Monthly Trends */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Activity className="w-5 h-5 text-green-600" />
                Monthly Trends
              </CardTitle>
            </CardHeader>
            <CardContent>
              <ResponsiveContainer width="100%" height={300}>
                <AreaChart data={analytics.monthlyTrends}>
                  <CartesianGrid strokeDasharray="3 3" className="opacity-30" />
                  <XAxis dataKey="month" />
                  <YAxis />
                  <Tooltip 
                    contentStyle={{ 
                      backgroundColor: 'rgba(255, 255, 255, 0.95)', 
                      border: '1px solid #e5e7eb',
                      borderRadius: '8px'
                    }} 
                  />
                  <Area 
                    type="monotone" 
                    dataKey="decisions" 
                    stroke="#10b981" 
                    fill="url(#colorDecisions)" 
                  />
                  <defs>
                    <linearGradient id="colorDecisions" x1="0" y1="0" x2="0" y2="1">
                      <stop offset="5%" stopColor="#10b981" stopOpacity={0.8}/>
                      <stop offset="95%" stopColor="#10b981" stopOpacity={0.1}/>
                    </linearGradient>
                  </defs>
                </AreaChart>
              </ResponsiveContainer>
            </CardContent>
          </Card>

          {/* Domain Distribution */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <PieChartIcon className="w-5 h-5 text-green-600" />
                Decision Domains
              </CardTitle>
            </CardHeader>
            <CardContent>
              <ResponsiveContainer width="100%" height={300}>
                <PieChart>
                  <Pie
                    data={analytics.topDomains}
                    cx="50%"
                    cy="50%"
                    innerRadius={60}
                    outerRadius={100}
                    dataKey="count"
                    label={({ name, count }) => `${name}: ${count}`}
                  >
                    {analytics.topDomains.map((entry, index) => (
                      <Cell key={`cell-${index}`} fill={entry.color} />
                    ))}
                  </Pie>
                  <Tooltip />
                </PieChart>
              </ResponsiveContainer>
            </CardContent>
          </Card>
        </div>

        {/* Charts Row 2 */}
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-8 mb-8">
          {/* Feasibility Distribution */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <BarChart3 className="w-5 h-5 text-green-600" />
                Feasibility Score Distribution
              </CardTitle>
            </CardHeader>
            <CardContent>
              <ResponsiveContainer width="100%" height={300}>
                <BarChart data={analytics.feasibilityDistribution}>
                  <CartesianGrid strokeDasharray="3 3" className="opacity-30" />
                  <XAxis dataKey="range" />
                  <YAxis />
                  <Tooltip />
                  <Bar dataKey="count" fill="#3b82f6" />
                </BarChart>
              </ResponsiveContainer>
            </CardContent>
          </Card>

          {/* Status Breakdown */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Activity className="w-5 h-5 text-green-600" />
                Decision Status
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {analytics.statusBreakdown.map((status) => (
                  <div key={status.status} className="flex items-center justify-between">
                    <div className="flex items-center gap-3">
                      {status.status === 'Completed' && <CheckCircle2 className="w-4 h-4 text-green-600" />}
                      {status.status === 'Analyzed' && <Sparkles className="w-4 h-4 text-blue-600" />}
                      {status.status === 'Draft' && <AlertTriangle className="w-4 h-4 text-amber-600" />}
                      <span className="font-medium">{status.status}</span>
                    </div>
                    <div className="flex items-center gap-2">
                      <span className="text-sm text-foreground/60">{status.count}</span>
                      <Badge variant="secondary">{status.percentage}%</Badge>
                    </div>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Risk Analysis Table */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <XCircle className="w-5 h-5 text-red-600" />
              Risk Analysis by Domain
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead>
                  <tr className="border-b">
                    <th className="text-left py-3 px-4">Domain</th>
                    <th className="text-left py-3 px-4">Avg Risks per Decision</th>
                    <th className="text-left py-3 px-4">High-Risk Decisions</th>
                    <th className="text-left py-3 px-4">Risk Level</th>
                  </tr>
                </thead>
                <tbody>
                  {analytics.riskAnalysis.map((domain) => (
                    <tr key={domain.domain} className="border-b hover:bg-muted/50">
                      <td className="py-3 px-4 font-medium">{domain.domain}</td>
                      <td className="py-3 px-4">{domain.avgRisks}</td>
                      <td className="py-3 px-4">{domain.highRiskCount}</td>
                      <td className="py-3 px-4">
                        <Badge 
                          variant={domain.highRiskCount > 2 ? "destructive" : domain.highRiskCount > 0 ? "secondary" : "default"}
                        >
                          {domain.highRiskCount > 2 ? "High" : domain.highRiskCount > 0 ? "Medium" : "Low"}
                        </Badge>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </CardContent>
        </Card>
      </div>
    </main>
  )
}