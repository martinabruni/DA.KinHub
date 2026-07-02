import { vi } from 'vitest'

/**
 * Lightweight, deterministic MediaRecorder + getUserMedia doubles for jsdom.
 *
 * jsdom implements neither `navigator.mediaDevices.getUserMedia` nor
 * `MediaRecorder`, so the AudioCaptureDialog degrades to the "unsupported"
 * branch by default. These helpers install controllable fakes so the recording
 * happy-path, permission handling and auto-stop timing can be exercised.
 */

export interface FakeMediaTrack {
  stop: ReturnType<typeof vi.fn>
  readyState: 'live' | 'ended'
}

export interface FakeMediaStream {
  getTracks: () => FakeMediaTrack[]
  tracks: FakeMediaTrack[]
}

export function createFakeStream(): FakeMediaStream {
  const track: FakeMediaTrack = {
    readyState: 'live',
    stop: vi.fn(function stop(this: FakeMediaTrack) {
      this.readyState = 'ended'
    }),
  }

  const tracks = [track]
  return {
    tracks,
    getTracks: () => tracks,
  }
}

export class FakeMediaRecorder {
  static isTypeSupported = vi.fn((type: string): boolean => {
    void type
    return true
  })

  static instances: FakeMediaRecorder[] = []

  state: 'inactive' | 'recording' | 'paused' = 'inactive'
  stream: FakeMediaStream
  mimeType: string
  ondataavailable: ((event: { data: Blob }) => void) | null = null
  onstop: (() => void) | null = null
  start = vi.fn(() => {
    this.state = 'recording'
  })

  constructor(stream: FakeMediaStream, options?: { mimeType?: string }) {
    this.stream = stream
    this.mimeType = options?.mimeType ?? 'audio/webm'
    FakeMediaRecorder.instances.push(this)
  }

  stop = vi.fn(() => {
    this.state = 'inactive'
    // Emit a chunk then signal completion, mirroring the real event ordering.
    this.ondataavailable?.({ data: new Blob(['fake-audio'], { type: this.mimeType }) })
    this.onstop?.()
  })

  /** Emit a chunk without stopping (used for partial-data assertions). */
  emitData(part = 'chunk') {
    this.ondataavailable?.({ data: new Blob([part], { type: this.mimeType }) })
  }
}

export interface InstallOptions {
  /** When set, getUserMedia rejects with this error (permission denied etc.). */
  getUserMediaError?: Error
  /** MIME types considered supported. Defaults to all supported. */
  supportedMimeTypes?: string[]
}

export interface InstalledMedia {
  getUserMedia: ReturnType<typeof vi.fn>
  stream: FakeMediaStream
  restore: () => void
}

/**
 * Installs navigator.mediaDevices.getUserMedia and a global MediaRecorder.
 * Returns a restore() that removes the doubles so other tests keep the jsdom
 * (unsupported) baseline.
 */
export function installMediaRecorder(options: InstallOptions = {}): InstalledMedia {
  const stream = createFakeStream()
  const getUserMedia = vi.fn(async () => {
    if (options.getUserMediaError) {
      throw options.getUserMediaError
    }
    return stream as unknown as MediaStream
  })

  const originalMediaDevices = navigator.mediaDevices
  Object.defineProperty(navigator, 'mediaDevices', {
    configurable: true,
    value: { getUserMedia },
  })

  FakeMediaRecorder.instances = []
  FakeMediaRecorder.isTypeSupported = vi.fn((type: string) => {
    if (!options.supportedMimeTypes) {
      return true
    }
    return options.supportedMimeTypes.includes(type)
  })

  const globalWithRecorder = globalThis as unknown as { MediaRecorder?: unknown }
  const originalRecorder = globalWithRecorder.MediaRecorder
  globalWithRecorder.MediaRecorder = FakeMediaRecorder as unknown

  return {
    getUserMedia,
    stream,
    restore: () => {
      if (originalMediaDevices === undefined) {
        // jsdom leaves mediaDevices undefined by default; remove our shim.
        Reflect.deleteProperty(navigator, 'mediaDevices')
      } else {
        Object.defineProperty(navigator, 'mediaDevices', {
          configurable: true,
          value: originalMediaDevices,
        })
      }

      if (originalRecorder === undefined) {
        Reflect.deleteProperty(globalWithRecorder, 'MediaRecorder')
      } else {
        globalWithRecorder.MediaRecorder = originalRecorder
      }
    },
  }
}
