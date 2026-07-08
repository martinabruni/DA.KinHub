import { existsSync, readFileSync, readdirSync } from 'node:fs'
import { join } from 'node:path'

const authProviderPath = join(process.cwd(), 'src', 'features', 'auth', 'AuthProvider.tsx')
const apiClientPath = join(process.cwd(), 'src', 'api', 'apiClient.ts')
const distAssetsPath = join(process.cwd(), 'dist', 'assets')

const authProvider = readFileSync(authProviderPath, 'utf8')
const apiClient = readFileSync(apiClientPath, 'utf8')

// 1. Identity-bound auth calls must go through the dedicated identity client so that
//    authentication is centralized in KinHub Identity and never handled by the KinRecipe API.
const identityClientChecks = [
  "identityApiClient.get<User>('/api/auth/me')",
  "startOAuthLogout(oauthClientConfig)",
]

for (const check of identityClientChecks) {
  if (!authProvider.includes(check)) {
    throw new Error(`Missing KinRecipe identity auth marker: ${check}`)
  }
}

// 2. The KinRecipe API client must never handle authentication endpoints, and the SPA must not
//    perform password login/registration or hold refresh tokens: KinHub Identity is the OAuth
//    broker and renewal happens via a silent top-level authorize, not a SPA refresh_token grant.
const forbiddenChecks = [
  "apiClient.get<User>('/api/auth/me')",
  '/api/auth/login',
  '/api/auth/register',
  'post<AuthTokens>',
  'refreshToken',
]

for (const check of forbiddenChecks) {
  if (authProvider.includes(check)) {
    throw new Error(`KinRecipe AuthProvider must not reference forbidden auth construct: ${check}`)
  }
}

// 3. The non-identity SPA now targets a single KinHub backend URL plus the dedicated Identity API.
if (!apiClient.includes('createApiClient(KINHUB_API_URL)')) {
  throw new Error('KinRecipe apiClient must use VITE_KINHUB_API_URL.')
}

if (!apiClient.includes('createApiClient(IDENTITY_API_URL)')) {
  throw new Error('identityApiClient must use VITE_IDENTITY_API_URL.')
}

if (!apiClient.includes('export const kinListApiClient = createApiClient(KINHUB_API_URL)')) {
  throw new Error('KinList calls inside the unified SPA must use VITE_KINHUB_API_URL.')
}

// 4. The access token must be held in memory via the shared OAuth token store, never in localStorage.
if (apiClient.includes('localStorage')) {
  throw new Error('KinRecipe apiClient must not read/write the access token from localStorage.')
}

if (!apiClient.includes('@shared/oauth/oauthApiClient')) {
  throw new Error('KinRecipe apiClient must use the shared OAuth API client.')
}

// 5. When a built bundle is present, make sure auth endpoints are not pinned to the KinRecipe API.
if (existsSync(distAssetsPath)) {
  const distBundle = readdirSync(distAssetsPath)
    .filter((file) => file.endsWith('.js'))
    .map((file) => readFileSync(join(distAssetsPath, file), 'utf8'))
    .join('\n')

  const identityApiUrl = process.env.VITE_IDENTITY_API_URL
  const kinhubApiUrl = process.env.VITE_KINHUB_API_URL

  if (identityApiUrl && !distBundle.includes(identityApiUrl)) {
    throw new Error('Built bundle does not contain the configured identity API URL.')
  }

  if (kinhubApiUrl && !distBundle.includes(kinhubApiUrl)) {
    throw new Error('Built bundle does not contain the configured KinHub API URL.')
  }

  for (const endpoint of ['/api/auth/me', '/logout']) {
    if (kinhubApiUrl && distBundle.includes(`${kinhubApiUrl}${endpoint}`)) {
      throw new Error(`Built bundle still points ${endpoint} at the KinHub API URL.`)
    }
  }
}

console.log('KinRecipe auth client verification passed.')
