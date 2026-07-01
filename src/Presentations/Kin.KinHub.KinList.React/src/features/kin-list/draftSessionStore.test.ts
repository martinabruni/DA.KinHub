import { afterEach, describe, expect, it } from 'vitest'
import {
  clearDraftSession,
  createDraftFromAudio,
  createEmptyDraft,
  readDraftSession,
  saveDraftSession,
} from '@/features/kin-list/draftSessionStore'

describe('draftSessionStore', () => {
  afterEach(() => {
    clearDraftSession()
  })

  it('creates a fresh manual draft with an idempotency key', () => {
    const draft = createEmptyDraft()

    expect(draft.source).toBe('manual')
    expect(draft.idempotencyKey).toBeTruthy()
    expect(readDraftSession()).toEqual(draft)
  })

  it('maps audio items into selected draft entries', () => {
    const draft = createDraftFromAudio({
      title: 'Spesa settimanale',
      items: ['Latte', 'Pane'],
      detectedLanguage: 'it-IT',
      promptVersion: 'kin-list-v1',
    })

    expect(draft.source).toBe('audio')
    expect(draft.items).toHaveLength(2)
    expect(draft.items.every((item) => item.isSelected)).toBe(true)
    expect(draft.detectedLanguage).toBe('it-IT')
    expect(draft.promptVersion).toBe('kin-list-v1')
  })

  it('replaces the current draft when saving an updated session', () => {
    const draft = createEmptyDraft()

    saveDraftSession({
      ...draft,
      title: 'Weekend',
      items: [{ id: 'item-1', text: 'Pomodori', isSelected: true }],
    })

    expect(readDraftSession()).toEqual({
      ...draft,
      title: 'Weekend',
      items: [{ id: 'item-1', text: 'Pomodori', isSelected: true }],
    })
  })
})
