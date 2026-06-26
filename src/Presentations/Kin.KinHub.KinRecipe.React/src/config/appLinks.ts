const defaultIdentityUrl = 'http://localhost:5174'

export const identityUrl = import.meta.env.VITE_IDENTITY_URL ?? defaultIdentityUrl

function buildIdentityUrl(path: string) {
  return new URL(path, identityUrl)
}

export function buildIdentityLoginUrl(returnTo = window.location.href) {
  const url = buildIdentityUrl('/login')
  url.searchParams.set('returnTo', returnTo)
  return url.toString()
}

export function buildIdentitySelectMemberUrl(returnTo = window.location.href) {
  const url = buildIdentityUrl('/select-member')
  url.searchParams.set('returnTo', returnTo)
  return url.toString()
}

export function redirectToIdentityLogin(returnTo = window.location.href) {
  window.location.assign(buildIdentityLoginUrl(returnTo))
}

export function redirectToIdentitySelectMember(returnTo = window.location.href) {
  window.location.assign(buildIdentitySelectMemberUrl(returnTo))
}
