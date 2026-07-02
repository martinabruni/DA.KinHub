import axios from 'axios'
import { toast } from 'sonner'
import { redirectToIdentityLogin } from '@/config/appLinks'
import { getStatusAwareErrorMessage } from '@/lib/errors'
import { attachOAuthInterceptors } from '@shared/oauth/oauthApiClient'

const getEnvUrl = (value: unknown, fallback: string) => {
  return typeof value === 'string' && value.trim() ? value.trim() : fallback
}

const KINRECIPE_API_URL = getEnvUrl(
  import.meta.env.VITE_KINRECIPE_API_URL,
  'http://localhost:5000',
)
const IDENTITY_API_URL = getEnvUrl(
  import.meta.env.VITE_IDENTITY_API_URL,
  'http://localhost:5001',
)
const KINLIST_API_URL = getEnvUrl(
  import.meta.env.VITE_KINLIST_API_URL,
  'http://localhost:5002',
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

export const apiClient = createApiClient(KINRECIPE_API_URL)
export const identityApiClient = createApiClient(IDENTITY_API_URL)
export const kinListApiClient = createApiClient(KINLIST_API_URL)
