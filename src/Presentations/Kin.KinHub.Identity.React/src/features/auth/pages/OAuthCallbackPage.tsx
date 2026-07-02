import { useEffect } from 'react'
import { Loader2 } from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import { toast } from 'sonner'
import { completeOAuthLogin } from '@shared/oauth/oauthClient'
import { oauthClientConfig } from '@/config/oauth'
import { useAuth } from '@/features/auth/AuthProvider'

export function OAuthCallbackPage() {
  const navigate = useNavigate()
  const { completeLogin } = useAuth()

  useEffect(() => {
    let isCancelled = false

    const run = async () => {
      try {
        const result = await completeOAuthLogin(oauthClientConfig)
        if (isCancelled) {
          return
        }

        await completeLogin(result.accessToken)
        navigate(result.returnTo, { replace: true })
      } catch (error) {
        if (isCancelled) {
          return
        }

        toast.error(error instanceof Error ? error.message : 'OAuth login failed.')
        navigate('/login', { replace: true })
      }
    }

    void run()

    return () => {
      isCancelled = true
    }
  }, [completeLogin, navigate])

  return (
    <div className="min-h-screen flex items-center justify-center">
      <Loader2 className="w-8 h-8 animate-spin text-primary" />
    </div>
  )
}
