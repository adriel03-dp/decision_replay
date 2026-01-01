import { type NextRequest, NextResponse } from "next/server"
import { generateFactorAnalysis, isGeminiConfigured } from "@/lib/gemini-client"
import { decisionApi } from "@/lib/api"

export async function POST(request: NextRequest, { params }: { params: { id: string } }) {
  try {
    if (!isGeminiConfigured()) {
      return NextResponse.json({ 
        error: "AI features not configured",
        message: "Please set the GEMINI_API_KEY environment variable to enable factor analysis."
      }, { status: 503 })
    }

    const decision = await decisionApi.getDecisionById(params.id)
    if (!decision) {
      return NextResponse.json({ error: "Decision not found" }, { status: 404 })
    }

    const analysis = await generateFactorAnalysis(decision)

    return NextResponse.json({
      id: decision.id,
      analysis,
      generatedAt: new Date().toISOString(),
    })
  } catch (error) {
    console.error("Gemini API error:", error)
    return NextResponse.json({ 
      error: "Failed to generate analysis",
      message: error instanceof Error ? error.message : "Unknown error"
    }, { status: 500 })
  }
}
