import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Navigate, useLocation } from 'react-router-dom'
import type { KinListDetail, KinListItem } from '@/types'
import { renderWithProviders } from '@/test/renderWithProviders'
import {
  clearDraftSession,
  createDraftFromAudio,
  createEmptyDraft,
  readDraftSession,
} from '@/features/kin-list/draftSessionStore'
import { FakeMediaRecorder, installMediaRecorder } from '@/test/mediaRecorderMock'
import type { InstalledMedia } from '@/test/mediaRecorderMock'

const apiMocks = vi.hoisted(() => ({
  get: vi.fn(),
  post: vi.fn(),
  delete: vi.fn(),
  patch: vi.fn(),
}))

vi.mock('@/api/apiClient', () => ({
  apiClient: apiMocks,
}))

vi.mock('sonner', () => ({
  toast: {
    success: vi.fn(),
    error: vi.fn(),
  },
}))

import { KinListDetailPage } from './KinListDetailPage'
import { toast } from 'sonner'

function makeItem(overrides: Partial<KinListItem>): KinListItem {
  return {
    id: 'item-1',
    text: 'Milk',
    etag: 'item-etag-1',
    isCompleted: false,
    createdAt: '2026-06-30T10:00:00.000Z',
    updatedAt: '2026-06-30T10:00:00.000Z',
    ...overrides,
  }
}

function makeDetail(overrides: Partial<KinListDetail>): KinListDetail {
  return {
    id: 'list-1',
    title: 'Weekly groceries',
    etag: 'list-etag-1',
    totalItems: 0,
    completedItems: 0,
    isCompleted: false,
    lastModifiedAt: '2026-06-30T10:00:00.000Z',
    items: [],
    ...overrides,
  }
}

function LocationProbe() {
  const location = useLocation()
  return <div data-testid="location">{location.pathname}</div>
}

function renderDraft(initialEntries = ['/draft/new']) {
  return renderWithProviders({
    initialEntries,
    routes: [
      { path: '/', element: <div>Lists home<LocationProbe /></div> },
      { path: '/draft/new', element: <><KinListDetailPage /><LocationProbe /></> },
      { path: '/lists/:id', element: <><KinListDetailPage /><LocationProbe /></> },
      { path: '*', element: <Navigate to="/" replace /> },
    ],
  })
}

function renderDetail(id: string) {
  return renderWithProviders({
    initialEntries: [`/lists/${id}`],
    routes: [
      { path: '/', element: <div>Lists home<LocationProbe /></div> },
      { path: '/lists/:id', element: <><KinListDetailPage /><LocationProbe /></> },
    ],
  })
}

