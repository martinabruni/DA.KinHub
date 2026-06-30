import type { ReactNode } from 'react'
import { createContext, useCallback, useContext } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { useTranslation } from 'react-i18next'
import { apiClient } from '@/api/apiClient'
import { useAuthContext } from '@/store/authContext'
import type { User } from '@/types'

interface AuthProviderValue {
  user: User | null
  isAuthenticated: boolean
  isLoadingUser: boolean
  completeLogin: (accessToken: string) => Promise<void>
  logout: () => Promise<void>
}

const AuthProviderContext = createContext<AuthProviderValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const { setUser, setAccessToken, clearAuth, isAuthenticated, user } = useAuthContext()

  const { isLoading: isLoadingUser } = useQuery({
    queryKey: ['auth', 'me'],
    queryFn: async () => {
      const { data } = await apiClient.get<User>('/api/auth/me')
      setUser(data)
      return data
    },
    enabled: isAuthenticated,
    retry: false,
  })

  const logoutMutation = useMutation({
    mutationFn: async () => {
      await apiClient.post('/logout')
    },
    onSettled: () => {
      clearAuth()
      queryClient.clear()
      toast.success(t('auth.loggedOut'))
    },
  })

  const completeLogin = useCallback(
    async (accessToken: string) => {
      setAccessToken(accessToken)
      const { data } = await apiClient.get<User>('/api/auth/me')
      setUser(data)
      await queryClient.invalidateQueries({ queryKey: ['auth', 'me'] })
    },
    [queryClient, setAccessToken, setUser],
  )

  const logout = useCallback(async () => {
    await logoutMutation.mutateAsync()
  }, [logoutMutation])

  return (
    <AuthProviderContext.Provider
      value={{ user, isAuthenticated, isLoadingUser, completeLogin, logout }}
    >
      {children}
    </AuthProviderContext.Provider>
  )
}

export function useAuth() {
  const ctx = useContext(AuthProviderContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}
