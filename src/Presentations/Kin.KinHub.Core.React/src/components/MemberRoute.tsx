import { Navigate, Outlet } from 'react-router-dom'
import { useAuthContext } from '@/store/authContext'

export function MemberRoute() {
  const { activeMember } = useAuthContext()
  if (!activeMember) return <Navigate to="/select-member" replace />
  return <Outlet />
}
