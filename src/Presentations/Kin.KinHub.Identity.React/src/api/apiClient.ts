import axios from 'axios'
import { toast } from 'sonner'
import { getStatusAwareErrorMessage } from '@/lib/errors'
import { getAccessToken } from '@shared/oauth/tokenStore'

const getEnvUrl = (value: unknown, fallback: string) => {
  return typeof value === 'string' && value.trim() ? value.trim() : fallback
}

const BASE_URL = getEnvUrl(
  import.meta.env.VITE_IDENTITY_API_URL,
  'http://localhost:5001',
)

export const apiClient = axios.create({
  baseURL: BASE_URL,
  headers: { 'Content-Type': 'application/json' },
})

apiClient.interceptors.request.use((config) => {
  const token = getAccessToken()
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }

  return config
})

apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const status = error.response?.status

    if (status !== undefined && status >= 400) {
      toast.error(getStatusAwareErrorMessage(error, status))
    }

    return Promise.reject(error)
  },
)
