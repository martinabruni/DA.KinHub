import { createContext, useContext } from 'react'
import type { User } from '@/types'

export interface AuthProviderValue {
  user: User | null
  isAuthenticated: boolean
  isLoadingUser: boolean
  completeLogin: (accessToken: string) => Promise<void>
  logout: () => Promise<void>
}

export const AuthProviderContext = createContext<AuthProviderValue | null>(null)

export function useAuth() {
  const ctx = useContext(AuthProviderContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}
