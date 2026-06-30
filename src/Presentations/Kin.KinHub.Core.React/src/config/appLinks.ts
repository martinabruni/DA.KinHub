import type { FamilyMember } from '@/types'
import { startOAuthLogin } from '@shared/oauth/oauthClient'
import { oauthClientConfig } from '@/config/oauth'

const defaultIdentityUrl = 'http://localhost:5174'
const defaultKinRecipeUrl = 'http://localhost:5175'

export const identityUrl = import.meta.env.VITE_IDENTITY_URL ?? defaultIdentityUrl
export const kinRecipeUrl = import.meta.env.VITE_KINRECIPE_URL ?? defaultKinRecipeUrl

function buildIdentityUrl(path: string) {
  return new URL(path, identityUrl)
}

export function buildIdentityLoginUrl(returnTo = window.location.href) {
  const url = buildIdentityUrl('/login')
  url.searchParams.set('returnTo', returnTo)
  return url.toString()
}

export function redirectToIdentityLogin(returnTo = window.location.href) {
  void startOAuthLogin(oauthClientConfig, returnTo)
}

export function buildKinRecipeLaunchUrl(_member: FamilyMember | null, path = '/') {
  const targetUrl = new URL(path, kinRecipeUrl)
  return targetUrl.toString()
}
