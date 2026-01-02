import { type NextRequest, NextResponse } from "next/server"
import { generateDecisionExplanation, isGeminiConfigured } from "@/lib/gemini-client"
import { decisionApi } from "@/lib/api"

export async function POST(
  request: NextRequest, 
  { params }: { params: Promise<{ id: string }> }
) {
  const { id } = await params
  
  try {
    if (!isGeminiConfigured()) {
      return NextResponse.json({ 
        error: "AI features not configured",
        message: "Please set the GEMINI_API_KEY environment variable to enable AI explanations."
      }, { status: 503 })
    }

    const decision = await decisionApi.getDecisionById(id)
    if (!decision) {
      return NextResponse.json({ error: "Decision not found" }, { status: 404 })
    }

    const events = await decisionApi.getEvents(id)

    const explanation = await generateDecisionExplanation(decision, events)

    return NextResponse.json({
      id: decision.id,
      explanation,
      generatedAt: new Date().toISOString(),
    })
  } catch (error) {
    console.error("Gemini API error:", error)
    return NextResponse.json({ 
      error: "Failed to generate explanation",
      message: error instanceof Error ? error.message : "Unknown error"
    }, { status: 500 })
  }
}
