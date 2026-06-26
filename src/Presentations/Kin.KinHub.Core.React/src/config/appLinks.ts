import type { FamilyMember } from '@/types'

const defaultIdentityUrl = 'http://localhost:5174'
const defaultKinRecipeUrl = 'http://localhost:5175'
const relayHashKey = 'relay'

export const identityUrl = import.meta.env.VITE_IDENTITY_URL ?? defaultIdentityUrl
export const kinRecipeUrl = import.meta.env.VITE_KINRECIPE_URL ?? defaultKinRecipeUrl

function encodeRelayPayload(payload: Record<string, string>) {
  return btoa(
    String.fromCharCode(...new TextEncoder().encode(JSON.stringify(payload))),
  )
}

function buildIdentityUrl(path: string) {
  return new URL(path, identityUrl)
}

export function buildIdentityLoginUrl(returnTo = window.location.href) {
  const url = buildIdentityUrl('/login')
  url.searchParams.set('returnTo', returnTo)
  return url.toString()
}

export function redirectToIdentityLogin(returnTo = window.location.href) {
  window.location.assign(buildIdentityLoginUrl(returnTo))
}

export function appendSessionToUrl(targetUrl: string, member: FamilyMember | null) {
  const url = new URL(targetUrl)
  const accessToken = localStorage.getItem('accessToken')
  const refreshToken = localStorage.getItem('refreshToken')
  const relayPayload: Record<string, string> = {}

  if (accessToken) {
    relayPayload.accessToken = accessToken
  }

  if (refreshToken) {
    relayPayload.refreshToken = refreshToken
  }

  if (member) {
    relayPayload.memberId = member.id
    relayPayload.memberName = member.name
  }

  if (Object.keys(relayPayload).length > 0) {
    const relayHash = new URLSearchParams({
      [relayHashKey]: encodeRelayPayload(relayPayload),
    })
    url.hash = relayHash.toString()
  }

  return url.toString()
}

export function buildKinRecipeLaunchUrl(member: FamilyMember | null, path = '/') {
  const targetUrl = new URL(path, kinRecipeUrl)
  return appendSessionToUrl(targetUrl.toString(), member)
}
