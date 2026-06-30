export interface OAuthClientConfig {
  authorizationServerUrl: string
  clientId: string
  redirectUri: string
  scope: string
  postLoginPath: string
}

interface OAuthTransaction {
  codeVerifier: string
  createdAt: string
  returnTo: string
  state: string
}

interface OAuthTokenResponse {
  access_token: string
}

const transactionKeyPrefix = 'kinhub.oauth.transaction'

function getTransactionKey(clientId: string) {
  return `${transactionKeyPrefix}.${clientId}`
}

function toBase64Url(bytes: Uint8Array) {
  const base64 = btoa(String.fromCharCode(...bytes))
  return base64.replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/g, '')
}

function createRandomString(length = 32) {
  const bytes = crypto.getRandomValues(new Uint8Array(length))
  return toBase64Url(bytes)
}

async function createCodeChallenge(codeVerifier: string) {
  const digest = await crypto.subtle.digest(
    'SHA-256',
    new TextEncoder().encode(codeVerifier),
  )

  return toBase64Url(new Uint8Array(digest))
}

function readTransaction(clientId: string) {
  const raw = sessionStorage.getItem(getTransactionKey(clientId))
  if (!raw) {
    return null
  }

  try {
    return JSON.parse(raw) as OAuthTransaction
  } catch {
    sessionStorage.removeItem(getTransactionKey(clientId))
    return null
  }
}

function writeTransaction(clientId: string, transaction: OAuthTransaction) {
  sessionStorage.setItem(getTransactionKey(clientId), JSON.stringify(transaction))
}

function clearTransaction(clientId: string) {
  sessionStorage.removeItem(getTransactionKey(clientId))
}

function resolveSameOriginReturnTo(returnTo: string | null | undefined, fallback: string) {
  if (!returnTo) {
    return fallback
  }

  try {
    const url = new URL(returnTo, window.location.origin)
    if (url.origin !== window.location.origin) {
      return fallback
    }

    return `${url.pathname}${url.search}${url.hash}`
  } catch {
    return fallback
  }
}

export async function startOAuthLogin(
  config: OAuthClientConfig,
  returnTo = `${window.location.pathname}${window.location.search}${window.location.hash}`,
) {
  const codeVerifier = createRandomString(64)
  const codeChallenge = await createCodeChallenge(codeVerifier)
  const state = createRandomString()

  writeTransaction(config.clientId, {
    codeVerifier,
    createdAt: new Date().toISOString(),
    returnTo: resolveSameOriginReturnTo(returnTo, config.postLoginPath),
    state,
  })

  const authorizeUrl = new URL('/authorize', config.authorizationServerUrl)
  authorizeUrl.searchParams.set('response_type', 'code')
  authorizeUrl.searchParams.set('client_id', config.clientId)
  authorizeUrl.searchParams.set('redirect_uri', config.redirectUri)
  authorizeUrl.searchParams.set('scope', config.scope)
  authorizeUrl.searchParams.set('state', state)
  authorizeUrl.searchParams.set('code_challenge', codeChallenge)
  authorizeUrl.searchParams.set('code_challenge_method', 'S256')

  window.location.assign(authorizeUrl.toString())
}

export async function completeOAuthLogin(config: OAuthClientConfig) {
  const currentUrl = new URL(window.location.href)
  const code = currentUrl.searchParams.get('code')
  const state = currentUrl.searchParams.get('state')
  const oauthError = currentUrl.searchParams.get('error')
  const oauthErrorDescription = currentUrl.searchParams.get('error_description')

  if (oauthError) {
    throw new Error(oauthErrorDescription ?? oauthError)
  }

  if (!code || !state) {
    throw new Error('Missing OAuth callback parameters.')
  }

  const transaction = readTransaction(config.clientId)
  if (!transaction || transaction.state !== state) {
    clearTransaction(config.clientId)
    throw new Error('Invalid or expired OAuth state.')
  }

  const body = new URLSearchParams({
    grant_type: 'authorization_code',
    client_id: config.clientId,
    code,
    redirect_uri: config.redirectUri,
    code_verifier: transaction.codeVerifier,
  })

  const response = await fetch(new URL('/token', config.authorizationServerUrl), {
    method: 'POST',
    headers: {
      'Content-Type': 'application/x-www-form-urlencoded',
    },
    body,
    credentials: 'omit',
  })

  if (!response.ok) {
    clearTransaction(config.clientId)
    throw new Error('OAuth token exchange failed.')
  }

  const payload = (await response.json()) as OAuthTokenResponse
  clearTransaction(config.clientId)

  return {
    accessToken: payload.access_token,
    returnTo: resolveSameOriginReturnTo(transaction.returnTo, config.postLoginPath),
  }
}
