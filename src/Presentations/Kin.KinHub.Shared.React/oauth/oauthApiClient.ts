import { clearAccessToken, getAccessToken } from './tokenStore'

interface HttpError {
  response?: {
    status?: number
  }
}

interface AttachOAuthInterceptorsOptions {
  clearSessionStorageKeys?: string[]
  getUnauthorizedReturnTo?: () => string
  onAuthenticationRequired: (returnTo: string) => void | Promise<void>
  onHttpError?: (status: number, error: HttpError) => void
}

let authenticationRedirectStarted = false

function startAuthenticationRedirect(
  onAuthenticationRequired: AttachOAuthInterceptorsOptions['onAuthenticationRequired'],
  returnTo: string,
) {
  if (authenticationRedirectStarted) {
    return
  }

  authenticationRedirectStarted = true
  void Promise.resolve(onAuthenticationRequired(returnTo))
}

export function attachOAuthInterceptors<TClient>(
  client: TClient,
  {
    clearSessionStorageKeys = [],
    getUnauthorizedReturnTo = () => window.location.href,
    onAuthenticationRequired,
    onHttpError,
  }: AttachOAuthInterceptorsOptions,
): TClient {
  const httpClient = client as {
    interceptors: {
      request: {
        use: (onFulfilled: (config: { headers: Record<string, unknown> }) => { headers: Record<string, unknown> } | Promise<{ headers: Record<string, unknown> }>) => unknown
      }
      response: {
        use: (
          onFulfilled: (response: unknown) => unknown,
          onRejected: (error: HttpError) => Promise<never>,
        ) => unknown
      }
    }
  }

  httpClient.interceptors.request.use((config) => {
    const token = getAccessToken()
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }

    return config
  })

  httpClient.interceptors.response.use(
    (response) => response,
    async (error) => {
      const status = error.response?.status

      if (status === 401) {
        clearAccessToken()
        for (const key of clearSessionStorageKeys) {
          sessionStorage.removeItem(key)
        }

        startAuthenticationRedirect(onAuthenticationRequired, getUnauthorizedReturnTo())
        return Promise.reject(error)
      }

      if (status !== undefined && status >= 400) {
        onHttpError?.(status, error)
      }

      return Promise.reject(error)
    },
  )

  return client
}
