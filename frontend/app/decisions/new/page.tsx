"use client"

import type React from "react"

import { useState } from "react"
import { useRouter } from "next/navigation"
import Link from "next/link"
import { ChevronLeft } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Card } from "@/components/ui/card"
import { decisionApi } from "@/lib/api"

export default function NewDecisionPage() {
  const router = useRouter()
  const [formData, setFormData] = useState({
    type: "LOAN_APPROVAL",
    createdBy: "user@example.com",
    loanAmount: "",
    creditScore: "",
    employmentStatus: "EMPLOYED",
    debtToIncomeRatio: "",
    context: "",
  })

  const [submitted, setSubmitted] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const [createdDecisionId, setCreatedDecisionId] = useState<string | null>(null)

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) => {
    const { name, value } = e.target
    setFormData((prev) => ({ ...prev, [name]: value }))
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    
    // Validate required fields
    if (!formData.loanAmount || !formData.creditScore || !formData.debtToIncomeRatio) {
      alert("Please fill in all required fields")
      return
    }

    setSubmitting(true)

    try {
      const inputData = {
        loanAmount: parseFloat(formData.loanAmount),
        creditScore: parseInt(formData.creditScore),
        employmentStatus: formData.employmentStatus,
        debtToIncomeRatio: parseFloat(formData.debtToIncomeRatio),
        context: formData.context || undefined
      }

      const result = await decisionApi.createDecision({
        type: formData.type,
        createdBy: formData.createdBy,
        inputData
      })

      setCreatedDecisionId(result.id)
      setSubmitted(true)
    } catch (error) {
      console.error('Failed to create decision:', error)
      alert('Failed to create decision. Please try again.')
    } finally {
      setSubmitting(false)
    }
  }

  if (submitted) {
    return (
      <main className="min-h-screen bg-background">
        <div className="bg-card border-b border-border">
          <div className="max-w-3xl mx-auto px-6 py-4 flex items-center gap-4">
            <Link href="/decisions">
              <Button variant="ghost" size="sm">
                <ChevronLeft className="w-4 h-4 mr-2" />
                Back
              </Button>
            </Link>
            <h1 className="text-2xl font-bold text-foreground">Decision Created</h1>
          </div>
        </div>

        <div className="max-w-3xl mx-auto px-6 py-12">
          <Card className="p-8 bg-card border border-border text-center">
            <div className="space-y-4">
              <div className="text-4xl">✓</div>
              <h2 className="text-2xl font-bold text-foreground">Decision submitted successfully</h2>
              <p className="text-muted-foreground">Your decision has been created and saved.</p>
              {createdDecisionId && (
                <p className="text-sm font-mono text-foreground">ID: {createdDecisionId}</p>
              )}
              <div className="pt-4 flex gap-2 justify-center">
                {createdDecisionId && (
                  <Link href={`/decisions/${createdDecisionId}`}>
                    <Button>View Decision</Button>
                  </Link>
                )}
                <Link href="/decisions">
                  <Button variant={createdDecisionId ? "outline" : "default"}>View All Decisions</Button>
                </Link>
                <Button variant="outline" onClick={() => {
                  setSubmitted(false)
                  setCreatedDecisionId(null)
                  setFormData({
                    type: "LOAN_APPROVAL",
                    createdBy: "user@example.com",
                    loanAmount: "",
                    creditScore: "",
                    employmentStatus: "EMPLOYED",
                    debtToIncomeRatio: "",
                    context: "",
                  })
                }}>
                  Create Another
                </Button>
              </div>
            </div>
          </Card>
        </div>
      </main>
    )
  }

  return (
    <main className="min-h-screen bg-background">
      <div className="bg-card border-b border-border">
        <div className="max-w-3xl mx-auto px-6 py-4 flex items-center gap-4">
          <Link href="/decisions">
            <Button variant="ghost" size="sm">
              <ChevronLeft className="w-4 h-4 mr-2" />
              Back
            </Button>
          </Link>
          <h1 className="text-2xl font-bold text-foreground">Create New Decision</h1>
        </div>
      </div>

      <div className="max-w-3xl mx-auto px-6 py-6">
        <form onSubmit={handleSubmit} className="space-y-6">
          {/* Decision Type */}
          <Card className="p-6 bg-card border border-border">
            <h2 className="text-lg font-bold text-foreground mb-4">Decision Type</h2>
            <div>
              <label className="block text-sm font-semibold text-foreground mb-2">Type</label>
              <select
                name="type"
                value={formData.type}
                onChange={handleInputChange}
                className="w-full px-4 py-2 text-sm bg-background border border-border rounded-md text-foreground"
              >
                <option>LOAN_APPROVAL</option>
                <option>FRAUD_DETECTION</option>
                <option>CLAIM_PROCESSING</option>
                <option>PRICING_ADJUSTMENT</option>
              </select>
            </div>
          </Card>

          {/* Structured Inputs */}
          <Card className="p-6 bg-card border border-border">
            <h2 className="text-lg font-bold text-foreground mb-4">Loan Details</h2>
            <div className="space-y-4">
              <div>
                <label className="block text-sm font-semibold text-foreground mb-2">
                  Loan Amount <span className="text-red-600">*</span>
                </label>
                <input
                  type="number"
                  name="loanAmount"
                  value={formData.loanAmount}
                  onChange={handleInputChange}
                  placeholder="250000"
                  required
                  className="w-full px-4 py-2 text-sm bg-background border border-border rounded-md text-foreground"
                />
              </div>

              <div>
                <label className="block text-sm font-semibold text-foreground mb-2">
                  Credit Score <span className="text-red-600">*</span>
                </label>
                <input
                  type="number"
                  name="creditScore"
                  value={formData.creditScore}
                  onChange={handleInputChange}
                  placeholder="720"
                  required
                  className="w-full px-4 py-2 text-sm bg-background border border-border rounded-md text-foreground"
                />
              </div>

              <div>
                <label className="block text-sm font-semibold text-foreground mb-2">Employment Status</label>
                <select
                  name="employmentStatus"
                  value={formData.employmentStatus}
                  onChange={handleInputChange}
                  className="w-full px-4 py-2 text-sm bg-background border border-border rounded-md text-foreground"
                >
                  <option>EMPLOYED</option>
                  <option>SELF_EMPLOYED</option>
                  <option>UNEMPLOYED</option>
                  <option>RETIRED</option>
                </select>
              </div>

              <div>
                <label className="block text-sm font-semibold text-foreground mb-2">
                  Debt-to-Income Ratio <span className="text-red-600">*</span>
                </label>
                <input
                  type="number"
                  step="0.01"
                  name="debtToIncomeRatio"
                  value={formData.debtToIncomeRatio}
                  onChange={handleInputChange}
                  placeholder="0.35"
                  required
                  className="w-full px-4 py-2 text-sm bg-background border border-border rounded-md text-foreground"
                />
              </div>
            </div>
          </Card>

          {/* Unstructured Context */}
          <Card className="p-6 bg-card border border-border">
            <h2 className="text-lg font-bold text-foreground mb-4">Additional Context</h2>
            <div>
              <label className="block text-sm font-semibold text-foreground mb-2">Notes</label>
              <textarea
                name="context"
                value={formData.context}
                onChange={handleInputChange}
                placeholder="Any additional context for the decision..."
                rows={5}
                className="w-full px-4 py-2 text-sm bg-background border border-border rounded-md text-foreground"
              />
            </div>
          </Card>

          {/* Submit */}
          <div className="flex gap-3">
            <Button type="submit" className="flex-1" disabled={submitting}>
              {submitting ? "Creating..." : "Submit Decision"}
            </Button>
            <Link href="/decisions" className="flex-1">
              <Button variant="outline" className="w-full bg-transparent" disabled={submitting}>
                Cancel
              </Button>
            </Link>
          </div>
        </form>
      </div>
    </main>
  )
}
