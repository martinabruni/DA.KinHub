import { startOAuthLogin } from '@shared/oauth/oauthClient'
import { oauthClientConfig } from '@/config/oauth'

const defaultIdentityUrl = 'http://localhost:5174'
const defaultCoreUrl = 'http://localhost:5173'

export const identityUrl = import.meta.env.VITE_IDENTITY_URL ?? defaultIdentityUrl
export const coreUrl = import.meta.env.VITE_CORE_URL ?? defaultCoreUrl

function buildIdentityUrl(path: string) {
  return new URL(path, identityUrl)
}

function buildCoreUrl(path: string) {
  return new URL(path, coreUrl)
}

export function buildIdentityLoginUrl(returnTo = window.location.href) {
  const url = buildIdentityUrl('/login')
  url.searchParams.set('returnTo', returnTo)
  return url.toString()
}

export function buildCoreProfileUrl() {
  return buildCoreUrl('/profile').toString()
}

export function buildCoreFamilyUrl() {
  return buildCoreUrl('/family').toString()
}

export function buildCoreServicesUrl() {
  return buildCoreUrl('/services').toString()
}

export function buildCoreSelectMemberUrl(returnTo = window.location.href) {
  const url = buildCoreUrl('/select-member')
  url.searchParams.set('returnTo', returnTo)
  return url.toString()
}

export function redirectToIdentityLogin(returnTo = window.location.href) {
  void startOAuthLogin(oauthClientConfig, returnTo)
}
