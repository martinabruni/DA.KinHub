const attempts = Number.parseInt(process.env.SMOKE_MAX_ATTEMPTS ?? '8', 10);
const delayMs = Number.parseInt(process.env.SMOKE_RETRY_DELAY_MS ?? '10000', 10);
const timeoutMs = Number.parseInt(process.env.SMOKE_TIMEOUT_MS ?? '30000', 10);

const checks = [
  { name: 'Function health', url: process.env.FUNCTION_HEALTH_URL, token: process.env.FUNCTION_HEALTH_TOKEN },
  { name: 'Static Web App root', url: process.env.STATIC_ROOT_URL },
  { name: 'Static Web App version', url: process.env.STATIC_VERSION_URL },
];

if (![attempts, delayMs, timeoutMs].every(Number.isFinite) || attempts < 1 || delayMs < 0 || timeoutMs < 1) {
  throw new Error('Smoke test timing configuration is invalid.');
}

for (const check of checks) {
  if (!check.url) throw new Error(`${check.name} URL is missing.`);
  await verify(check);
}

async function verify(check) {
  let lastFailure = 'unknown error';

  for (let attempt = 1; attempt <= attempts; attempt += 1) {
    const controller = new AbortController();
    const timeout = setTimeout(() => controller.abort(), timeoutMs);

    try {
      const response = await fetch(check.url, {
        redirect: 'follow',
        signal: controller.signal,
        headers: check.token ? { authorization: `Bearer ${check.token}` } : undefined,
      });
      clearTimeout(timeout);

      if (response.status === 200) {
        console.log(`${check.name}: ok (status 200, attempt ${attempt})`);
        return;
      }

      lastFailure = `HTTP ${response.status}`;
      if (!isRetryableStatus(response.status)) break;
    } catch (error) {
      clearTimeout(timeout);
      lastFailure = error?.name === 'AbortError' ? 'timeout' : 'network error';
    }

    if (attempt < attempts) await new Promise((resolve) => setTimeout(resolve, delayMs));
  }

  throw new Error(`${check.name} failed: ${lastFailure} after ${attempts} attempt(s).`);
}

function isRetryableStatus(status) {
  return status === 408 || status === 429 || status >= 500;
}
