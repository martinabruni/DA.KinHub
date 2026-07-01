import { readFileSync } from 'node:fs'
import { join } from 'node:path'

const authProviderPath = join(process.cwd(), 'src', 'features', 'auth', 'AuthProvider.tsx')
const authContextPath = join(process.cwd(), 'src', 'store', 'authContext.tsx')
const appLinksPath = join(process.cwd(), 'src', 'config', 'appLinks.ts')
const oauthConfigPath = join(process.cwd(), 'src', 'config', 'oauth.ts')
const oauthCallbackPath = join(process.cwd(), 'src', 'features', 'auth', 'pages', 'OAuthCallbackPage.tsx')
const apiClientPath = join(process.cwd(), 'src', 'api', 'apiClient.ts')
const sharedOauthClientPath = join(process.cwd(), '..', 'Kin.KinHub.Frontend.Shared', 'oauth', 'oauthClient.ts')
const sharedTokenStorePath = join(process.cwd(), '..', 'Kin.KinHub.Frontend.Shared', 'oauth', 'tokenStore.ts')

const authProvider = readFileSync(authProviderPath, 'utf8')
const authContext = readFileSync(authContextPath, 'utf8')
const appLinks = readFileSync(appLinksPath, 'utf8')
const oauthConfig = readFileSync(oauthConfigPath, 'utf8')
const oauthCallback = readFileSync(oauthCallbackPath, 'utf8')
const apiClient = readFileSync(apiClientPath, 'utf8')
const sharedOauthClient = readFileSync(sharedOauthClientPath, 'utf8')
const sharedTokenStore = readFileSync(sharedTokenStorePath, 'utf8')

const requiredAuthProviderChecks = [
  "identityApiClient.get<User>('/api/auth/me')",
  "identityApiClient.post('/logout')",
]

for (const check of requiredAuthProviderChecks) {
  if (!authProvider.includes(check)) {
    throw new Error(`Missing KinList auth verification marker: ${check}`)
  }
}

const forbiddenChecks = [
  '/api/auth/login',
  '/api/auth/register',
  '/api/auth/refresh',
  'localStorage',
]

for (const source of [authProvider, authContext, apiClient, appLinks, oauthConfig, oauthCallback, sharedOauthClient, sharedTokenStore]) {
  for (const check of forbiddenChecks) {
    if (source.includes(check)) {
      throw new Error(`KinList SPA must not rely on legacy auth storage or endpoints: ${check}`)
    }
  }
}

if (!apiClient.includes('const KINLIST_API_URL = getEnvUrl(') || !apiClient.includes('createApiClient(KINLIST_API_URL)')) {
  throw new Error('KinList apiClient must use VITE_KINLIST_API_URL.')
}

if (!apiClient.includes('const IDENTITY_API_URL = getEnvUrl(') || !apiClient.includes('createApiClient(IDENTITY_API_URL)')) {
  throw new Error('identityApiClient must use VITE_IDENTITY_API_URL.')
}

if (!appLinks.includes('startOAuthLogin(oauthClientConfig, returnTo)')) {
  throw new Error('KinList login redirect must use the shared OAuth client.')
}

if (!oauthCallback.includes('completeOAuthLogin(oauthClientConfig)')) {
  throw new Error('KinList OAuth callback must use the shared OAuth client.')
}

if (!oauthConfig.includes("clientId: 'kinhub-kinlist-spa'")) {
  throw new Error('KinList OAuth config must use the shared KinList SPA client id.')
}

if (!sharedOauthClient.includes("grant_type: 'authorization_code'")) {
  throw new Error('Shared OAuth client must exchange PKCE authorization codes.')
}

if (!sharedTokenStore.includes('let accessToken: string | null = null')) {
  throw new Error('Shared token store must keep the access token in memory only.')
}

console.log('KinList auth client verification passed.')
