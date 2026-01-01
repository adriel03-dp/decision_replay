"use client"

import Link from "next/link"
import { useRouter } from "next/navigation"
import { Button } from "@/components/ui/button"
import { ThemeToggle } from "@/components/theme-toggle"
import { Menu, X, LogOut, User as UserIcon } from "lucide-react"
import { useState } from "react"
import { useAuth } from "@/lib/auth-context"

export function Navbar() {
  const router = useRouter()
  const { user, logout, isAuthenticated } = useAuth()
  const [isOpen, setIsOpen] = useState(false)

  const handleLogout = () => {
    logout()
    router.push("/login")
  }

  return (
    <nav className="fixed top-0 w-full bg-white/80 dark:bg-slate-950/80 backdrop-blur-lg border-b border-green-100 dark:border-slate-800 z-50 smooth-transition">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex justify-between items-center h-16">
          {/* Logo */}
          <Link href="/" className="flex items-center gap-2 group">
            <div className="w-8 h-8 rounded-lg bg-gradient-to-br from-green-500 to-emerald-600 flex items-center justify-center text-white font-bold text-sm group-hover:shadow-lg group-hover:shadow-green-500/50 smooth-transition">
              DR
            </div>
            <span className="font-bold text-lg gradient-text hidden sm:inline">Decision Replay</span>
          </Link>

          {/* Desktop Navigation */}
          <div className="hidden md:flex items-center gap-8">
            <Link href="/about" className="text-foreground/70 hover:text-foreground smooth-transition font-medium">
              About
            </Link>
            {isAuthenticated && (
              <>
                <Link href="/decisions" className="text-foreground/70 hover:text-foreground smooth-transition font-medium">
                  Decisions
                </Link>
                <Link
                  href="/decisions-dashboard"
                  className="text-foreground/70 hover:text-foreground smooth-transition font-medium"
                >
                  Dashboard
                </Link>
                <Link href="/audit" className="text-foreground/70 hover:text-foreground smooth-transition font-medium">
                  Analytics
                </Link>
                <Link href="/api/gemini" className="text-foreground/70 hover:text-foreground smooth-transition font-medium">
                  Gemini AI
                </Link>
              </>
            )}
            <div className="flex items-center gap-3">
              <ThemeToggle />
              {isAuthenticated ? (
                <>
                  <div className="flex items-center gap-2 px-3 py-1.5 rounded-lg bg-green-50 dark:bg-slate-800 border border-green-100 dark:border-slate-700">
                    <UserIcon className="w-4 h-4 text-green-600 dark:text-green-400" />
                    <span className="text-sm font-medium text-foreground">{user?.name}</span>
                  </div>
                  <Link href="/settings">
                    <Button variant="outline" className="smooth-transition bg-transparent">
                      Settings
                    </Button>
                  </Link>
                  <Button onClick={handleLogout} variant="outline" className="smooth-transition bg-transparent">
                    <LogOut className="w-4 h-4 mr-2" />
                    Logout
                  </Button>
                </>
              ) : (
                <>
                  <Link href="/login">
                    <Button variant="outline" className="smooth-transition bg-transparent">
                      Login
                    </Button>
                  </Link>
                  <Link href="/signup">
                    <Button className="gradient-button">Sign Up</Button>
                  </Link>
                </>
              )}
            </div>
          </div>

          {/* Mobile Menu Button */}
          <div className="md:hidden flex items-center gap-3">
            <ThemeToggle />
            <button
              onClick={() => setIsOpen(!isOpen)}
              className="p-2 hover:bg-slate-100 dark:hover:bg-slate-800 rounded-lg smooth-transition"
            >
              {isOpen ? <X className="w-6 h-6" /> : <Menu className="w-6 h-6" />}
            </button>
          </div>
        </div>

        {/* Mobile Menu */}
        {isOpen && (
          <div className="md:hidden pb-4 animate-slide-down">
            <div className="flex flex-col gap-3">
              <Link href="/about">
                <Button variant="ghost" className="w-full justify-start">
                  About
                </Button>
              </Link>
              {isAuthenticated && (
                <>
                  <Link href="/decisions">
                    <Button variant="ghost" className="w-full justify-start">
                      Decisions
                    </Button>
                  </Link>
                  <Link href="/decisions-dashboard">
                    <Button variant="ghost" className="w-full justify-start">
                      Dashboard
                    </Button>
                  </Link>
                  <Link href="/audit">
                    <Button variant="ghost" className="w-full justify-start">
                      Analytics
                    </Button>
                  </Link>
                  <Link href="/api/gemini">
                    <Button variant="ghost" className="w-full justify-start">
                      Gemini AI
                    </Button>
                  </Link>
                  <Link href="/settings">
                    <Button variant="ghost" className="w-full justify-start">
                      Settings
                    </Button>
                  </Link>
                </>
              )}
              <div className="pt-2 border-t border-green-100 dark:border-slate-800 flex flex-col gap-2">
                {isAuthenticated ? (
                  <>
                    <div className="flex items-center gap-2 px-3 py-2 rounded-lg bg-green-50 dark:bg-slate-800 border border-green-100 dark:border-slate-700">
                      <UserIcon className="w-4 h-4 text-green-600 dark:text-green-400" />
                      <span className="text-sm font-medium text-foreground">{user?.name}</span>
                    </div>
                    <Button onClick={handleLogout} variant="outline" className="w-full smooth-transition bg-transparent">
                      <LogOut className="w-4 h-4 mr-2" />
                      Logout
                    </Button>
                  </>
                ) : (
                  <div className="flex gap-2">
                    <Link href="/login" className="flex-1">
                      <Button variant="outline" className="w-full smooth-transition bg-transparent">
                        Login
                      </Button>
                    </Link>
                    <Link href="/signup" className="flex-1">
                      <Button className="w-full gradient-button">Sign Up</Button>
                    </Link>
                  </div>
                )}
              </div>
            </div>
          </div>
        )}
      </div>
    </nav>
  )
}
