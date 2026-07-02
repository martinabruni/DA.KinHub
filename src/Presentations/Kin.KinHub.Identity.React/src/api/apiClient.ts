import axios from 'axios'
import { toast } from 'sonner'
import { getStatusAwareErrorMessage } from '@/lib/errors'
import { attachOAuthInterceptors } from '@shared/oauth/oauthApiClient'
import { startOAuthLogin } from '@shared/oauth/oauthClient'
import { oauthClientConfig } from '@/config/oauth'

const getEnvUrl = (value: unknown, fallback: string) => {
  return typeof value === 'string' && value.trim() ? value.trim() : fallback
}

const BASE_URL = getEnvUrl(
  import.meta.env.VITE_IDENTITY_API_URL,
  'http://localhost:5001',
)

export const apiClient = attachOAuthInterceptors(axios.create({
  baseURL: BASE_URL,
  headers: { 'Content-Type': 'application/json' },
}), {
  onAuthenticationRequired: (returnTo) => {
    return startOAuthLogin(oauthClientConfig, returnTo)
  },
  onHttpError: (status, error) => {
    toast.error(getStatusAwareErrorMessage(error, status))
  },
})
