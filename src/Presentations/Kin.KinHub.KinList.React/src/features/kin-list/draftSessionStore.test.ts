import { afterEach, describe, expect, it } from 'vitest'
import {
  clearDraftSession,
  createDraftFromAudio,
  createEmptyDraft,
  readDraftSession,
  saveDraftSession,
} from './draftSessionStore'

afterEach(() => {
  clearDraftSession()
  localStorage.clear()
  sessionStorage.clear()
})

describe('draftSessionStore', () => {
  it('creates an empty manual draft with a fresh idempotency key', () => {
    const draft = createEmptyDraft()

    expect(draft.source).toBe('manual')
    expect(draft.title).toBe('')
    expect(draft.items).toEqual([])
    expect(draft.idempotencyKey).toMatch(/[0-9a-f-]{36}/i)
    expect(readDraftSession()).toBe(draft)
  })

  it('maps audio items to selected editable items by default', () => {
    const draft = createDraftFromAudio({
      title: 'Groceries',
      items: ['2 packs of milk', 'bread'],
      detectedLanguage: 'en',
      promptVersion: 'v1',
    })

    expect(draft.source).toBe('audio')
    expect(draft.detectedLanguage).toBe('en')
    expect(draft.promptVersion).toBe('v1')
    expect(draft.items).toHaveLength(2)
    expect(draft.items.every((item) => item.isSelected)).toBe(true)
    expect(draft.items[0].text).toBe('2 packs of milk')
    // Each generated item id is unique.
    expect(new Set(draft.items.map((item) => item.id)).size).toBe(2)
  })

  it('gives every draft a distinct idempotency key', () => {
    const first = createEmptyDraft().idempotencyKey
    const second = createDraftFromAudio({ title: 't', items: ['x'] }).idempotencyKey

    expect(first).not.toBe(second)
  })

  it('does not persist the draft to web storage before save', () => {
    createDraftFromAudio({ title: 'Secret list', items: ['private item'] })
    saveDraftSession({
      title: 'Secret list',
      items: [{ id: '1', text: 'private item', isSelected: true }],
      source: 'audio',
      idempotencyKey: 'key',
    })

    expect(localStorage.length).toBe(0)
    expect(sessionStorage.length).toBe(0)
  })

  it('clears the in-memory draft', () => {
    createEmptyDraft()
    clearDraftSession()

    expect(readDraftSession()).toBeNull()
  })
})
