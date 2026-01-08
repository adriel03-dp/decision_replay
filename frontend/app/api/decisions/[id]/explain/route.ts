import { type NextRequest, NextResponse } from "next/server"

export async function POST(
  request: NextRequest, 
  { params }: { params: Promise<{ id: string }> }
) {
  const { id } = await params
  
  try {
    // Redirect to backend V2 API for explanation
    const backendUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000'
    const response = await fetch(`${backendUrl}/v2/decisions/${id}/explain`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': request.headers.get('Authorization') || ''
      }
    })

    if (!response.ok) {
      const error = await response.text()
      return NextResponse.json({ 
        error: "Backend explanation failed",
        message: error
      }, { status: response.status })
    }

    const result = await response.json()
    return NextResponse.json(result)
  } catch (error) {
    console.error("Backend proxy error:", error)
    return NextResponse.json({ 
      error: "Failed to connect to backend",
      message: error instanceof Error ? error.message : "Unknown error"
    }, { status: 500 })
  }
}
