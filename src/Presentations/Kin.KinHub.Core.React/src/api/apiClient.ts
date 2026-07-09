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
const KINHUB_API_URL = getEnvUrl(
  import.meta.env.VITE_KINHUB_API_URL,
  'http://localhost:5000',
)

function createApiClient(baseURL: string) {
  return attachOAuthInterceptors(axios.create({
    baseURL,
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
}

export const apiClient = createApiClient(BASE_URL)
// Family/Services now live on App.Functions, not Identity.Api.
export const kinHubApiClient = createApiClient(KINHUB_API_URL)
