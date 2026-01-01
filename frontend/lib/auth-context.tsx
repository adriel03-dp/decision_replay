'use client'

import React, { createContext, useContext, useState, useEffect } from 'react'

interface User {
  userId: string
  name: string
  email: string
}

interface AuthContextType {
  user: User | null
  token: string | null
  login: (email: string, password: string) => Promise<{ success: boolean; error?: string }>
  register: (name: string, email: string, password: string) => Promise<{ success: boolean; error?: string }>
  updateProfile: (name: string) => Promise<{ success: boolean; error?: string }>
  changePassword: (currentPassword: string, newPassword: string) => Promise<{ success: boolean; error?: string }>
  deleteAccount: (password: string) => Promise<{ success: boolean; error?: string }>
  logout: () => void
  isAuthenticated: boolean
  isLoading: boolean
}

const AuthContext = createContext<AuthContextType | undefined>(undefined)

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<User | null>(null)
  const [token, setToken] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  // Load user and token from localStorage on mount
  useEffect(() => {
    const storedToken = localStorage.getItem('token')
    const storedUser = localStorage.getItem('user')

    if (storedToken && storedUser) {
      setToken(storedToken)
      setUser(JSON.parse(storedUser))
    }
    setIsLoading(false)
  }, [])

  const login = async (email: string, password: string) => {
    try {
      const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/auth/login`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password }),
      })

      if (!response.ok) {
        const error = await response.json()
        return { success: false, error: error.message || 'Login failed' }
      }

      const data = await response.json()
      
      // Save to state and localStorage
      setToken(data.token)
      setUser({
        userId: data.userId,
        name: data.name,
        email: data.email,
      })

      localStorage.setItem('token', data.token)
      localStorage.setItem('user', JSON.stringify({
        userId: data.userId,
        name: data.name,
        email: data.email,
      }))

      return { success: true }
    } catch (error) {
      return { success: false, error: 'Network error. Please try again.' }
    }
  }

  const register = async (name: string, email: string, password: string) => {
    try {
      const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/auth/register`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ name, email, password }),
      })

      if (!response.ok) {
        const error = await response.json()
        return { success: false, error: error.message || 'Registration failed' }
      }

      const data = await response.json()
      
      // Save to state and localStorage
      setToken(data.token)
      setUser({
        userId: data.userId,
        name: data.name,
        email: data.email,
      })

      localStorage.setItem('token', data.token)
      localStorage.setItem('user', JSON.stringify({
        userId: data.userId,
        name: data.name,
        email: data.email,
      }))

      return { success: true }
    } catch (error) {
      return { success: false, error: 'Network error. Please try again.' }
    }
  }

  const logout = () => {
    setUser(null)
    setToken(null)
    localStorage.removeItem('token')
    localStorage.removeItem('user')
  }

  const updateProfile = async (name: string) => {
    if (!token) {
      return { success: false, error: 'Not authenticated' }
    }

    try {
      const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/auth/profile`, {
        method: 'PUT',
        headers: { 
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify({ name }),
      })

      if (!response.ok) {
        const error = await response.json()
        return { success: false, error: error.message || 'Update failed' }
      }

      const data = await response.json()
      
      // Update user in state and localStorage
      const updatedUser = { ...user!, name: data.name }
      setUser(updatedUser)
      localStorage.setItem('user', JSON.stringify(updatedUser))

      return { success: true }
    } catch (error) {
      return { success: false, error: 'Network error. Please try again.' }
    }
  }

  const changePassword = async (currentPassword: string, newPassword: string) => {
    if (!token) {
      return { success: false, error: 'Not authenticated' }
    }

    try {
      const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/auth/password`, {
        method: 'PUT',
        headers: { 
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify({ currentPassword, newPassword }),
      })

      if (!response.ok) {
        const error = await response.json()
        return { success: false, error: error.message || 'Password change failed' }
      }

      return { success: true }
    } catch (error) {
      return { success: false, error: 'Network error. Please try again.' }
    }
  }

  const deleteAccount = async (password: string) => {
    if (!token) {
      return { success: false, error: 'Not authenticated' }
    }

    try {
      const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/auth/account`, {
        method: 'DELETE',
        headers: { 
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify({ password }),
      })

      if (!response.ok) {
        const error = await response.json()
        return { success: false, error: error.message || 'Account deletion failed' }
      }

      return { success: true }
    } catch (error) {
      return { success: false, error: 'Network error. Please try again.' }
    }
  }

  return (
    <AuthContext.Provider
      value={{
        user,
        token,
        login,
        register,
        updateProfile,
        changePassword,
        deleteAccount,
        logout,
        isAuthenticated: !!user && !!token,
        isLoading,
      }}
    >
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  return context
}
