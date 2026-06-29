const defaultIdentityUrl = 'http://localhost:5174'
const defaultCoreUrl = 'http://localhost:5173'
const relayHashKey = 'relay'

export const identityUrl = import.meta.env.VITE_IDENTITY_URL ?? defaultIdentityUrl
export const coreUrl = import.meta.env.VITE_CORE_URL ?? defaultCoreUrl

function buildIdentityUrl(path: string) {
  return new URL(path, identityUrl)
}

function buildCoreUrl(path: string) {
  return new URL(path, coreUrl)
}

function encodeRelayPayload(payload: Record<string, string>) {
  return btoa(
    String.fromCharCode(...new TextEncoder().encode(JSON.stringify(payload))),
  )
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
  window.location.assign(buildIdentityLoginUrl(returnTo))
}

export function appendSessionToUrl(
  targetUrl: string,
  member: { id: string; name: string } | null,
) {
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
