import { useEffect } from 'react'
import { Outlet } from 'react-router-dom'
import { Loader2 } from 'lucide-react'
import { redirectToIdentityLogin } from '@/config/appLinks'
import { useAuth } from '@/features/auth/authProviderContext'

export function ProtectedRoute() {
  const { isAuthenticated, isLoadingUser } = useAuth()

  useEffect(() => {
    if (!isLoadingUser && !isAuthenticated) {
      redirectToIdentityLogin()
    }
  }, [isAuthenticated, isLoadingUser])

  if (isLoadingUser) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <Loader2 className="w-8 h-8 animate-spin text-primary" />
      </div>
    )
  }

  if (!isAuthenticated) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <Loader2 className="w-8 h-8 animate-spin text-primary" />
      </div>
    )
  }

  return <Outlet />
}
