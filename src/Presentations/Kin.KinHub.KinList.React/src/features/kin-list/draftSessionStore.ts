import { randomUUID } from '@/lib/utils'

export interface KinListDraftItemState {
  id: string
  text: string
  isSelected: boolean
}

export interface KinListDraftSession {
  title: string
  items: KinListDraftItemState[]
  source: 'manual' | 'audio'
  idempotencyKey: string
  detectedLanguage?: string | null
  promptVersion?: string | null
}

let currentDraft: KinListDraftSession | null = null

export function createEmptyDraft(): KinListDraftSession {
  currentDraft = {
    title: '',
    items: [],
    source: 'manual',
    idempotencyKey: randomUUID(),
  }

  return currentDraft
}

export function saveDraftSession(draft: KinListDraftSession) {
  currentDraft = draft
}

export function readDraftSession() {
  return currentDraft
}

export function clearDraftSession() {
  currentDraft = null
}

export function createDraftFromAudio(input: {
  title: string
  items: string[]
  detectedLanguage?: string | null
  promptVersion?: string | null
}) {
  currentDraft = {
    title: input.title,
    items: input.items.map((text) => ({
      id: randomUUID(),
      text,
      isSelected: true,
    })),
    source: 'audio',
    idempotencyKey: randomUUID(),
    detectedLanguage: input.detectedLanguage,
    promptVersion: input.promptVersion,
  }

  return currentDraft
}
