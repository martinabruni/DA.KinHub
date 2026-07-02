import axios from 'axios'
import { toast } from 'sonner'
import { redirectToIdentityLogin } from '@/config/appLinks'
import { getStatusAwareErrorMessage } from '@/lib/errors'
import { clearAccessToken, getAccessToken } from '@shared/oauth/tokenStore'

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
let loginRedirectStarted = false

function createApiClient(baseURL: string) {
  const client = axios.create({
    baseURL,
    headers: { 'Content-Type': 'application/json' },
  })

  client.interceptors.request.use((config) => {
    const token = getAccessToken()
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }

    return config
  })

  client.interceptors.response.use(
    (response) => response,
    async (error) => {
      const status = error.response?.status

      if (status === 401) {
        clearAccessToken()
        sessionStorage.removeItem('activeMember')
        if (!loginRedirectStarted) {
          loginRedirectStarted = true
          redirectToIdentityLogin(window.location.href)
        }
        return Promise.reject(error)
      }

      if (status !== undefined && status >= 400) {
        toast.error(getStatusAwareErrorMessage(error, status))
      }

      return Promise.reject(error)
    },
  )

  return client
}

export const apiClient = createApiClient(KINRECIPE_API_URL)
export const identityApiClient = createApiClient(IDENTITY_API_URL)
export const kinListApiClient = createApiClient(KINLIST_API_URL)
