"use client"

import type React from "react"

import { useState } from "react"
import Link from "next/link"
import { useRouter } from "next/navigation"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { ArrowRight, Mail, Lock, User, AlertCircle, CheckCircle2 } from "lucide-react"
import { useAuth } from "@/lib/auth-context"

export default function SignUpPage() {
  const router = useRouter()
  const { register } = useAuth()
  const [formData, setFormData] = useState({
    name: "",
    email: "",
    password: "",
    confirmPassword: "",
    agreeToTerms: false,
  })
  const [error, setError] = useState("")
  const [isLoading, setIsLoading] = useState(false)
  const [submitted, setSubmitted] = useState(false)

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, type, checked, value } = e.target
    setFormData((prev) => ({
      ...prev,
      [name]: type === "checkbox" ? checked : value,
    }))
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError("")
    setIsLoading(true)

    // Validate form
    if (!formData.name || !formData.email || !formData.password || !formData.confirmPassword) {
      setError("Please fill in all fields")
      setIsLoading(false)
      return
    }

    if (formData.password !== formData.confirmPassword) {
      setError("Passwords do not match")
      setIsLoading(false)
      return
    }

    if (!formData.agreeToTerms) {
      setError("Please agree to the terms and conditions")
      setIsLoading(false)
      return
    }

    const result = await register(formData.name, formData.email, formData.password)
    
    if (result.success) {
      setSubmitted(true)
      setTimeout(() => {
        router.push("/decisions")
      }, 1500)
    } else {
      setError(result.error || "Registration failed. Please try again.")
      setIsLoading(false)
    }
  }

  if (submitted) {
    return (
      <main className="gradient-bg min-h-screen flex items-center justify-center px-4">
        <div className="w-full max-w-md slide-up">
          <div className="relative">
            <div className="absolute -top-20 -left-20 w-40 h-40 bg-gradient-to-br from-green-400/20 to-emerald-400/10 rounded-full blur-3xl pointer-events-none" />

            <div className="relative p-8 rounded-2xl border border-green-100 dark:border-slate-700 bg-white/70 dark:bg-slate-900/70 backdrop-blur-md shadow-lg text-center">
              <div className="mb-6 flex justify-center">
                <div className="w-16 h-16 rounded-full bg-gradient-to-br from-green-500 to-emerald-600 flex items-center justify-center">
                  <CheckCircle2 className="w-8 h-8 text-white" />
                </div>
              </div>

              <h1 className="text-2xl font-bold mb-2">Welcome to Decision Replay!</h1>
              <p className="text-foreground/70 mb-8">
                Your account has been created successfully. Check your email to verify your account and get started.
              </p>

              <div className="space-y-3">
                <Button className="w-full gradient-button">
                  <Link href="/decisions">Go to Dashboard</Link>
                </Button>
                <Button
                  variant="outline"
                  className="w-full bg-white/50 dark:bg-slate-800/50 border-green-100 dark:border-slate-700"
                >
                  <Link href="/">Back to Home</Link>
                </Button>
              </div>
            </div>

            <div className="absolute -bottom-20 -right-20 w-40 h-40 bg-gradient-to-br from-emerald-400/20 to-green-400/10 rounded-full blur-3xl pointer-events-none" />
          </div>
        </div>
      </main>
    )
  }

  return (
    <main className="gradient-bg min-h-screen flex items-center justify-center px-4 py-12">
      <div className="w-full max-w-md slide-up">
        <div className="relative">
          <div className="absolute -top-20 -left-20 w-40 h-40 bg-gradient-to-br from-green-400/20 to-emerald-400/10 rounded-full blur-3xl pointer-events-none" />

          <div className="relative p-8 rounded-2xl border border-green-100 dark:border-slate-700 bg-white/70 dark:bg-slate-900/70 backdrop-blur-md shadow-lg">
            {/* Header */}
            <div className="mb-8">
              <div className="flex items-center gap-2 mb-6">
                <div className="w-8 h-8 rounded-lg bg-gradient-to-br from-green-500 to-emerald-600 flex items-center justify-center text-white font-bold text-sm">
                  DR
                </div>
                <span className="font-bold gradient-text">Decision Replay</span>
              </div>
              <h1 className="text-2xl font-bold mb-2">Create Your Account</h1>
              <p className="text-foreground/70">Join teams building trustworthy AI decisions</p>
            </div>

            {/* Error Message */}
            {error && (
              <div className="mb-6 p-4 rounded-lg bg-red-100/50 dark:bg-red-900/30 border border-red-200 dark:border-red-800 flex items-start gap-3">
                <AlertCircle className="w-5 h-5 text-red-600 dark:text-red-400 mt-0.5 flex-shrink-0" />
                <span className="text-sm text-red-700 dark:text-red-300">{error}</span>
              </div>
            )}

            {/* Form */}
            <form onSubmit={handleSubmit} className="space-y-4">
              {/* Name Field */}
              <div className="space-y-2">
                <Label htmlFor="name" className="text-sm font-medium">
                  Full Name
                </Label>
                <div className="relative">
                  <User className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-foreground/40" />
                  <Input
                    id="name"
                    name="name"
                    type="text"
                    placeholder="Enter your Name"
                    value={formData.name}
                    onChange={handleChange}
                    className="pl-10 bg-white/50 dark:bg-slate-800/50 border-green-100 dark:border-slate-700 focus:border-green-400 focus:ring-green-400/30"
                  />
                </div>
              </div>

              {/* Email Field */}
              <div className="space-y-2">
                <Label htmlFor="email" className="text-sm font-medium">
                  Email
                </Label>
                <div className="relative">
                  <Mail className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-foreground/40" />
                  <Input
                    id="email"
                    name="email"
                    type="email"
                    placeholder="Enter your Email"
                    value={formData.email}
                    onChange={handleChange}
                    className="pl-10 bg-white/50 dark:bg-slate-800/50 border-green-100 dark:border-slate-700 focus:border-green-400 focus:ring-green-400/30"
                  />
                </div>
              </div>

              {/* Password Field */}
              <div className="space-y-2">
                <Label htmlFor="password" className="text-sm font-medium">
                  Password
                </Label>
                <div className="relative">
                  <Lock className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-foreground/40" />
                  <Input
                    id="password"
                    name="password"
                    type="password"
                    placeholder="••••••••"
                    value={formData.password}
                    onChange={handleChange}
                    className="pl-10 bg-white/50 dark:bg-slate-800/50 border-green-100 dark:border-slate-700 focus:border-green-400 focus:ring-green-400/30"
                  />
                </div>
              </div>

              {/* Confirm Password Field */}
              <div className="space-y-2">
                <Label htmlFor="confirmPassword" className="text-sm font-medium">
                  Confirm Password
                </Label>
                <div className="relative">
                  <Lock className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-foreground/40" />
                  <Input
                    id="confirmPassword"
                    name="confirmPassword"
                    type="password"
                    placeholder="••••••••"
                    value={formData.confirmPassword}
                    onChange={handleChange}
                    className="pl-10 bg-white/50 dark:bg-slate-800/50 border-green-100 dark:border-slate-700 focus:border-green-400 focus:ring-green-400/30"
                  />
                </div>
              </div>

              {/* Terms Checkbox */}
              <div className="flex items-start gap-2 pt-2">
                <input
                  type="checkbox"
                  id="agreeToTerms"
                  name="agreeToTerms"
                  checked={formData.agreeToTerms}
                  onChange={handleChange}
                  className="w-4 h-4 rounded border-green-200 dark:border-slate-700 accent-green-600 mt-1"
                />
                <label htmlFor="agreeToTerms" className="text-sm text-foreground/70">
                  I agree to the{" "}
                  <Link href="#" className="text-green-600 dark:text-green-400 hover:text-green-700 font-medium">
                    Terms of Service
                  </Link>{" "}
                  and{" "}
                  <Link href="#" className="text-green-600 dark:text-green-400 hover:text-green-700 font-medium">
                    Privacy Policy
                  </Link>
                </label>
              </div>

              {/* Submit Button */}
              <Button type="submit" disabled={isLoading} className="w-full gradient-button pt-2">
                {isLoading ? "Creating account..." : "Create Account"}
                {!isLoading && <ArrowRight className="w-4 h-4 ml-2" />}
              </Button>
            </form>

            {/* Divider */}
            <div className="my-6 relative">
              <div className="absolute inset-0 flex items-center">
                <div className="w-full border-t border-green-100 dark:border-slate-700" />
              </div>
              <div className="relative flex justify-center text-sm">
                <span className="px-2 bg-white dark:bg-slate-900 text-foreground/70">Or</span>
              </div>
            </div>

            {/* Social Signup */}
            <div className="grid grid-cols-2 gap-3 mb-6">
              <Button
                variant="outline"
                className="bg-white/50 dark:bg-slate-800/50 border-green-100 dark:border-slate-700"
              >
                Google
              </Button>
              <Button
                variant="outline"
                className="bg-white/50 dark:bg-slate-800/50 border-green-100 dark:border-slate-700"
              >
                GitHub
              </Button>
            </div>

            {/* Login Link */}
            <p className="text-center text-sm text-foreground/70">
              Already have an account?{" "}
              <Link
                href="/login"
                className="text-green-600 dark:text-green-400 font-semibold hover:text-green-700 smooth-transition"
              >
                Sign in
              </Link>
            </p>
          </div>
        </div>

        <div className="absolute -bottom-20 -right-20 w-40 h-40 bg-gradient-to-br from-emerald-400/20 to-green-400/10 rounded-full blur-3xl pointer-events-none" />
      </div>
    </main>
  )
}
