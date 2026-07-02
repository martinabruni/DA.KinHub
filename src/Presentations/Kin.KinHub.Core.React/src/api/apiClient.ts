import axios from 'axios'
import { toast } from 'sonner'
import { redirectToIdentityLogin } from '@/config/appLinks'
import { getStatusAwareErrorMessage } from '@/lib/errors'
import { attachOAuthInterceptors } from '@shared/oauth/oauthApiClient'

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
  clearSessionStorageKeys: ['activeMember'],
  onAuthenticationRequired: (returnTo) => {
    redirectToIdentityLogin(returnTo)
  },
  onHttpError: (status, error) => {
    toast.error(getStatusAwareErrorMessage(error, status))
  },
})
