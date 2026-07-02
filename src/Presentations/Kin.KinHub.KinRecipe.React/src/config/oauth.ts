import type { OAuthClientConfig } from '@shared/oauth/oauthClient'

const defaultIdentityApiUrl = 'http://localhost:5001'

function getEnvUrl(value: unknown, fallback: string) {
  return typeof value === 'string' && value.trim() ? value.trim() : fallback
}

export const oauthClientConfig: OAuthClientConfig = {
  authorizationServerUrl: getEnvUrl(
    import.meta.env.VITE_IDENTITY_API_URL,
    defaultIdentityApiUrl,
  ),
  clientId: 'kinhub-kinrecipe-spa',
  redirectUri: new URL('/oauth/callback', window.location.origin).toString(),
  scope: 'kinhub.api',
  postLoginPath: '/',
}
