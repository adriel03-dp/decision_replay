'use client'

import type React from "react"
import { useState } from "react"
import { useRouter } from "next/navigation"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { useAuth } from "@/lib/auth-context"
import { 
  User as UserIcon, 
  Lock, 
  Trash2, 
  AlertCircle, 
  CheckCircle2,
  Palette
} from "lucide-react"
import { ThemeToggle } from "@/components/theme-toggle"

export default function SettingsPage() {
  const router = useRouter()
  const { user, token, updateProfile, changePassword, deleteAccount, logout } = useAuth()
  
  // Profile Update State
  const [name, setName] = useState(user?.name || "")
  const [profileError, setProfileError] = useState("")
  const [profileSuccess, setProfileSuccess] = useState("")
  const [isUpdatingProfile, setIsUpdatingProfile] = useState(false)

  // Password Change State
  const [currentPassword, setCurrentPassword] = useState("")
  const [newPassword, setNewPassword] = useState("")
  const [confirmPassword, setConfirmPassword] = useState("")
  const [passwordError, setPasswordError] = useState("")
  const [passwordSuccess, setPasswordSuccess] = useState("")
  const [isChangingPassword, setIsChangingPassword] = useState(false)

  // Delete Account State
  const [deletePassword, setDeletePassword] = useState("")
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false)
  const [deleteError, setDeleteError] = useState("")
  const [isDeleting, setIsDeleting] = useState(false)

  // Redirect if not authenticated
  if (!user || !token) {
    router.push("/login")
    return null
  }

  const handleUpdateProfile = async (e: React.FormEvent) => {
    e.preventDefault()
    setProfileError("")
    setProfileSuccess("")
    setIsUpdatingProfile(true)

    if (!name || name.length < 2) {
      setProfileError("Name must be at least 2 characters")
      setIsUpdatingProfile(false)
      return
    }

    const result = await updateProfile(name)

    if (result.success) {
      setProfileSuccess("Profile updated successfully!")
      setTimeout(() => setProfileSuccess(""), 3000)
    } else {
      setProfileError(result.error || "Failed to update profile")
    }

    setIsUpdatingProfile(false)
  }

  const handleChangePassword = async (e: React.FormEvent) => {
    e.preventDefault()
    setPasswordError("")
    setPasswordSuccess("")
    setIsChangingPassword(true)

    if (!currentPassword || !newPassword || !confirmPassword) {
      setPasswordError("Please fill in all password fields")
      setIsChangingPassword(false)
      return
    }

    if (newPassword.length < 6) {
      setPasswordError("New password must be at least 6 characters")
      setIsChangingPassword(false)
      return
    }

    if (newPassword !== confirmPassword) {
      setPasswordError("New passwords do not match")
      setIsChangingPassword(false)
      return
    }

    const result = await changePassword(currentPassword, newPassword)

    if (result.success) {
      setPasswordSuccess("Password changed successfully!")
      setCurrentPassword("")
      setNewPassword("")
      setConfirmPassword("")
      setTimeout(() => setPasswordSuccess(""), 3000)
    } else {
      setPasswordError(result.error || "Failed to change password")
    }

    setIsChangingPassword(false)
  }

  const handleDeleteAccount = async () => {
    setDeleteError("")
    setIsDeleting(true)

    if (!deletePassword) {
      setDeleteError("Please enter your password to confirm")
      setIsDeleting(false)
      return
    }

    const result = await deleteAccount(deletePassword)

    if (result.success) {
      logout()
      router.push("/")
    } else {
      setDeleteError(result.error || "Failed to delete account")
      setIsDeleting(false)
    }
  }

  return (
    <main className="gradient-bg min-h-screen py-24 px-4">
      <div className="max-w-4xl mx-auto space-y-6">
        {/* Header */}
        <div className="slide-up">
          <h1 className="text-3xl font-bold gradient-text mb-2">Settings</h1>
          <p className="text-foreground/70">Manage your account settings and preferences</p>
        </div>

        {/* Profile Section */}
        <div className="slide-up relative">
          <div className="absolute -top-10 -left-10 w-32 h-32 bg-gradient-to-br from-green-400/20 to-emerald-400/10 rounded-full blur-3xl pointer-events-none" />
          
          <div className="relative p-6 rounded-2xl border border-green-100 dark:border-slate-700 bg-white/70 dark:bg-slate-900/70 backdrop-blur-md shadow-lg">
            <div className="flex items-center gap-3 mb-6">
              <div className="w-10 h-10 rounded-lg bg-gradient-to-br from-green-500 to-emerald-600 flex items-center justify-center">
                <UserIcon className="w-5 h-5 text-white" />
              </div>
              <div>
                <h2 className="text-xl font-bold">Profile Information</h2>
                <p className="text-sm text-foreground/70">Update your name and email</p>
              </div>
            </div>

            {profileError && (
              <div className="mb-4 p-4 rounded-lg bg-red-100/50 dark:bg-red-900/30 border border-red-200 dark:border-red-800 flex items-start gap-3">
                <AlertCircle className="w-5 h-5 text-red-600 dark:text-red-400 mt-0.5 flex-shrink-0" />
                <span className="text-sm text-red-700 dark:text-red-300">{profileError}</span>
              </div>
            )}

            {profileSuccess && (
              <div className="mb-4 p-4 rounded-lg bg-green-100/50 dark:bg-green-900/30 border border-green-200 dark:border-green-800 flex items-start gap-3">
                <CheckCircle2 className="w-5 h-5 text-green-600 dark:text-green-400 mt-0.5 flex-shrink-0" />
                <span className="text-sm text-green-700 dark:text-green-300">{profileSuccess}</span>
              </div>
            )}

            <form onSubmit={handleUpdateProfile} className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="name">Name</Label>
                <Input
                  id="name"
                  type="text"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  className="bg-white/50 dark:bg-slate-800/50 border-green-100 dark:border-slate-700 focus:border-green-400 focus:ring-green-400/30"
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="email">Email</Label>
                <Input
                  id="email"
                  type="email"
                  value={user?.email || ""}
                  disabled
                  className="bg-slate-100 dark:bg-slate-800 opacity-60 cursor-not-allowed"
                />
                <p className="text-xs text-foreground/60">Email cannot be changed</p>
              </div>

              <Button
                type="submit"
                disabled={isUpdatingProfile}
                className="gradient-button w-full sm:w-auto"
              >
                {isUpdatingProfile ? "Updating..." : "Update Profile"}
              </Button>
            </form>
          </div>
        </div>

        {/* Theme Section */}
        <div className="slide-up" style={{ animationDelay: '0.1s' }}>
          <div className="p-6 rounded-2xl border border-green-100 dark:border-slate-700 bg-white/70 dark:bg-slate-900/70 backdrop-blur-md shadow-lg">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 rounded-lg bg-gradient-to-br from-purple-500 to-pink-600 flex items-center justify-center">
                  <Palette className="w-5 h-5 text-white" />
                </div>
                <div>
                  <h2 className="text-xl font-bold">Appearance</h2>
                  <p className="text-sm text-foreground/70">Customize your theme preference</p>
                </div>
              </div>
              <ThemeToggle />
            </div>
          </div>
        </div>

        {/* Password Section */}
        <div className="slide-up" style={{ animationDelay: '0.2s' }}>
          <div className="p-6 rounded-2xl border border-green-100 dark:border-slate-700 bg-white/70 dark:bg-slate-900/70 backdrop-blur-md shadow-lg">
            <div className="flex items-center gap-3 mb-6">
              <div className="w-10 h-10 rounded-lg bg-gradient-to-br from-blue-500 to-cyan-600 flex items-center justify-center">
                <Lock className="w-5 h-5 text-white" />
              </div>
              <div>
                <h2 className="text-xl font-bold">Change Password</h2>
                <p className="text-sm text-foreground/70">Update your password regularly for security</p>
              </div>
            </div>

            {passwordError && (
              <div className="mb-4 p-4 rounded-lg bg-red-100/50 dark:bg-red-900/30 border border-red-200 dark:border-red-800 flex items-start gap-3">
                <AlertCircle className="w-5 h-5 text-red-600 dark:text-red-400 mt-0.5 flex-shrink-0" />
                <span className="text-sm text-red-700 dark:text-red-300">{passwordError}</span>
              </div>
            )}

            {passwordSuccess && (
              <div className="mb-4 p-4 rounded-lg bg-green-100/50 dark:bg-green-900/30 border border-green-200 dark:border-green-800 flex items-start gap-3">
                <CheckCircle2 className="w-5 h-5 text-green-600 dark:text-green-400 mt-0.5 flex-shrink-0" />
                <span className="text-sm text-green-700 dark:text-green-300">{passwordSuccess}</span>
              </div>
            )}

            <form onSubmit={handleChangePassword} className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="currentPassword">Current Password</Label>
                <Input
                  id="currentPassword"
                  type="password"
                  value={currentPassword}
                  onChange={(e) => setCurrentPassword(e.target.value)}
                  className="bg-white/50 dark:bg-slate-800/50 border-green-100 dark:border-slate-700 focus:border-green-400 focus:ring-green-400/30"
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="newPassword">New Password</Label>
                <Input
                  id="newPassword"
                  type="password"
                  value={newPassword}
                  onChange={(e) => setNewPassword(e.target.value)}
                  className="bg-white/50 dark:bg-slate-800/50 border-green-100 dark:border-slate-700 focus:border-green-400 focus:ring-green-400/30"
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="confirmPassword">Confirm New Password</Label>
                <Input
                  id="confirmPassword"
                  type="password"
                  value={confirmPassword}
                  onChange={(e) => setConfirmPassword(e.target.value)}
                  className="bg-white/50 dark:bg-slate-800/50 border-green-100 dark:border-slate-700 focus:border-green-400 focus:ring-green-400/30"
                />
              </div>

              <Button
                type="submit"
                disabled={isChangingPassword}
                className="gradient-button w-full sm:w-auto"
              >
                {isChangingPassword ? "Changing..." : "Change Password"}
              </Button>
            </form>
          </div>
        </div>

        {/* Delete Account Section */}
        <div className="slide-up" style={{ animationDelay: '0.3s' }}>
          <div className="p-6 rounded-2xl border border-red-200 dark:border-red-900/50 bg-red-50/50 dark:bg-red-950/20 backdrop-blur-md shadow-lg">
            <div className="flex items-center gap-3 mb-4">
              <div className="w-10 h-10 rounded-lg bg-gradient-to-br from-red-500 to-rose-600 flex items-center justify-center">
                <Trash2 className="w-5 h-5 text-white" />
              </div>
              <div>
                <h2 className="text-xl font-bold text-red-700 dark:text-red-400">Danger Zone</h2>
                <p className="text-sm text-red-600 dark:text-red-500">Permanently delete your account</p>
              </div>
            </div>

            {deleteError && (
              <div className="mb-4 p-4 rounded-lg bg-red-100 dark:bg-red-900/50 border border-red-300 dark:border-red-800 flex items-start gap-3">
                <AlertCircle className="w-5 h-5 text-red-600 dark:text-red-400 mt-0.5 flex-shrink-0" />
                <span className="text-sm text-red-700 dark:text-red-300">{deleteError}</span>
              </div>
            )}

            <div className="space-y-4">
              <div className="p-4 rounded-lg bg-red-100 dark:bg-red-900/30 border border-red-200 dark:border-red-800">
                <p className="text-sm text-red-800 dark:text-red-200 font-medium mb-2">
                  ⚠️ Warning: This action cannot be undone
                </p>
                <p className="text-xs text-red-700 dark:text-red-300">
                  Deleting your account will permanently remove all your data, including decisions, analytics, and profile information.
                </p>
              </div>

              {!showDeleteConfirm ? (
                <Button
                  type="button"
                  variant="destructive"
                  onClick={() => setShowDeleteConfirm(true)}
                  className="bg-red-600 hover:bg-red-700"
                >
                  Delete My Account
                </Button>
              ) : (
                <div className="space-y-4 p-4 rounded-lg bg-white dark:bg-slate-900 border-2 border-red-300 dark:border-red-800">
                  <p className="text-sm font-medium">Enter your password to confirm account deletion:</p>
                  
                  <div className="space-y-2">
                    <Label htmlFor="deletePassword">Password</Label>
                    <Input
                      id="deletePassword"
                      type="password"
                      value={deletePassword}
                      onChange={(e) => setDeletePassword(e.target.value)}
                      className="bg-white dark:bg-slate-800 border-red-300 dark:border-red-700"
                      placeholder="Enter your password"
                    />
                  </div>

                  <div className="flex gap-3">
                    <Button
                      type="button"
                      onClick={handleDeleteAccount}
                      disabled={isDeleting}
                      variant="destructive"
                      className="bg-red-600 hover:bg-red-700"
                    >
                      {isDeleting ? "Deleting..." : "Confirm Delete"}
                    </Button>
                    <Button
                      type="button"
                      variant="outline"
                      onClick={() => {
                        setShowDeleteConfirm(false)
                        setDeletePassword("")
                        setDeleteError("")
                      }}
                    >
                      Cancel
                    </Button>
                  </div>
                </div>
              )}
            </div>
          </div>
        </div>
      </div>
    </main>
  )
}
