import { useEffect } from 'react'
import { Loader2 } from 'lucide-react'
import { identityUrl } from '@/config/appLinks'

export function RegisterPage() {
  useEffect(() => {
    const targetUrl = new URL('/register', identityUrl)
    targetUrl.searchParams.set('returnTo', window.location.href)
    window.location.assign(targetUrl.toString())
  }, [])

  return (
    <div className="min-h-screen flex items-center justify-center">
      <Loader2 className="w-8 h-8 animate-spin text-primary" />
    </div>
  )
}
