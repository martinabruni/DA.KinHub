import { useEffect } from 'react'
import { Outlet } from 'react-router-dom'
import { Loader2 } from 'lucide-react'
import { appendSessionToUrl, buildCoreSelectMemberUrl } from '@/config/appLinks'
import { useAuthContext } from '@/store/authContext'

export function MemberRoute() {
  const { activeMember } = useAuthContext()

  useEffect(() => {
    if (!activeMember) {
      window.location.assign(
        appendSessionToUrl(buildCoreSelectMemberUrl(), activeMember),
      )
    }
  }, [activeMember])

  if (!activeMember) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <Loader2 className="w-8 h-8 animate-spin text-primary" />
      </div>
    )
  }

  return <Outlet />
}
