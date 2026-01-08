"use client"

import { useEffect, useState } from "react"
import { useRouter, useParams } from "next/navigation"

// Redirect to the analysis page - V2 decisions use analysis-based view instead of event replay
export default function DecisionPage() {
  const router = useRouter()
  const params = useParams()
  const [error, setError] = useState<string | null>(null)
  
  useEffect(() => {
    const id = params.id as string
    if (id) {
      // First check if the decision exists before redirecting
      checkDecisionExists(id)
    }
  }, [params, router])

  const checkDecisionExists = async (id: string) => {
    try {
      const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/v2/decisions/${id}`, {
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('token')}`
        }
      })

      if (response.ok) {
        // Decision exists, redirect to analysis
        router.replace(`/decisions/${id}/analysis`)
      } else {
        // Decision doesn't exist or permission denied
        setError('Decision not found or access denied')
        setTimeout(() => router.push('/decisions'), 2000)
      }
    } catch (error) {
      console.error('Error checking decision:', error)
      setError('Failed to load decision')
      setTimeout(() => router.push('/decisions'), 2000)
    }
  }

  if (error) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <div className="text-center">
          <h2 className="text-xl font-semibold mb-2 text-destructive">Error</h2>
          <p className="text-muted-foreground mb-4">{error}</p>
          <p className="text-sm text-muted-foreground">Redirecting back to decisions...</p>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-background flex items-center justify-center">
      <div className="text-center">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary mx-auto mb-4"></div>
        <p className="text-muted-foreground">Loading decision...</p>
      </div>
    </div>
  )
}
