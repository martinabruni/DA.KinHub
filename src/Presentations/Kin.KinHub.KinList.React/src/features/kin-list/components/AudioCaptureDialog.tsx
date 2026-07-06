import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { Loader2, Mic, RotateCcw, Square, TriangleAlert } from 'lucide-react'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'

const MAX_DURATION_SECONDS = 60

function pickSupportedMimeType() {
  if (typeof MediaRecorder === 'undefined') {
    return ''
  }

  const candidates = [
    'audio/webm;codecs=opus',
    'audio/mp4',
    'audio/ogg;codecs=opus',
  ]

  return candidates.find((candidate) => MediaRecorder.isTypeSupported(candidate)) ?? ''
}

interface AudioCaptureDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  title: string
  description: string
  onConfirm: (blob: Blob, signal: AbortSignal) => Promise<void>
}

export function AudioCaptureDialog({
  open,
  onOpenChange,
  title,
  description,
  onConfirm,
}: AudioCaptureDialogProps) {
  const recorderRef = useRef<MediaRecorder | null>(null)
  const streamRef = useRef<MediaStream | null>(null)
  const chunksRef = useRef<BlobPart[]>([])
  const timeoutRef = useRef<number | null>(null)
  const intervalRef = useRef<number | null>(null)
  const abortControllerRef = useRef<AbortController | null>(null)

  const [blob, setBlob] = useState<Blob | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [isRecording, setIsRecording] = useState(false)
  const [elapsedSeconds, setElapsedSeconds] = useState(0)
  const [isSubmitting, setIsSubmitting] = useState(false)

  const supportsRecording = typeof navigator !== 'undefined'
    && !!navigator.mediaDevices?.getUserMedia
    && typeof MediaRecorder !== 'undefined'

  const mimeType = useMemo(() => pickSupportedMimeType(), [])

  // Releases the MediaRecorder, microphone stream and timers (external resources only).
  const cleanupMedia = useCallback(() => {
    if (timeoutRef.current) {
      window.clearTimeout(timeoutRef.current)
      timeoutRef.current = null
    }

    if (intervalRef.current) {
      window.clearInterval(intervalRef.current)
      intervalRef.current = null
    }

    recorderRef.current = null
    streamRef.current?.getTracks().forEach((track) => track.stop())
    streamRef.current = null
    chunksRef.current = []
    setIsRecording(false)
  }, [])

  const resetCapture = useCallback(() => {
    abortControllerRef.current = null
    cleanupMedia()
    setBlob(null)
    setError(null)
    setElapsedSeconds(0)
    setIsSubmitting(false)
  }, [cleanupMedia])

  // When the dialog closes (and on unmount) release the microphone and discard the
  // in-memory blob so nothing survives between sessions. The teardown runs in the
  // effect cleanup so it fires exactly on the open -> closed transition.
  useEffect(() => {
    if (!open) {
      return
    }

    return () => resetCapture()
  }, [open, resetCapture])

  const stopRecording = () => {
    if (recorderRef.current?.state === 'recording') {
      recorderRef.current.stop()
    } else {
      cleanupMedia()
    }
  }

  const startRecording = async () => {
    if (!supportsRecording) {
      setError('Audio recording is not supported on this browser. Use manual creation instead.')
      return
    }

    try {
      setError(null)
      setBlob(null)
      setElapsedSeconds(0)
      chunksRef.current = []

      const stream = await navigator.mediaDevices.getUserMedia({ audio: true })
      streamRef.current = stream

      const recorder = mimeType ? new MediaRecorder(stream, { mimeType }) : new MediaRecorder(stream)
      recorderRef.current = recorder
      recorder.ondataavailable = (event) => {
        if (event.data.size > 0) {
          chunksRef.current.push(event.data)
        }
      }

      recorder.onstop = () => {
        const nextBlob = new Blob(chunksRef.current, {
          type: recorder.mimeType || mimeType || 'audio/webm',
        })

        cleanupMedia()
        setBlob(nextBlob.size > 0 ? nextBlob : null)
      }

      recorder.start()
      setIsRecording(true)
      intervalRef.current = window.setInterval(() => {
        setElapsedSeconds((current) => {
          if (current >= MAX_DURATION_SECONDS) {
            stopRecording()
            return MAX_DURATION_SECONDS
          }

          return current + 1
        })
      }, 1000)

      timeoutRef.current = window.setTimeout(() => {
        stopRecording()
      }, MAX_DURATION_SECONDS * 1000)
    } catch {
      cleanupMedia()
      setError('Microphone access was denied or failed. You can continue with manual creation.')
    }
  }

  const confirmAudio = async () => {
    if (!blob) {
      return
    }

    const controller = new AbortController()
    abortControllerRef.current = controller
    try {
      setIsSubmitting(true)
      await onConfirm(blob, controller.signal)
      onOpenChange(false)
    } catch {
      // The caller surfaces the error state; keep the dialog open without bubbling
      // an unhandled rejection from the click handler.
    } finally {
      abortControllerRef.current = null
      setIsSubmitting(false)
    }
  }

  const cancelDialog = () => {
    if (isSubmitting) {
      abortControllerRef.current?.abort(new DOMException('The operation was aborted.', 'AbortError'))
    }

    onOpenChange(false)
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
          <DialogDescription>{description}</DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          {!supportsRecording ? (
            <div className="rounded-2xl border border-amber-300/50 bg-amber-50 p-4 text-sm text-amber-900">
              <div className="flex items-start gap-3">
                <TriangleAlert className="mt-0.5 h-4 w-4 shrink-0" />
                <p>Audio recording is not supported here. Use manual creation instead.</p>
              </div>
            </div>
          ) : null}

          {error ? (
            <div className="rounded-2xl border border-destructive/20 bg-destructive/5 p-4 text-sm text-destructive">
              {error}
            </div>
          ) : null}

          <div className="rounded-3xl border bg-card p-6 text-center">
            <p className="text-sm uppercase tracking-[0.22em] text-muted-foreground">Recorder</p>
            <p className="mt-3 text-4xl font-semibold tabular-nums">
              {new Date(elapsedSeconds * 1000).toISOString().slice(14, 19)}
            </p>
            <p className="mt-2 text-sm text-muted-foreground">
              Stop automatically at {MAX_DURATION_SECONDS} seconds.
            </p>

            <div className="mt-5 flex flex-wrap items-center justify-center gap-3">
              {!isRecording ? (
                <Button type="button" onClick={startRecording} className="rounded-full px-5">
                  <Mic className="mr-2 h-4 w-4" />
                  Start recording
                </Button>
              ) : (
                <Button type="button" variant="destructive" onClick={stopRecording} className="rounded-full px-5">
                  <Square className="mr-2 h-4 w-4" />
                  Stop
                </Button>
              )}

              {blob ? (
                <Button type="button" variant="outline" onClick={resetCapture} className="rounded-full px-5">
                  <RotateCcw className="mr-2 h-4 w-4" />
                  Retry
                </Button>
              ) : null}
            </div>
          </div>
        </div>

        <DialogFooter>
          <Button type="button" variant="outline" onClick={cancelDialog}>
            {isSubmitting ? 'Cancel processing' : 'Cancel'}
          </Button>
          <Button type="button" onClick={confirmAudio} disabled={!blob || isSubmitting}>
            {isSubmitting ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : <Mic className="mr-2 h-4 w-4" />}
            Process audio
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
