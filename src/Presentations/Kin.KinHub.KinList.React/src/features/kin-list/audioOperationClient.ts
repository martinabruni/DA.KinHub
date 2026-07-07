import { apiClient } from '@/api/apiClient'
import type { AudioOperationResponse, AudioOperationType, CreateAudioOperationResponse } from '@/types'

const terminalStatuses = new Set(['Succeeded', 'Failed', 'Expired', 'Cancelled'])

function sleep(milliseconds: number, signal?: AbortSignal) {
  return new Promise<void>((resolve, reject) => {
    const abortSignal = signal

    if (abortSignal?.aborted) {
      reject(abortSignal.reason ?? new DOMException('The operation was aborted.', 'AbortError'))
      return
    }

    const timeoutId = window.setTimeout(() => {
      abortSignal?.removeEventListener('abort', handleAbort)
      resolve()
    }, milliseconds)

    const handleAbort = () => {
      window.clearTimeout(timeoutId)
      abortSignal?.removeEventListener('abort', handleAbort)
      reject(abortSignal?.reason ?? new DOMException('The operation was aborted.', 'AbortError'))
    }

    abortSignal?.addEventListener('abort', handleAbort, { once: true })
  })
}

export async function createAudioOperation(input: {
  type: AudioOperationType
  contentType: string
  declaredByteSize: number
  listId?: string
}, signal?: AbortSignal) {
  const { data, headers } = await apiClient.post<CreateAudioOperationResponse>('/api/audio-operations', input, { signal })
  const responseHeaders = headers ?? {}
  return {
    ...data,
    retryAfterSeconds: Number(responseHeaders['retry-after'] ?? data.retryAfterSeconds ?? 2),
  }
}

export async function uploadAudioToSas(uploadUrl: string, blob: Blob, signal?: AbortSignal) {
  const response = await fetch(uploadUrl, {
    method: 'PUT',
    signal,
    headers: {
      'x-ms-blob-type': 'BlockBlob',
      'Content-Type': blob.type || 'application/octet-stream',
    },
    body: blob,
  })

  if (!response.ok) {
    throw new Error(`Audio upload failed with status ${response.status}.`)
  }
}

export async function completeAudioOperation(operationId: string, signal?: AbortSignal) {
  const { data, headers } = await apiClient.post<AudioOperationResponse>(`/api/audio-operations/${operationId}/complete-upload`, null, { signal })
  const responseHeaders = headers ?? {}
  return {
    ...data,
    retryAfterSeconds: Number(responseHeaders['retry-after'] ?? data.retryAfterSeconds ?? 2),
  }
}

export async function getAudioOperation(operationId: string, signal?: AbortSignal) {
  const { data, headers } = await apiClient.get<AudioOperationResponse>(`/api/audio-operations/${operationId}`, { signal })
  const responseHeaders = headers ?? {}
  return {
    ...data,
    retryAfterSeconds: Number(responseHeaders['retry-after'] ?? data.retryAfterSeconds ?? 2),
  }
}

export async function deleteAudioOperation(operationId: string) {
  await apiClient.delete(`/api/audio-operations/${operationId}`)
}

export async function waitForAudioOperation(operationId: string, timeoutMs = 120000, signal?: AbortSignal) {
  const startedAt = Date.now()
  let latest = await getAudioOperation(operationId, signal)

  while (!terminalStatuses.has(latest.status)) {
    if (Date.now() - startedAt > timeoutMs) {
      throw new Error('Audio processing timed out.')
    }

    await sleep(Math.max(latest.retryAfterSeconds, 1) * 1000, signal)
    latest = await getAudioOperation(operationId, signal)
  }

  return latest
}

export function savePendingAudioOperation(key: string, operationId: string) {
  sessionStorage.setItem(key, operationId)
}

export function readPendingAudioOperation(key: string) {
  return sessionStorage.getItem(key)
}

export function clearPendingAudioOperation(key: string) {
  sessionStorage.removeItem(key)
}
