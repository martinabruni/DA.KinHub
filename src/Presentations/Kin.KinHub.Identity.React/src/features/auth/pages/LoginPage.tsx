import { useEffect } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { Loader2 } from 'lucide-react'
import { startOAuthLogin } from '@shared/oauth/oauthClient'
import { oauthClientConfig } from '@/config/oauth'

export function LoginPage() {
  const [searchParams] = useSearchParams()

  useEffect(() => {
    const returnTo = searchParams.get('returnTo') ?? oauthClientConfig.postLoginPath
    void startOAuthLogin(oauthClientConfig, returnTo)
  }, [searchParams])

  const returnTo = searchParams.get('returnTo')
  const registerHref = returnTo
    ? `/register?returnTo=${encodeURIComponent(returnTo)}`
    : '/register'

  return (
    <div className="min-h-dvh flex flex-col items-center justify-center gap-4 bg-background px-4">
      <Loader2 className="w-8 h-8 animate-spin text-primary" />
      <p className="text-sm text-muted-foreground">Redirecting to KinHub sign-in…</p>
      <Link className="text-sm font-medium text-primary hover:underline" to={registerHref}>
        Create account
      </Link>
    </div>
  )
}
