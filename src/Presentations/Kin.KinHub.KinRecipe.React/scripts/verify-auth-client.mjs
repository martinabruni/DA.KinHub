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

// 3. The two axios clients must target the correct origins via the shared factory.
if (!apiClient.includes('createApiClient(KINRECIPE_API_URL)')) {
  throw new Error('KinRecipe apiClient must use VITE_KINRECIPE_API_URL.')
}

if (!apiClient.includes('createApiClient(IDENTITY_API_URL)')) {
  throw new Error('identityApiClient must use VITE_IDENTITY_API_URL.')
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
  const kinRecipeApiUrl = process.env.VITE_KINRECIPE_API_URL

  if (identityApiUrl && !distBundle.includes(identityApiUrl)) {
    throw new Error('Built bundle does not contain the configured identity API URL.')
  }

  if (kinRecipeApiUrl && !distBundle.includes(kinRecipeApiUrl)) {
    throw new Error('Built bundle does not contain the configured KinRecipe API URL.')
  }

  for (const endpoint of ['/api/auth/me', '/logout']) {
    if (kinRecipeApiUrl && distBundle.includes(`${kinRecipeApiUrl}${endpoint}`)) {
      throw new Error(`Built bundle still points ${endpoint} at the KinRecipe API URL.`)
    }
  }
}

console.log('KinRecipe auth client verification passed.')
