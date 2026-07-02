import { createContext, useContext } from 'react'
import type { User } from '@/types'

export interface AuthContextValue {
  user: User | null
  accessToken: string | null
  setUser: (user: User | null) => void
  setAccessToken: (accessToken: string | null) => void
  clearAuth: () => void
  isAuthenticated: boolean
}

export const AuthContext = createContext<AuthContextValue | null>(null)

export function useAuthContext() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuthContext must be used within AuthContextProvider')
  return ctx
}
