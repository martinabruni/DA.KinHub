import { useEffect } from 'react'
import { Loader2 } from 'lucide-react'
import { redirectToIdentityLogin } from '@/config/appLinks'

export function LoginPage() {
  useEffect(() => {
    redirectToIdentityLogin(window.location.href)
  }, [])

  return (
    <div className="min-h-screen flex items-center justify-center">
      <Loader2 className="w-8 h-8 animate-spin text-primary" />
    </div>
  )
}
