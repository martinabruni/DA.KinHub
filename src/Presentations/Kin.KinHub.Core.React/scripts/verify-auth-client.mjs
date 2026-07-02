import { existsSync, readFileSync, readdirSync } from 'node:fs'
import { join } from 'node:path'

const authProviderPath = join(process.cwd(), 'src', 'features', 'auth', 'AuthProvider.tsx')
const apiClientPath = join(process.cwd(), 'src', 'api', 'apiClient.ts')
const distAssetsPath = join(process.cwd(), 'dist', 'assets')

const authProvider = readFileSync(authProviderPath, 'utf8')
const apiClient = readFileSync(apiClientPath, 'utf8')

const requiredChecks = [
  "apiClient.get<User>('/api/auth/me')",
  "startOAuthLogout(oauthClientConfig)",
  "import.meta.env.VITE_IDENTITY_API_URL",
  "@shared/oauth/oauthApiClient",
]

for (const check of requiredChecks) {
  if (!`${authProvider}\n${apiClient}`.includes(check)) {
    throw new Error(`Missing Core auth wiring marker: ${check}`)
  }
}

const forbiddenChecks = [
  '/api/auth/login',
  '/api/auth/refresh',
  'refreshToken',
  'localStorage',
]

for (const check of forbiddenChecks) {
  if (`${authProvider}\n${apiClient}`.includes(check)) {
    throw new Error(`Core auth wiring must not reference forbidden construct: ${check}`)
  }
}

if (!apiClient.includes('redirectToIdentityLogin(window.location.href)')) {
  if (!apiClient.includes('onAuthenticationRequired')) {
    throw new Error('Core apiClient must redirect 401 responses through the shared OAuth login flow.')
  }
}

if (existsSync(distAssetsPath)) {
  const distBundle = readdirSync(distAssetsPath)
    .filter((file) => file.endsWith('.js'))
    .map((file) => readFileSync(join(distAssetsPath, file), 'utf8'))
    .join('\n')

  const identityApiUrl = process.env.VITE_IDENTITY_API_URL

  if (identityApiUrl && !distBundle.includes(identityApiUrl)) {
    throw new Error('Built bundle does not contain the configured identity API URL.')
  }

  for (const endpoint of ['/api/auth/login', '/api/auth/refresh']) {
    if (distBundle.includes(endpoint)) {
      throw new Error(`Built bundle still references forbidden endpoint ${endpoint}.`)
    }
  }
}

console.log('Core auth client verification passed.')