describe('KinListDetailPage', () => {
  beforeEach(() => {
    apiMocks.get.mockReset()
    apiMocks.post.mockReset()
    apiMocks.delete.mockReset()
    apiMocks.patch.mockReset()
    clearDraftSession()
  })

  afterEach(() => {
    clearDraftSession()
    vi.clearAllMocks()
  })

  describe('draft mode (no pre-save persistence)', () => {
    it('redirects home when there is no in-memory draft session', async () => {
      renderDraft()
      await waitFor(() => expect(screen.getByText(/lists home/i)).toBeInTheDocument())
    })

    it('renders the shared manual draft from the in-memory store', async () => {
      createEmptyDraft()
      renderDraft()

      expect(await screen.findByText(/draft before save/i)).toBeInTheDocument()
      // A manual draft persists nothing to web storage.
      expect(localStorage.length).toBe(0)
      expect(sessionStorage.length).toBe(0)
    })

    it('saves via POST /api/lists with the draft idempotency key and clears the draft', async () => {
      const user = userEvent.setup()
      createEmptyDraft()
      const draft = readDraftSession()!
      apiMocks.post.mockResolvedValue({ data: makeDetail({ id: 'created-1', title: 'Fruit run' }) })
      apiMocks.get.mockResolvedValue({ data: makeDetail({ id: 'created-1', title: 'Fruit run' }) })
      // Saving marks the draft dirty; the blocker asks to confirm the resulting nav.
      const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true)

      renderDraft()
      await screen.findByText(/draft before save/i)

      await user.type(screen.getByPlaceholderText(/list title/i), 'Fruit run')
      await user.click(screen.getByRole('button', { name: /add item/i }))
      const itemBox = await screen.findByPlaceholderText(/item 1/i)
      await user.type(itemBox, 'Apples')

      await user.click(screen.getByRole('button', { name: /save list/i }))

      await waitFor(() =>
        expect(apiMocks.post).toHaveBeenCalledWith(
          '/api/lists',
          { title: 'Fruit run', items: ['Apples'] },
          { headers: { 'Idempotency-Key': draft.idempotencyKey } },
        ),
      )
      // After a successful save the in-memory draft is cleared (no lingering state).
      await waitFor(() => expect(readDraftSession()).toBeNull())
      confirmSpy.mockRestore()
    })

    it('maps audio-seeded items into the same draft detail editor', async () => {
      createDraftFromAudio({ title: 'Voice list', items: ['Eggs', 'Butter'] })
      renderDraft()

      await screen.findByText(/draft before save/i)
      expect(screen.getByDisplayValue('Voice list')).toBeInTheDocument()
      expect(screen.getByDisplayValue('Eggs')).toBeInTheDocument()
      expect(screen.getByDisplayValue('Butter')).toBeInTheDocument()
    })
  })

  describe('dirty-navigation confirmation', () => {
    it('prompts before leaving a modified draft and stays when cancelled', async () => {
      const user = userEvent.setup()
      createEmptyDraft()
      const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(false)

      renderDraft()
      await screen.findByText(/draft before save/i)
      await user.type(screen.getByPlaceholderText(/list title/i), 'Dirty')

      await user.click(screen.getByRole('button', { name: /back to lists/i }))

      expect(confirmSpy).toHaveBeenCalled()
      // Navigation was cancelled: still on the draft screen with the draft intact.
      expect(screen.getByText(/draft before save/i)).toBeInTheDocument()
      expect(readDraftSession()).not.toBeNull()
      confirmSpy.mockRestore()
    })

    it('discards the draft and navigates away when confirmed', async () => {
      const user = userEvent.setup()
      createEmptyDraft()
      const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true)

      renderDraft()
      await screen.findByText(/draft before save/i)
      await user.type(screen.getByPlaceholderText(/list title/i), 'Dirty')

      await user.click(screen.getByRole('button', { name: /back to lists/i }))

      await waitFor(() => expect(screen.getByText(/lists home/i)).toBeInTheDocument())
      expect(readDraftSession()).toBeNull()
      confirmSpy.mockRestore()
    })
  })

  describe('persisted detail', () => {
    it('dims completed items and strikes their text (completed sink to the bottom of the server order)', async () => {
      const active = makeItem({ id: 'a', text: 'Bananas', isCompleted: false })
      const done = makeItem({ id: 'b', text: 'Bread', etag: 'e-b', isCompleted: true })
      apiMocks.get.mockResolvedValue({
        data: makeDetail({ items: [active, done], totalItems: 2, completedItems: 1 }),
      })

      renderDetail('list-1')
      await screen.findByText('Bananas')

      const doneText = screen.getByText('Bread')
      expect(doneText.className).toMatch(/line-through/)
      const doneCard = doneText.closest('.rounded-2xl') as HTMLElement
      expect(doneCard.className).toMatch(/muted/)

      // Active item is not struck through.
      expect(screen.getByText('Bananas').className).not.toMatch(/line-through/)
    })

    it('toggling an item completion PATCHes with the item ETag (deselecting reactivates it)', async () => {
      const done = makeItem({ id: 'b', text: 'Bread', etag: 'e-b', isCompleted: true })
      apiMocks.get.mockResolvedValue({
        data: makeDetail({ items: [done], totalItems: 1, completedItems: 1 }),
      })
      apiMocks.patch.mockResolvedValue({
        data: makeDetail({ items: [{ ...done, isCompleted: false }], totalItems: 1, completedItems: 0 }),
      })

      const user = userEvent.setup()
      renderDetail('list-1')
      await screen.findByText('Bread')

      const card = screen.getByText('Bread').closest('.rounded-2xl') as HTMLElement
      // The checkbox is the first button in the item card.
      const checkbox = within(card).getAllByRole('button')[0]
      await user.click(checkbox)

      await waitFor(() =>
        expect(apiMocks.patch).toHaveBeenCalledWith(
          '/api/lists/list-1/items/b',
          { text: 'Bread', isCompleted: false },
          { headers: { 'If-Match': 'e-b' } },
        ),
      )
    })

    it('deletes an item with a 5-second undo that restores via the server', async () => {
      const item = makeItem({ id: 'a', text: 'Bananas', etag: 'e-a' })
      apiMocks.get.mockResolvedValue({ data: makeDetail({ items: [item], totalItems: 1 }) })
      apiMocks.delete.mockResolvedValue({ data: makeDetail({ items: [], totalItems: 0 }) })
      apiMocks.post.mockResolvedValue({ data: makeDetail({ items: [item], totalItems: 1 }) })

      const user = userEvent.setup()
      renderDetail('list-1')
      await screen.findByText('Bananas')

      const card = screen.getByText('Bananas').closest('.rounded-2xl') as HTMLElement
      await user.click(within(card).getByRole('button', { name: /remove/i }))

      await waitFor(() =>
        expect(apiMocks.delete).toHaveBeenCalledWith('/api/lists/list-1/items/a', {
          headers: { 'If-Match': 'e-a' },
        }),
      )
      await waitFor(() => expect(toast.success).toHaveBeenCalled())
      const toastArgs = (toast.success as unknown as ReturnType<typeof vi.fn>).mock.calls[0]
      expect(toastArgs[1]).toMatchObject({ duration: 5000 })
      expect(toastArgs[1].action.label).toBe('Undo')

      await toastArgs[1].action.onClick()
      expect(apiMocks.post).toHaveBeenCalledWith('/api/lists/list-1/items/a/restore', null, {
        headers: { 'If-Match': 'e-a' },
      })
    })

    it('adds an item via POST with the list ETag', async () => {
      apiMocks.get.mockResolvedValue({ data: makeDetail({ items: [] }) })
      apiMocks.post.mockResolvedValue({ data: makeDetail({ items: [makeItem({ text: 'Coffee' })], totalItems: 1 }) })

      const user = userEvent.setup()
      renderDetail('list-1')
      await screen.findByRole('button', { name: /^add item$/i })

      await user.type(screen.getByPlaceholderText(/add an item/i), 'Coffee')
      await user.click(screen.getByRole('button', { name: /^add item$/i }))

      await waitFor(() =>
        expect(apiMocks.post).toHaveBeenCalledWith(
          '/api/lists/list-1/items',
          { text: 'Coffee' },
          { headers: { 'If-Match': 'list-etag-1' } },
        ),
      )
    })
  })

  describe('ETag conflict handling', () => {
    it('surfaces a warning toast and reloads on an etag_conflict error', async () => {
      const item = makeItem({ id: 'a', text: 'Bananas', etag: 'e-a', isCompleted: false })
      apiMocks.get.mockResolvedValue({ data: makeDetail({ items: [item], totalItems: 1 }) })
      apiMocks.patch.mockRejectedValue({ response: { data: { code: 'etag_conflict' } } })

      const user = userEvent.setup()
      renderDetail('list-1')
      await screen.findByText('Bananas')

      apiMocks.get.mockClear()

      const card = screen.getByText('Bananas').closest('.rounded-2xl') as HTMLElement
      await user.click(within(card).getAllByRole('button')[0])

      await waitFor(() =>
        expect(toast.error).toHaveBeenCalledWith(expect.stringMatching(/changed elsewhere/i)),
      )
      // The conflict handler invalidates the detail query, triggering a reload.
      await waitFor(() => expect(apiMocks.get).toHaveBeenCalled())
    })
  })

  describe('audio append (adds items via preview, in-memory retry)', () => {
    let media: InstalledMedia

    afterEach(() => {
      media?.restore()
    })

    it('records, proposes items via preview, and only persists after Confirm', async () => {
      media = installMediaRecorder()
      const user = userEvent.setup()

      apiMocks.get.mockResolvedValue({ data: makeDetail({ items: [] }) })
      // First POST: item-drafts/from-audio -> proposals. Second POST: items/confirm.
      apiMocks.post.mockImplementation((url: string) => {
        if (url.endsWith('/item-drafts/from-audio')) {
          return Promise.resolve({
            data: {
              items: [{ text: 'Olive oil', isSelectedByDefault: true, duplicateOfItemId: null }],
              existingDuplicates: [{ itemId: 'x', text: 'Salt', isCompleted: false }],
            },
          })
        }
        return Promise.resolve({ data: makeDetail({ items: [makeItem({ text: 'Olive oil' })], totalItems: 1 }) })
      })

      renderDetail('list-1')
      await user.click(await screen.findByRole('button', { name: /add from audio/i }))

      // Record and process the clip through the dialog.
      await user.click(await screen.findByRole('button', { name: /start recording/i }))
      await waitFor(() => expect(FakeMediaRecorder.instances.length).toBeGreaterThan(0))
      await user.click(screen.getByRole('button', { name: /^stop$/i }))
      await waitFor(() => expect(screen.getByRole('button', { name: /process audio/i })).toBeEnabled())
      await user.click(screen.getByRole('button', { name: /process audio/i }))

      // Proposals surface (with a duplicate warning) but nothing is confirmed yet.
      expect(await screen.findByText(/audio proposals/i)).toBeInTheDocument()
      expect(screen.getByText(/existing duplicates detected/i)).toBeInTheDocument()
      expect(screen.getByDisplayValue('Olive oil')).toBeInTheDocument()
      expect(apiMocks.post).not.toHaveBeenCalledWith(
        expect.stringContaining('/items/confirm'),
        expect.anything(),
        expect.anything(),
      )

      // Confirming posts the selected proposals to the confirm endpoint with the ETag.
      await user.click(screen.getByRole('button', { name: /confirm selected items/i }))
      await waitFor(() =>
        expect(apiMocks.post).toHaveBeenCalledWith(
          '/api/lists/list-1/items/confirm',
          { items: ['Olive oil'] },
          { headers: { 'If-Match': 'list-etag-1' } },
        ),
      )
    })
  })

  describe('negative assertions', () => {
    it('never persists a draft to IndexedDB / web storage before save', async () => {
      createEmptyDraft()
      renderDraft()
      await screen.findByText(/draft before save/i)

      expect(localStorage.length).toBe(0)
      expect(sessionStorage.length).toBe(0)
      // No IndexedDB databases are opened for draft persistence.
      const idb = (globalThis as { indexedDB?: { databases?: () => Promise<unknown[]> } }).indexedDB
      if (idb?.databases) {
        const dbs = await idb.databases()
        expect(dbs).toHaveLength(0)
      }
    })

    it('offers no alternative file-upload capture path on the detail screen', async () => {
      apiMocks.get.mockResolvedValue({ data: makeDetail({ items: [] }) })
      renderDetail('list-1')
      await screen.findByRole('button', { name: /add from audio/i })

      expect(document.querySelector('input[type="file"]')).toBeNull()
    })
  })
})
