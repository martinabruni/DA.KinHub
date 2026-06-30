const defaultCoreUrl = 'http://localhost:5173'
const defaultKinRecipeUrl = 'http://localhost:5175'
const defaultKinListUrl = 'http://localhost:5176'

export const coreUrl = import.meta.env.VITE_CORE_URL ?? defaultCoreUrl
export const kinRecipeUrl = import.meta.env.VITE_KINRECIPE_URL ?? defaultKinRecipeUrl
export const kinListUrl = import.meta.env.VITE_KINLIST_URL ?? defaultKinListUrl

export function buildKinRecipeLaunchUrl(path = '/') {
  const targetUrl = new URL(path, kinRecipeUrl)
  return targetUrl.toString()
}

export function buildKinListLaunchUrl(path = '/') {
  const targetUrl = new URL(path, kinListUrl)
  return targetUrl.toString()
}

export function buildCoreSelectMemberUrl(returnTo?: string | null) {
  const targetUrl = new URL('/select-member', coreUrl)

  if (returnTo) {
    targetUrl.searchParams.set('returnTo', returnTo)
  }

  return targetUrl.toString()
}
