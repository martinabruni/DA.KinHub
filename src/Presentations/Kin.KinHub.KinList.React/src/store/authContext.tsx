import type { ReactNode } from 'react'
import { useCallback, useState } from 'react'
import type { User } from '@/types'
import { getAccessToken, setAccessToken as setSharedAccessToken } from '@shared/oauth/tokenStore'
import { AuthContext } from '@/store/authContextValue'

export function AuthContextProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null)
  const [accessToken, setAccessTokenState] = useState<string | null>(() => getAccessToken())

  const setAccessToken = useCallback((value: string | null) => {
    setSharedAccessToken(value)
    setAccessTokenState(value)
  }, [])

  const clearAuth = useCallback(() => {
    setSharedAccessToken(null)
    setAccessTokenState(null)
    setUser(null)
  }, [])

  return (
    <AuthContext.Provider
      value={{
        user,
        accessToken,
        setUser,
        setAccessToken,
        clearAuth,
        isAuthenticated: accessToken !== null,
      }}
    >
      {children}
    </AuthContext.Provider>
  )
}
