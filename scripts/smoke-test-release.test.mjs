import assert from 'node:assert/strict';
import { createServer } from 'node:http';
import { once } from 'node:events';
import { spawn } from 'node:child_process';
import { test } from 'node:test';
import { fileURLToPath } from 'node:url';

const script = fileURLToPath(new URL('./smoke-test-release.mjs', import.meta.url));

test('accepts success responses and follows redirects', async () => {
  const server = createServer((request, response) => {
    if (request.url === '/health') assert.equal(request.headers.authorization, 'Bearer test-token');
    if (request.url === '/redirect') {
      response.writeHead(302, { location: '/root' }).end();
      return;
    }
    response.writeHead(200).end();
  });
  const port = await listen(server);

  await run({
    FUNCTION_HEALTH_URL: `http://127.0.0.1:${port}/health`,
    FUNCTION_HEALTH_TOKEN: 'test-token',
    STATIC_ROOT_URL: `http://127.0.0.1:${port}/redirect`,
    STATIC_VERSION_URL: `http://127.0.0.1:${port}/version`,
  });
  await close(server);
});

test('fails on an HTTP error', async () => {
  const server = createServer((_request, response) => response.writeHead(401).end());
  const port = await listen(server);

  await assert.rejects(run({
    FUNCTION_HEALTH_URL: `http://127.0.0.1:${port}/health`,
    STATIC_ROOT_URL: `http://127.0.0.1:${port}/root`,
    STATIC_VERSION_URL: `http://127.0.0.1:${port}/version`,
    SMOKE_MAX_ATTEMPTS: '1',
  }), /Function health failed: HTTP 401/);
  await close(server);
});

test('fails on timeout with bounded attempts', async () => {
  const server = createServer(() => {});
  const port = await listen(server);

  await assert.rejects(run({
    FUNCTION_HEALTH_URL: `http://127.0.0.1:${port}/health`,
    STATIC_ROOT_URL: `http://127.0.0.1:${port}/root`,
    STATIC_VERSION_URL: `http://127.0.0.1:${port}/version`,
    SMOKE_MAX_ATTEMPTS: '1',
    SMOKE_TIMEOUT_MS: '20',
  }), /Function health failed: timeout/);
  server.closeAllConnections();
  await close(server);
});

async function run(environment) {
  const child = spawn(process.execPath, [script], {
    env: { ...process.env, ...environment, SMOKE_RETRY_DELAY_MS: '1' },
    stdio: ['ignore', 'pipe', 'pipe'],
  });
  const stdout = collectImmediately(child.stdout);
  const stderr = collectImmediately(child.stderr);
  const [exitCode, signal] = await once(child, 'close');
  if (exitCode !== 0) {
    throw new Error(`exit=${exitCode}, signal=${signal}\n${await stdout}\n${await stderr}`.trim());
  }
}

async function listen(server) {
  server.listen(0, '127.0.0.1');
  await once(server, 'listening');
  return server.address().port;
}

async function close(server) {
  server.close();
  await once(server, 'close');
}

async function collect(stream) {
  const chunks = [];
  for await (const chunk of stream) chunks.push(chunk);
  return Buffer.concat(chunks).toString();
}

function collectImmediately(stream) {
  const chunks = [];
  stream.on('data', (chunk) => chunks.push(chunk));
  return once(stream, 'end').then(() => Buffer.concat(chunks).toString());
}
