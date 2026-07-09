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
const KINHUB_API_URL = getEnvUrl(
  import.meta.env.VITE_KINHUB_API_URL,
  'http://localhost:5000',
)

function createApiClient(baseURL: string) {
  return attachOAuthInterceptors(axios.create({
    baseURL,
    headers: { 'Content-Type': 'application/json' },
  }), {
    onAuthenticationRequired: (returnTo) => {
      return startOAuthLogin(oauthClientConfig, returnTo)
    },
    onHttpError: (status, error) => {
      toast.error(getStatusAwareErrorMessage(error, status))
    },
  })
}

export const apiClient = createApiClient(BASE_URL)
// Family/Services now live on App.Functions, not Identity.Api.
export const kinHubApiClient = createApiClient(KINHUB_API_URL)
