import { readFileSync, readdirSync } from 'node:fs'
import { join } from 'node:path'

const authProviderPath = join(process.cwd(), 'src', 'features', 'auth', 'AuthProvider.tsx')
const apiClientPath = join(process.cwd(), 'src', 'api', 'apiClient.ts')
const distAssetsPath = join(process.cwd(), 'dist', 'assets')

const authProvider = readFileSync(authProviderPath, 'utf8')
const apiClient = readFileSync(apiClientPath, 'utf8')
const distBundle = readdirSync(distAssetsPath)
  .filter((file) => file.endsWith('.js'))
  .map((file) => readFileSync(join(distAssetsPath, file), 'utf8'))
  .join('\n')

const identityClientChecks = [
  'identityApiClient.get<User>("/api/auth/me")',
  'identityApiClient.post<AuthTokens>(',
  'identityApiClient.post("/api/auth/register", payload)',
  'identityApiClient.post("/api/auth/logout", { refreshToken })',
]

for (const check of identityClientChecks) {
  if (!authProvider.includes(check)) {
    throw new Error(`Missing KinRecipe auth verification marker: ${check}`)
  }
}

const forbiddenChecks = [
  'apiClient.get<User>("/api/auth/me")',
  'apiClient.post<AuthTokens>(',
  'apiClient.post("/api/auth/register", payload)',
  'apiClient.post("/api/auth/logout", { refreshToken })',
]

for (const check of forbiddenChecks) {
  if (authProvider.includes(check)) {
    throw new Error(`KinRecipe API client must not handle auth endpoint: ${check}`)
  }
}

if (!apiClient.includes('baseURL: KINRECIPE_API_URL')) {
  throw new Error('KinRecipe apiClient must use VITE_KINRECIPE_API_URL.')
}

if (!apiClient.includes('baseURL: IDENTITY_API_URL')) {
  throw new Error('identityApiClient must use VITE_IDENTITY_API_URL.')
}

const identityApiUrl = process.env.VITE_IDENTITY_API_URL
const kinRecipeApiUrl = process.env.VITE_KINRECIPE_API_URL

if (identityApiUrl && !distBundle.includes(identityApiUrl)) {
  throw new Error('Built bundle does not contain the configured identity API URL.')
}

if (kinRecipeApiUrl && !distBundle.includes(kinRecipeApiUrl)) {
  throw new Error('Built bundle does not contain the configured KinRecipe API URL.')
}

for (const endpoint of ['/api/auth/me', '/api/auth/login', '/api/auth/register', '/api/auth/logout']) {
  if (kinRecipeApiUrl && distBundle.includes(`${kinRecipeApiUrl}${endpoint}`)) {
    throw new Error(`Built bundle still points ${endpoint} at the KinRecipe API URL.`)
  }
}

console.log('KinRecipe auth client verification passed.')
