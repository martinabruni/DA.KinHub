import '@testing-library/jest-dom/vitest'
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { AudioCaptureDialog } from '@/features/kin-list/components/AudioCaptureDialog'

describe('AudioCaptureDialog', () => {
  const originalMediaRecorder = globalThis.MediaRecorder
  const originalMediaDevices = navigator.mediaDevices

  beforeEach(() => {
    vi.useRealTimers()
  })

  afterEach(() => {
    cleanup()
    vi.restoreAllMocks()
    globalThis.MediaRecorder = originalMediaRecorder
    Object.defineProperty(navigator, 'mediaDevices', {
      configurable: true,
      value: originalMediaDevices,
    })
  })

  it('shows the fallback guidance when media recording is unavailable', () => {
    globalThis.MediaRecorder = undefined as unknown as typeof MediaRecorder
    Object.defineProperty(navigator, 'mediaDevices', {
      configurable: true,
      value: undefined,
    })

    render(
      <AudioCaptureDialog
        open
        onOpenChange={vi.fn()}
        title="Nuova registrazione"
        description="Descrizione"
        onConfirm={vi.fn()}
      />,
    )

    expect(screen.getByText(/manual creation instead/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /process audio/i })).toBeDisabled()
  })

  it('records audio and submits the captured blob', async () => {
    const onConfirm = vi.fn().mockResolvedValue(undefined)
    const onOpenChange = vi.fn()
    const stream = {
      getTracks: () => [{ stop: vi.fn() }],
    } as unknown as MediaStream

    class FakeMediaRecorder {
      static isTypeSupported(type: string) {
        return type === 'audio/webm;codecs=opus'
      }

      public mimeType = 'audio/webm;codecs=opus'
      public state: 'inactive' | 'recording' = 'inactive'
      public ondataavailable: ((event: BlobEvent) => void) | null = null
      public onstop: (() => void) | null = null

      constructor() {}

      start() {
        this.state = 'recording'
      }

      stop() {
        this.state = 'inactive'
        this.ondataavailable?.({
          data: new Blob(['audio-bytes'], { type: this.mimeType }),
        } as BlobEvent)
        this.onstop?.()
      }
    }

    globalThis.MediaRecorder = FakeMediaRecorder as unknown as typeof MediaRecorder
    Object.defineProperty(navigator, 'mediaDevices', {
      configurable: true,
      value: {
        getUserMedia: vi.fn().mockResolvedValue(stream),
      },
    })

    render(
      <AudioCaptureDialog
        open
        onOpenChange={onOpenChange}
        title="Nuova registrazione"
        description="Descrizione"
        onConfirm={onConfirm}
      />,
    )

    fireEvent.click(screen.getByRole('button', { name: /start recording/i }))
    fireEvent.click(await screen.findByRole('button', { name: /^stop$/i }))
    fireEvent.click(await screen.findByRole('button', { name: /process audio/i }))

    await waitFor(() => {
      expect(onConfirm).toHaveBeenCalledTimes(1)
    })

    expect(onConfirm.mock.calls[0]?.[0]).toBeInstanceOf(Blob)
    expect(onOpenChange).toHaveBeenCalledWith(false)
  })
})
