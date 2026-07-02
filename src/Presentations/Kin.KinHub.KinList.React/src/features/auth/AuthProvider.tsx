import type { ReactNode } from 'react'
import { useCallback } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { useTranslation } from 'react-i18next'
import { identityApiClient } from '@/api/apiClient'
import { useAuthContext } from '@/store/authContextValue'
import { AuthProviderContext } from '@/features/auth/authProviderContext'
import type { User } from '@/types'

export function AuthProvider({ children }: { children: ReactNode }) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const { setUser, setAccessToken, clearAuth, isAuthenticated, user } = useAuthContext()

  const { isLoading: isLoadingUser } = useQuery({
    queryKey: ['auth', 'me'],
    queryFn: async () => {
      const { data } = await identityApiClient.get<User>('/api/auth/me')
      setUser(data)
      return data
    },
    enabled: isAuthenticated,
    retry: false,
  })

  const logoutMutation = useMutation({
    mutationFn: async () => {
      await identityApiClient.post('/logout')
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
      const { data } = await identityApiClient.get<User>('/api/auth/me')
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
