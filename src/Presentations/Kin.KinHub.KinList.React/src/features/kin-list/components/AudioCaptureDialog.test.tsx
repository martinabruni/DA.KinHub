import { describe, expect, it, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import { AudioCaptureDialog } from './AudioCaptureDialog'

function renderDialog(open = true) {
  const onConfirm = vi.fn().mockResolvedValue(undefined)
  const onOpenChange = vi.fn()
  render(
    <AudioCaptureDialog
      open={open}
      onOpenChange={onOpenChange}
      title="Record a list"
      description="Speak your items"
      onConfirm={onConfirm}
    />,
  )
  return { onConfirm, onOpenChange }
}

describe('AudioCaptureDialog', () => {
  it('warns and points to manual creation when recording is unsupported', () => {
    // jsdom does not implement MediaRecorder, so the dialog must degrade gracefully.
    expect(typeof MediaRecorder).toBe('undefined')

    renderDialog()

    expect(
      screen.getByText(/recording is not supported/i),
    ).toBeInTheDocument()
    // No alternative file-upload affordance is offered.
    expect(screen.queryByText(/upload/i)).not.toBeInTheDocument()
  })

  it('keeps the process button disabled until a blob is captured', () => {
    renderDialog()

    const processButton = screen.getByRole('button', { name: /process audio/i })
    expect(processButton).toBeDisabled()
  })

  it('renders the 60 second auto-stop hint', () => {
    renderDialog()

    expect(screen.getByText(/automatically at 60 seconds/i)).toBeInTheDocument()
  })
})
