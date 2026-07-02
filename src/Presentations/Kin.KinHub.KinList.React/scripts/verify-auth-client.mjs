import { existsSync, readFileSync, readdirSync } from 'node:fs'
import { join } from 'node:path'

const authProviderPath = join(process.cwd(), 'src', 'features', 'auth', 'AuthProvider.tsx')
const apiClientPath = join(process.cwd(), 'src', 'api', 'apiClient.ts')
const distAssetsPath = join(process.cwd(), 'dist', 'assets')

const authProvider = readFileSync(authProviderPath, 'utf8')
const apiClient = readFileSync(apiClientPath, 'utf8')

// 1. Identity-bound auth calls must go through the dedicated identity client so that
//    authentication is centralized in KinHub Identity and never handled by the Kin List API.
const identityClientChecks = [
  "identityApiClient.get<User>('/api/auth/me')",
  "startOAuthLogout(oauthClientConfig)",
]

for (const check of identityClientChecks) {
  if (!authProvider.includes(check)) {
    throw new Error(`Missing Kin List identity auth marker: ${check}`)
  }
}

// 2. The Kin List API client must never be used for authentication endpoints.
const forbiddenChecks = [
  "apiClient.get<User>('/api/auth/me')",
  "apiClient.post('/api/auth/login'",
  "apiClient.post('/api/auth/register'",
]

for (const check of forbiddenChecks) {
  if (authProvider.includes(check)) {
    throw new Error(`Kin List API client must not handle auth endpoint: ${check}`)
  }
}

// 3. The two axios clients must target the correct origins.
if (!apiClient.includes('createApiClient(KINLIST_API_URL)')) {
  throw new Error('Kin List apiClient must use VITE_KINLIST_API_URL.')
}

if (!apiClient.includes('createApiClient(IDENTITY_API_URL)')) {
  throw new Error('identityApiClient must use VITE_IDENTITY_API_URL.')
}

// 4. The access token must be held in memory via the shared OAuth token store,
//    never persisted to localStorage / sessionStorage.
if (apiClient.includes('localStorage') || apiClient.includes('sessionStorage')) {
  throw new Error('Kin List apiClient must not read/write the access token from web storage.')
}

if (!apiClient.includes('@shared/oauth/oauthApiClient')) {
  throw new Error('Kin List apiClient must use the shared OAuth API client.')
}

// 5. When a built bundle is present, make sure auth endpoints are not pinned to the Kin List API.
if (existsSync(distAssetsPath)) {
  const distBundle = readdirSync(distAssetsPath)
    .filter((file) => file.endsWith('.js'))
    .map((file) => readFileSync(join(distAssetsPath, file), 'utf8'))
    .join('\n')

  const identityApiUrl = process.env.VITE_IDENTITY_API_URL
  const kinListApiUrl = process.env.VITE_KINLIST_API_URL

  if (identityApiUrl && !distBundle.includes(identityApiUrl)) {
    throw new Error('Built bundle does not contain the configured identity API URL.')
  }

  if (kinListApiUrl && !distBundle.includes(kinListApiUrl)) {
    throw new Error('Built bundle does not contain the configured Kin List API URL.')
  }

  for (const endpoint of ['/api/auth/me', '/api/auth/login', '/api/auth/register', '/logout']) {
    if (kinListApiUrl && distBundle.includes(`${kinListApiUrl}${endpoint}`)) {
      throw new Error(`Built bundle still points ${endpoint} at the Kin List API URL.`)
    }
  }
}

console.log('Kin List auth client verification passed.')
