import type { ReactNode } from 'react'
import { createContext, useCallback, useContext, useState } from 'react'
import type { FamilyMember, User } from '@/types'
import { getAccessToken, setAccessToken as setSharedAccessToken } from '@shared/oauth/tokenStore'

interface AuthContextValue {
  user: User | null
  accessToken: string | null
  activeMember: FamilyMember | null
  setUser: (user: User | null) => void
  setAccessToken: (accessToken: string | null) => void
  setActiveMember: (member: FamilyMember) => void
  clearActiveMember: () => void
  clearAuth: () => void
  isAuthenticated: boolean
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthContextProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null)
  const [accessToken, setAccessTokenState] = useState<string | null>(() => getAccessToken())
  const [activeMember, setActiveMemberState] = useState<FamilyMember | null>(null)

  const setAccessToken = useCallback((value: string | null) => {
    setSharedAccessToken(value)
    setAccessTokenState(value)
  }, [])

  const setActiveMember = useCallback((member: FamilyMember) => {
    setActiveMemberState(member)
  }, [])

  const clearActiveMember = useCallback(() => {
    setActiveMemberState(null)
  }, [])

  const clearAuth = useCallback(() => {
    setSharedAccessToken(null)
    setAccessTokenState(null)
    setUser(null)
    setActiveMemberState(null)
  }, [])

  return (
    <AuthContext.Provider
      value={{
        user,
        accessToken,
        activeMember,
        setUser,
        setAccessToken,
        setActiveMember,
        clearActiveMember,
        clearAuth,
        isAuthenticated: accessToken !== null,
      }}
    >
      {children}
    </AuthContext.Provider>
  )
}

export function useAuthContext() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuthContext must be used within AuthContextProvider')
  return ctx
}
