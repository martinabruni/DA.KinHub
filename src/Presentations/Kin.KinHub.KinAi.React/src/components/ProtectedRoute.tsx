import { Navigate, Outlet } from 'react-router-dom'
import { useAuthContext } from '@/store/authContext'

export function ProtectedRoute() {
  const { isAuthenticated } = useAuthContext()
  if (!isAuthenticated) return <Navigate to="/login" replace />
  return <Outlet />
}
