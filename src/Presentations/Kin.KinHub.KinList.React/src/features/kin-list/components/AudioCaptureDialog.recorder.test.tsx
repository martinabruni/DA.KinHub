import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { act, fireEvent, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { AudioCaptureDialog } from './AudioCaptureDialog'
import { FakeMediaRecorder, installMediaRecorder } from '@/test/mediaRecorderMock'
import type { InstalledMedia } from '@/test/mediaRecorderMock'

function renderDialog(overrides: Partial<React.ComponentProps<typeof AudioCaptureDialog>> = {}) {
  const onConfirm = vi.fn().mockResolvedValue(undefined)
  const onOpenChange = vi.fn()
  render(
    <AudioCaptureDialog
      open
      onOpenChange={onOpenChange}
      title="Record a list"
      description="Speak your items"
      onConfirm={onConfirm}
      {...overrides}
    />,
  )
  return { onConfirm, onOpenChange }
}

describe('AudioCaptureDialog MediaRecorder integration', () => {
  let media: InstalledMedia

  afterEach(() => {
    media?.restore()
    vi.useRealTimers()
  })

  describe('capability degradation (no fakes installed)', () => {
    it('shows manual-fallback instructions and no upload path when the API is unsupported', () => {
      // jsdom provides neither MediaRecorder nor mediaDevices.getUserMedia.
      expect(typeof MediaRecorder).toBe('undefined')
      renderDialog()

      expect(screen.getByText(/recording is not supported/i)).toBeInTheDocument()
      expect(screen.getByText(/use manual creation instead/i)).toBeInTheDocument()
      // Negative assertion: no alternative file-upload affordance.
      expect(screen.queryByText(/upload/i)).not.toBeInTheDocument()
      expect(document.querySelector('input[type="file"]')).toBeNull()
    })
  })

  describe('permission handling', () => {
    it('shows an instructional error and keeps manual fallback available when permission is denied', async () => {
      const user = userEvent.setup()
      const denied = new DOMException('Permission denied', 'NotAllowedError')
      media = installMediaRecorder({ getUserMediaError: denied })

      renderDialog()
      await user.click(screen.getByRole('button', { name: /start recording/i }))

      await waitFor(() => {
        expect(screen.getByText(/denied or failed/i)).toBeInTheDocument()
      })
      expect(screen.getByText(/continue with manual creation/i)).toBeInTheDocument()
      // The process button remains disabled: no blob was captured.
      expect(screen.getByRole('button', { name: /process audio/i })).toBeDisabled()
      // Recording never started, so the recorder is never constructed.
      expect(FakeMediaRecorder.instances).toHaveLength(0)
    })
  })

  describe('recording lifecycle', () => {
    it('captures an in-memory blob and enables processing after stop', async () => {
      const user = userEvent.setup()
      media = installMediaRecorder()

      renderDialog()
      await user.click(screen.getByRole('button', { name: /start recording/i }))

      await waitFor(() => expect(media.getUserMedia).toHaveBeenCalledWith({ audio: true }))
      expect(FakeMediaRecorder.instances).toHaveLength(1)

      await user.click(screen.getByRole('button', { name: /^stop$/i }))

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /process audio/i })).toBeEnabled()
      })
      // Stopping releases the microphone stream.
      expect(media.stream.getTracks()[0].stop).toHaveBeenCalled()
    })

    it('passes the captured blob to onConfirm and closes the dialog', async () => {
      const user = userEvent.setup()
      media = installMediaRecorder()
      const { onConfirm, onOpenChange } = renderDialog()

      await user.click(screen.getByRole('button', { name: /start recording/i }))
      await waitFor(() => expect(FakeMediaRecorder.instances).toHaveLength(1))
      await user.click(screen.getByRole('button', { name: /^stop$/i }))
      await waitFor(() => expect(screen.getByRole('button', { name: /process audio/i })).toBeEnabled())

      await user.click(screen.getByRole('button', { name: /process audio/i }))

      await waitFor(() => expect(onConfirm).toHaveBeenCalledTimes(1))
      const blob = onConfirm.mock.calls[0][0] as Blob
      expect(blob).toBeInstanceOf(Blob)
      expect(blob.size).toBeGreaterThan(0)
      expect(onOpenChange).toHaveBeenCalledWith(false)
    })
  })

  describe('60-second auto-stop', () => {
    it('automatically stops recording at the 60 second cap', async () => {
      vi.useFakeTimers()
      media = installMediaRecorder()

      renderDialog()
      // fireEvent (not userEvent) to avoid the real-timer event loop under fake timers.
      fireEvent.click(screen.getByRole('button', { name: /start recording/i }))
      // Flush the getUserMedia promise so the recorder + timers are armed.
      await act(async () => {
        await Promise.resolve()
      })

      expect(FakeMediaRecorder.instances).toHaveLength(1)
      const recorder = FakeMediaRecorder.instances[0]
      expect(recorder.state).toBe('recording')

      // Advance the full cap; both the interval guard and the timeout fire.
      await act(async () => {
        await vi.advanceTimersByTimeAsync(60_000)
      })

      expect(recorder.stop).toHaveBeenCalled()
      expect(recorder.state).toBe('inactive')
    })
  })

  describe('supported MIME types across browsers', () => {
    it('prefers audio/webm;codecs=opus on Chrome desktop/Android', async () => {
      const user = userEvent.setup()
      media = installMediaRecorder({ supportedMimeTypes: ['audio/webm;codecs=opus', 'audio/ogg;codecs=opus'] })

      renderDialog()
      await user.click(screen.getByRole('button', { name: /start recording/i }))
      await waitFor(() => expect(FakeMediaRecorder.instances).toHaveLength(1))

      expect(FakeMediaRecorder.instances[0].mimeType).toBe('audio/webm;codecs=opus')
    })

    it('falls back to audio/mp4 on Safari iOS (no webm support)', async () => {
      const user = userEvent.setup()
      media = installMediaRecorder({ supportedMimeTypes: ['audio/mp4'] })

      renderDialog()
      await user.click(screen.getByRole('button', { name: /start recording/i }))
      await waitFor(() => expect(FakeMediaRecorder.instances).toHaveLength(1))

      expect(FakeMediaRecorder.instances[0].mimeType).toBe('audio/mp4')
    })

    it('constructs a recorder without an explicit mimeType when none are supported', async () => {
      const user = userEvent.setup()
      media = installMediaRecorder({ supportedMimeTypes: [] })

      renderDialog()
      await user.click(screen.getByRole('button', { name: /start recording/i }))
      await waitFor(() => expect(FakeMediaRecorder.instances).toHaveLength(1))

      // No supported candidate => default constructor => our fake's default type.
      expect(FakeMediaRecorder.instances[0].mimeType).toBe('audio/webm')
    })
  })

  describe('in-memory retry and release', () => {
    it('discards the captured blob on Retry and re-arms a fresh recording', async () => {
      const user = userEvent.setup()
      media = installMediaRecorder()

      renderDialog()
      await user.click(screen.getByRole('button', { name: /start recording/i }))
      await waitFor(() => expect(FakeMediaRecorder.instances).toHaveLength(1))
      await user.click(screen.getByRole('button', { name: /^stop$/i }))
      await waitFor(() => expect(screen.getByRole('button', { name: /process audio/i })).toBeEnabled())

      await user.click(screen.getByRole('button', { name: /retry/i }))

      // Retry clears the blob (process disabled again) and returns to idle.
      expect(screen.getByRole('button', { name: /process audio/i })).toBeDisabled()
      expect(screen.getByRole('button', { name: /start recording/i })).toBeInTheDocument()

      await user.click(screen.getByRole('button', { name: /start recording/i }))
      await waitFor(() => expect(FakeMediaRecorder.instances).toHaveLength(2))
    })

    it('releases the microphone stream and blob when the dialog closes', async () => {
      const user = userEvent.setup()
      media = installMediaRecorder()
      const onOpenChange = vi.fn()

      const { rerender } = render(
        <AudioCaptureDialog
          open
          onOpenChange={onOpenChange}
          title="Record a list"
          description="Speak your items"
          onConfirm={vi.fn().mockResolvedValue(undefined)}
        />,
      )

      await user.click(screen.getByRole('button', { name: /start recording/i }))
      await waitFor(() => expect(FakeMediaRecorder.instances).toHaveLength(1))
      await user.click(screen.getByRole('button', { name: /^stop$/i }))

      const track = media.stream.getTracks()[0]
      await waitFor(() => expect(track.stop).toHaveBeenCalled())

      // Closing the dialog runs the cleanup effect that releases resources.
      rerender(
        <AudioCaptureDialog
          open={false}
          onOpenChange={onOpenChange}
          title="Record a list"
          description="Speak your items"
          onConfirm={vi.fn().mockResolvedValue(undefined)}
        />,
      )

      // Reopening starts from a clean slate (no lingering blob).
      rerender(
        <AudioCaptureDialog
          open
          onOpenChange={onOpenChange}
          title="Record a list"
          description="Speak your items"
          onConfirm={vi.fn().mockResolvedValue(undefined)}
        />,
      )
      expect(screen.getByRole('button', { name: /process audio/i })).toBeDisabled()
    })
  })

  describe('negative assertions', () => {
    beforeEach(() => {
      media = installMediaRecorder()
    })

    it('never renders a file input as an alternative capture path', () => {
      renderDialog()
      expect(document.querySelector('input[type="file"]')).toBeNull()
      expect(screen.queryByText(/upload/i)).not.toBeInTheDocument()
    })
  })
})
