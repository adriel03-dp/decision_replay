import { GoogleGenerativeAI } from "@google/generative-ai"

let geminiClient: GoogleGenerativeAI | null = null

// Rate limiter for Gemini API (free tier: 5 requests per minute)
class RateLimiter {
  private requests: number[] = []
  private maxRequests: number
  private timeWindow: number // in milliseconds

  constructor(maxRequests: number = 5, timeWindowMinutes: number = 1) {
    this.maxRequests = maxRequests
    this.timeWindow = timeWindowMinutes * 60 * 1000
  }

  async waitForSlot(): Promise<void> {
    const now = Date.now()
    
    // Remove requests older than the time window
    this.requests = this.requests.filter(time => now - time < this.timeWindow)
    
    if (this.requests.length >= this.maxRequests) {
      // Calculate wait time until the oldest request expires
      const oldestRequest = this.requests[0]
      const waitTime = this.timeWindow - (now - oldestRequest) + 1000 // Add 1 second buffer
      
      console.log(`Rate limit reached. Waiting ${Math.ceil(waitTime / 1000)} seconds...`)
      await new Promise(resolve => setTimeout(resolve, waitTime))
      
      // Retry after waiting
      return this.waitForSlot()
    }
    
    // Record this request
    this.requests.push(now)
  }

  getRemainingRequests(): number {
    const now = Date.now()
    this.requests = this.requests.filter(time => now - time < this.timeWindow)
    return Math.max(0, this.maxRequests - this.requests.length)
  }
}

const rateLimiter = new RateLimiter(5, 1) // 5 requests per minute

export function getGeminiClient() {
  if (!geminiClient) {
    const apiKey = process.env.GEMINI_API_KEY
    if (!apiKey) {
      console.warn("GEMINI_API_KEY environment variable is not set. AI features will be disabled.")
      return null
    }
    geminiClient = new GoogleGenerativeAI(apiKey)
  }
  return geminiClient
}

export function isGeminiConfigured(): boolean {
  return !!process.env.GEMINI_API_KEY
}

export function getRemainingRequests(): number {
  return rateLimiter.getRemainingRequests()
}

export async function generateDecisionExplanation(decision: any, decisionEvents: any[]): Promise<string> {
  const client = getGeminiClient()
  
  if (!client) {
    return "AI explanation is not available. Please configure the GEMINI_API_KEY environment variable to enable AI-powered explanations."
  }

  try {
    // Wait for rate limit slot
    await rateLimiter.waitForSlot()
    console.log(`Gemini API call - Remaining requests: ${rateLimiter.getRemainingRequests()}`)
    
    const model = client.getGenerativeModel({ model: "gemini-pro" })

    const prompt = `You are an AI decision analysis expert. Analyze this decision replay data and provide a detailed explanation:

Decision: ${decision.id}
Type: ${decision.type}
Outcome: ${decision.outcome || decision.currentOutcome}
Risk Score: ${decision.riskScore || 0}

Decision Events Timeline:
${decisionEvents
  .map(
    (event) => `
- ${event.timestamp}: ${event.eventType}
  Payload: ${JSON.stringify(event.payload)}
`,
  )
  .join("")}

Please provide:
1. A concise summary of the decision process
2. Key factors that influenced the outcome
3. Why the AI chose this decision
4. Any notable patterns or trends
5. Recommendations for improvement

Keep the explanation clear, professional, and suitable for business stakeholders.`

    const result = await model.generateContent(prompt)
    const text = result.response.text()
    return text
  } catch (error) {
    console.error('Error generating AI explanation:', error)
    return "Failed to generate AI explanation. Please check your API configuration."
  }
}

export async function generateFactorAnalysis(decision: any): Promise<string> {
  const client = getGeminiClient()
  
  if (!client) {
    return "Factor analysis is not available. Please configure the GEMINI_API_KEY environment variable."
  }

  try {
    // Wait for rate limit slot
    await rateLimiter.waitForSlot()
    
    const model = client.getGenerativeModel({ model: "gemini-pro" })

    const prompt = `As an AI decision analyst, provide detailed analysis of factor influences on this decision:

Decision ID: ${decision.id}
Type: ${decision.type}
Risk Score: ${decision.riskScore || 0}

Provide:
1. Which factors were most influential and why
2. How these factors interacted
3. Impact of each factor on the final decision
4. Opportunities to optimize factor weighting`

    const result = await model.generateContent(prompt)
    return result.response.text()
  } catch (error) {
    console.error('Error generating factor analysis:', error)
    return "Failed to generate factor analysis. Please check your API configuration."
  }
}
