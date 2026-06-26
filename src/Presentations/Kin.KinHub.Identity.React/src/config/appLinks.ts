import type { FamilyMember } from '@/types'

const defaultCoreUrl = 'http://localhost:5173'
const defaultKinRecipeUrl = 'http://localhost:5175'
const relayHashKey = 'relay'

export const coreUrl = import.meta.env.VITE_CORE_URL ?? defaultCoreUrl
export const kinRecipeUrl = import.meta.env.VITE_KINRECIPE_URL ?? defaultKinRecipeUrl

function encodeRelayPayload(payload: Record<string, string>) {
  return btoa(
    String.fromCharCode(...new TextEncoder().encode(JSON.stringify(payload))),
  )
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

export function buildCoreSelectMemberUrl(returnTo?: string | null) {
  const targetUrl = new URL('/select-member', coreUrl)

  if (returnTo) {
    targetUrl.searchParams.set('returnTo', returnTo)
  }

  return targetUrl.toString()
}
