import type { ReactNode } from 'react'
import { createContext, useCallback, useContext, useState } from 'react'
import type { FamilyMember, User } from '@/types'
import { getAccessToken, setAccessToken as setSharedAccessToken } from '@shared/oauth/tokenStore'

const ACTIVE_MEMBER_KEY = 'activeMember'

function loadActiveMember(): FamilyMember | null {
  try {
    const raw = sessionStorage.getItem(ACTIVE_MEMBER_KEY)
    return raw ? (JSON.parse(raw) as FamilyMember) : null
  } catch {
    return null
  }
}

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
  const [activeMember, setActiveMemberState] = useState<FamilyMember | null>(loadActiveMember)
  const [accessToken, setAccessTokenState] = useState<string | null>(() => getAccessToken())

  const setAccessToken = useCallback((value: string | null) => {
    setSharedAccessToken(value)
    setAccessTokenState(value)
  }, [])

  const setActiveMember = useCallback((member: FamilyMember) => {
    sessionStorage.setItem(ACTIVE_MEMBER_KEY, JSON.stringify(member))
    setActiveMemberState(member)
  }, [])

  const clearActiveMember = useCallback(() => {
    sessionStorage.removeItem(ACTIVE_MEMBER_KEY)
    setActiveMemberState(null)
  }, [])

  const clearAuth = useCallback(() => {
    setSharedAccessToken(null)
    sessionStorage.removeItem(ACTIVE_MEMBER_KEY)
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
