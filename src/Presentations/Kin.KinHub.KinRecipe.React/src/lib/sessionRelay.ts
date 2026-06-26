const ACTIVE_MEMBER_KEY = 'activeMember'
const relayHashKey = 'relay'
const relayKeys = ['accessToken', 'refreshToken', 'memberId', 'memberName'] as const

function decodeRelayPayload(encodedPayload: string) {
  try {
    const bytes = Uint8Array.from(atob(encodedPayload), (char) => char.charCodeAt(0))
    return JSON.parse(new TextDecoder().decode(bytes)) as Partial<Record<(typeof relayKeys)[number], string>>
  } catch {
    return null
  }
}

export function hydrateSessionFromUrl() {
  const url = new URL(window.location.href)
  const hashParams = new URLSearchParams(url.hash.startsWith('#') ? url.hash.slice(1) : url.hash)
  const relayPayload = hashParams.get(relayHashKey)
  const decodedRelay = relayPayload ? decodeRelayPayload(relayPayload) : null
  const accessToken = decodedRelay?.accessToken ?? url.searchParams.get('accessToken')
  const refreshToken = decodedRelay?.refreshToken ?? url.searchParams.get('refreshToken')
  const memberId = decodedRelay?.memberId ?? url.searchParams.get('memberId')
  const memberName = decodedRelay?.memberName ?? url.searchParams.get('memberName')

  let changed = false

  if (accessToken) {
    localStorage.setItem('accessToken', accessToken)
    changed = true
  }

  if (refreshToken) {
    localStorage.setItem('refreshToken', refreshToken)
    changed = true
  }

  if (memberId && memberName) {
    sessionStorage.setItem(
      ACTIVE_MEMBER_KEY,
      JSON.stringify({
        id: memberId,
        name: memberName,
      }),
    )
    changed = true
  }

  if (!changed) {
    return
  }

  relayKeys.forEach((key) => url.searchParams.delete(key))
  hashParams.delete(relayHashKey)
  const nextHash = hashParams.toString()
  const nextUrl = `${url.pathname}${url.search}${nextHash ? `#${nextHash}` : ''}`
  window.history.replaceState({}, document.title, nextUrl)
}
