"use client"

import type React from "react"

import { useState } from "react"
import Link from "next/link"
import { useRouter } from "next/navigation"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { ArrowRight, Mail, Lock, AlertCircle } from "lucide-react"
import { useAuth } from "@/lib/auth-context"

export default function LoginPage() {
  const router = useRouter()
  const { login } = useAuth()
  const [email, setEmail] = useState("")
  const [password, setPassword] = useState("")
  const [error, setError] = useState("")
  const [isLoading, setIsLoading] = useState(false)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError("")
    setIsLoading(true)

    if (!email || !password) {
      setError("Please fill in all fields")
      setIsLoading(false)
      return
    }

    const result = await login(email, password)
    
    if (result.success) {
      router.push("/decisions")
    } else {
      setError(result.error || "Login failed. Please try again.")
      setIsLoading(false)
    }
  }

  return (
    <main className="gradient-bg min-h-screen flex items-center justify-center px-4">
      <div className="w-full max-w-md slide-up">
        {/* Left Gradient Accent */}
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
              <h1 className="text-2xl font-bold mb-2">Welcome Back</h1>
              <p className="text-foreground/70">Sign in to your account to access your decisions</p>
            </div>

            {/* Error Message */}
            {error && (
              <div className="mb-6 p-4 rounded-lg bg-red-100/50 dark:bg-red-900/30 border border-red-200 dark:border-red-800 flex items-start gap-3">
                <AlertCircle className="w-5 h-5 text-red-600 dark:text-red-400 mt-0.5 flex-shrink-0" />
                <span className="text-sm text-red-700 dark:text-red-300">{error}</span>
              </div>
            )}

            {/* Form */}
            <form onSubmit={handleSubmit} className="space-y-5">
              {/* Email Field */}
              <div className="space-y-2">
                <Label htmlFor="email" className="text-sm font-medium">
                  Email
                </Label>
                <div className="relative">
                  <Mail className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-foreground/40" />
                  <Input
                    id="email"
                    type="email"
                    placeholder="you@company.com"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    className="pl-10 bg-white/50 dark:bg-slate-800/50 border-green-100 dark:border-slate-700 focus:border-green-400 focus:ring-green-400/30"
                  />
                </div>
              </div>

              {/* Password Field */}
              <div className="space-y-2">
                <div className="flex items-center justify-between">
                  <Label htmlFor="password" className="text-sm font-medium">
                    Password
                  </Label>
                  <Link
                    href="#"
                    className="text-sm text-green-600 dark:text-green-400 hover:text-green-700 smooth-transition"
                  >
                    Forgot?
                  </Link>
                </div>
                <div className="relative">
                  <Lock className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-foreground/40" />
                  <Input
                    id="password"
                    type="password"
                    placeholder="••••••••"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    className="pl-10 bg-white/50 dark:bg-slate-800/50 border-green-100 dark:border-slate-700 focus:border-green-400 focus:ring-green-400/30"
                  />
                </div>
              </div>

              {/* Remember Me */}
              <div className="flex items-center gap-2">
                <input
                  type="checkbox"
                  id="remember"
                  className="w-4 h-4 rounded border-green-200 dark:border-slate-700 accent-green-600"
                />
                <label htmlFor="remember" className="text-sm text-foreground/70 cursor-pointer">
                  Keep me signed in
                </label>
              </div>

              {/* Submit Button */}
              <Button type="submit" disabled={isLoading} className="w-full gradient-button">
                {isLoading ? "Signing in..." : "Sign In"}
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

            {/* Social Login */}
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

            {/* Sign Up Link */}
            <p className="text-center text-sm text-foreground/70">
              Don't have an account?{" "}
              <Link
                href="/signup"
                className="text-green-600 dark:text-green-400 font-semibold hover:text-green-700 smooth-transition"
              >
                Sign up
              </Link>
            </p>
          </div>
        </div>

        {/* Right Gradient Accent */}
        <div className="absolute -bottom-20 -right-20 w-40 h-40 bg-gradient-to-br from-emerald-400/20 to-green-400/10 rounded-full blur-3xl pointer-events-none" />
      </div>
    </main>
  )
}
