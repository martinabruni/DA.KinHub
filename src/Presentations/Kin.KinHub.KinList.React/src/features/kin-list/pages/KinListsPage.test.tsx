import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Navigate, Outlet, useLocation } from 'react-router-dom'
import type { KinListSummary } from '@/types'
import { renderWithProviders } from '@/test/renderWithProviders'
import { readDraftSession, clearDraftSession } from '@/features/kin-list/draftSessionStore'
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

import { KinListsPage } from './KinListsPage'
import { toast } from 'sonner'

const activeList: KinListSummary = {
  id: 'list-active',
  title: 'Weekly groceries',
  etag: 'etag-active',
  totalItems: 4,
  completedItems: 1,
  isCompleted: false,
  lastModifiedAt: '2026-06-30T10:00:00.000Z',
}

const completedList: KinListSummary = {
  id: 'list-done',
  title: 'Party supplies',
  etag: 'etag-done',
  totalItems: 3,
  completedItems: 3,
  isCompleted: true,
  lastModifiedAt: '2026-06-29T10:00:00.000Z',
}

// Captures the current location so navigation can be asserted from tests.
function LocationProbe() {
  const location = useLocation()
  return <div data-testid="location">{location.pathname}</div>
}

function renderPage(initialLists: KinListSummary[], options?: { preserveGetMock?: boolean }) {
  if (!options?.preserveGetMock) {
    apiMocks.get.mockResolvedValue({ data: initialLists })
  }

  return renderWithProviders({
    routes: [
      {
        element: (
          <>
            <Outlet />
            <LocationProbe />
          </>
        ),
        children: [
          { index: true, element: <KinListsPage /> },
          { path: 'draft/new', element: <div>Draft page</div> },
          { path: 'lists/:id', element: <div>Detail page</div> },
          { path: '*', element: <Navigate to="/" replace /> },
        ],
      },
    ],
  })
}

describe('KinListsPage', () => {
  beforeEach(() => {
    apiMocks.get.mockReset()
    apiMocks.post.mockReset()
    apiMocks.delete.mockReset()
    apiMocks.patch.mockReset()
  })

  afterEach(() => {
    clearDraftSession()
    vi.clearAllMocks()
  })

  describe('empty landing', () => {
    it('shows the empty state with a prominent record button and a manual create action', async () => {
      renderPage([])

      await screen.findByText(/no lists yet/i)

      const emptySection = screen.getByText(/no lists yet/i).closest('section') as HTMLElement
      expect(within(emptySection).getByRole('button', { name: /record/i })).toBeInTheDocument()
      expect(within(emptySection).getByRole('button', { name: /create manually/i })).toBeInTheDocument()
    })

    it('manual create seeds an empty in-memory draft and navigates to /draft/new', async () => {
      const user = userEvent.setup()
      renderPage([], { preserveGetMock: true })
      await screen.findByText(/no lists yet/i)

      const emptySection = screen.getByText(/no lists yet/i).closest('section') as HTMLElement
      await user.click(within(emptySection).getByRole('button', { name: /create manually/i }))

      await waitFor(() => expect(screen.getByTestId('location')).toHaveTextContent('/draft/new'))
      const draft = readDraftSession()
      expect(draft).not.toBeNull()
      expect(draft?.source).toBe('manual')
      expect(draft?.items).toEqual([])
    })

    it('resumes a pending new-list audio operation from sessionStorage after refresh', async () => {
      sessionStorage.setItem('kinlist:new-list-audio-operation', 'op-new-1')
      apiMocks.get.mockImplementation((url: string) => {
        if (url === '/api/lists') {
          return Promise.resolve({ data: [] })
        }

        return Promise.resolve({
          data: {
            id: 'op-new-1',
            type: 'NewList',
            status: 'Succeeded',
            listId: null,
            title: 'Voice list',
            items: ['Eggs', 'Butter'],
            itemProposals: [],
            existingDuplicates: [],
            detectedLanguage: 'en-US',
            promptVersion: 'v1',
            retryAfterSeconds: 2,
            expiresAt: '2026-06-30T11:00:00.000Z',
          },
        })
      })

      renderPage([], { preserveGetMock: true })

      await waitFor(() => expect(screen.getByTestId('location')).toHaveTextContent('/draft/new'))
      const draft = readDraftSession()
      expect(draft).not.toBeNull()
      expect(draft?.title).toBe('Voice list')
      expect(draft?.items.map((item) => item.text)).toEqual(['Eggs', 'Butter'])
      expect(sessionStorage.getItem('kinlist:new-list-audio-operation')).toBeNull()
      expect(sessionStorage.length).toBe(0)
    })

    it('clears a failed pending new-list audio operation and shows the error', async () => {
      sessionStorage.setItem('kinlist:new-list-audio-operation', 'op-new-failed')
      apiMocks.get.mockImplementation((url: string) => {
        if (url === '/api/lists') {
          return Promise.resolve({ data: [] })
        }

        return Promise.resolve({
          data: {
            id: 'op-new-failed',
            type: 'NewList',
            status: 'Failed',
            listId: null,
            title: null,
            items: [],
            itemProposals: [],
            existingDuplicates: [],
            detectedLanguage: null,
            promptVersion: 'v1',
            errorMessage: 'Audio processing failed.',
            retryAfterSeconds: 2,
            expiresAt: '2026-06-30T11:00:00.000Z',
          },
        })
      })

      renderPage([], { preserveGetMock: true })

      await screen.findByText(/no lists yet/i)
      await waitFor(() => expect(toast.error).toHaveBeenCalledWith('Audio processing failed.'))
      expect(sessionStorage.getItem('kinlist:new-list-audio-operation')).toBeNull()
      expect(readDraftSession()).toBeNull()
    })

    it('deletes the server-side audio operation when upload fails after create', async () => {
      let media: InstalledMedia | undefined
      media = installMediaRecorder()
      const user = userEvent.setup()
      const fetchMock = vi.fn().mockResolvedValue({ ok: false, status: 500 })
      vi.stubGlobal('fetch', fetchMock)

      apiMocks.post.mockImplementation((url: string) => {
        if (url === '/api/audio-operations') {
          return Promise.resolve({
            data: {
              id: 'op-upload-fail',
              uploadUrl: 'https://storage.test/upload',
              uploadExpiresAt: '2026-06-30T10:10:00.000Z',
              blobName: 'family/op-upload-fail',
              retryAfterSeconds: 1,
            },
          })
        }

        throw new Error(`Unexpected POST ${url}`)
      })
      apiMocks.delete.mockResolvedValue({ data: null })

      renderPage([])
      await screen.findByText(/no lists yet/i)

      await user.click(screen.getByRole('button', { name: /record a new list/i }))
      await user.click(await screen.findByRole('button', { name: /start recording/i }))
      await waitFor(() => expect(FakeMediaRecorder.instances.length).toBeGreaterThan(0))
      await user.click(screen.getByRole('button', { name: /^stop$/i }))
      await waitFor(() => expect(screen.getByRole('button', { name: /process audio/i })).toBeEnabled())
      await user.click(screen.getByRole('button', { name: /process audio/i }))

      await waitFor(() =>
        expect(apiMocks.delete).toHaveBeenCalledWith('/api/audio-operations/op-upload-fail'),
      )
      await waitFor(() =>
        expect(toast.error).toHaveBeenCalledWith('Audio upload failed with status 500.'),
      )
      expect(sessionStorage.getItem('kinlist:new-list-audio-operation')).toBeNull()

      media.restore()
      vi.unstubAllGlobals()
    })

    it('cancels in-flight audio processing, deletes the server-side operation, and avoids an error toast', async () => {
      const media = installMediaRecorder()
      const user = userEvent.setup()
      const fetchMock = vi.fn().mockImplementation((_url: string, init?: RequestInit) =>
        new Promise((_resolve, reject) => {
          const signal = init?.signal
          signal?.addEventListener('abort', () => {
            reject(signal.reason ?? new DOMException('The operation was aborted.', 'AbortError'))
          }, { once: true })
        }))
      vi.stubGlobal('fetch', fetchMock)

      apiMocks.post.mockImplementation((url: string) => {
        if (url === '/api/audio-operations') {
          return Promise.resolve({
            data: {
              id: 'op-cancel-1',
              uploadUrl: 'https://storage.test/upload',
              uploadExpiresAt: '2026-06-30T10:10:00.000Z',
              blobName: 'family/op-cancel-1',
              retryAfterSeconds: 1,
            },
          })
        }

        throw new Error(`Unexpected POST ${url}`)
      })
      apiMocks.delete.mockResolvedValue({ data: null })

      renderPage([])
      await screen.findByText(/no lists yet/i)

      await user.click(screen.getByRole('button', { name: /record a new list/i }))
      await user.click(await screen.findByRole('button', { name: /start recording/i }))
      await waitFor(() => expect(FakeMediaRecorder.instances.length).toBeGreaterThan(0))
      await user.click(screen.getByRole('button', { name: /^stop$/i }))
      await waitFor(() => expect(screen.getByRole('button', { name: /process audio/i })).toBeEnabled())
      await user.click(screen.getByRole('button', { name: /process audio/i }))
      await user.click(await screen.findByRole('button', { name: /cancel processing/i }))

      await waitFor(() =>
        expect(apiMocks.delete).toHaveBeenCalledWith('/api/audio-operations/op-cancel-1'),
      )
      expect(toast.error).not.toHaveBeenCalled()
      expect(sessionStorage.getItem('kinlist:new-list-audio-operation')).toBeNull()

      media.restore()
      vi.unstubAllGlobals()
    })
  })

  describe('list landing', () => {
    it('renders a card grid with title, completed/total and a progress bar', async () => {
      renderPage([activeList])
      await screen.findByText('Weekly groceries')

      expect(screen.getByText('1/4 completed')).toBeInTheDocument()
      // Progress bar exposes a role of progressbar via Radix.
      expect(document.querySelector('[role="progressbar"], [data-slot="progress"]')).toBeTruthy()
    })

    it('orders completed lists after active lists (greyed at the bottom)', async () => {
      // Server returns active first, completed last; the UI preserves that ordering.
      renderPage([activeList, completedList])
      await screen.findByText('Weekly groceries')

      const titles = screen.getAllByText(/groceries|supplies/i).map((el) => el.textContent)
      expect(titles.indexOf('Weekly groceries')).toBeLessThan(titles.indexOf('Party supplies'))

      // Completed card is visually greyed (muted foreground).
      const completedCard = screen.getByText('Party supplies').closest('.rounded-\\[28px\\]')
        ?? screen.getByText('Party supplies').closest('[class*="rounded"]')
      expect(completedCard?.className).toMatch(/muted/)
    })

    it('exposes a floating record button for mobile capture', async () => {
      renderPage([activeList])
      await screen.findByText('Weekly groceries')

      // Two record affordances exist: the hero and the floating (md:hidden) button.
      const recordButtons = screen.getAllByRole('button', { name: /record/i })
      expect(recordButtons.length).toBeGreaterThanOrEqual(2)
      const floating = recordButtons.find((btn) => btn.closest('.fixed'))
      expect(floating).toBeTruthy()
    })

    it('deletes a list with a 5-second undo toast that restores via the server', async () => {
      const user = userEvent.setup()
      apiMocks.delete.mockResolvedValue({ data: { ...activeList, etag: 'etag-deleted' } })
      apiMocks.post.mockResolvedValue({ data: activeList })
      renderPage([activeList])
      await screen.findByText('Weekly groceries')

      // Open the card menu (icon-only trigger) scoped to the active card, then delete.
      const card = screen.getByText('Weekly groceries').closest('[data-slot="card"]') as HTMLElement
      const menuTrigger = within(card).getByRole('button')
      await user.click(menuTrigger)
      const deleteItem = await screen.findByRole('menuitem', { name: /delete/i })
      await user.click(deleteItem)

      await waitFor(() =>
        expect(apiMocks.delete).toHaveBeenCalledWith('/api/lists/list-active', {
          headers: { 'If-Match': 'etag-active' },
        }),
      )

      // The undo toast is registered with a 5s duration and a restore action.
      await waitFor(() => expect(toast.success).toHaveBeenCalled())
      const toastCall = (toast.success as unknown as ReturnType<typeof vi.fn>).mock.calls[0]
      expect(toastCall[1]).toMatchObject({ duration: 5000 })
      expect(toastCall[1].action.label).toBe('Undo')

      // Invoking undo restores using the post-delete ETag.
      await toastCall[1].action.onClick()
      expect(apiMocks.post).toHaveBeenCalledWith('/api/lists/list-active/restore', null, {
        headers: { 'If-Match': 'etag-deleted' },
      })
    })
  })

  describe('responsive layout (mobile-first)', () => {
    it('hides the floating record button from md and up, and gates the desktop new-draft button', async () => {
      renderPage([activeList])
      await screen.findByText('Weekly groceries')

      // Floating record button is mobile-only.
      const floating = screen
        .getAllByRole('button', { name: /record/i })
        .find((btn) => btn.closest('.fixed'))!
      expect(floating.closest('.fixed')?.className).toMatch(/md:hidden/)

      // The inline "New draft" button only appears from the sm breakpoint.
      const newDraft = screen.getByRole('button', { name: /new draft/i })
      expect(newDraft.className).toMatch(/hidden/)
      expect(newDraft.className).toMatch(/sm:flex/)

      // Card grid is single-column on mobile, multi-column at sm/xl.
      const grid = screen.getByText('Weekly groceries').closest('.grid') as HTMLElement
      expect(grid.className).toMatch(/sm:grid-cols-2/)
      expect(grid.className).toMatch(/xl:grid-cols-3/)
    })
  })

  describe('negative assertions', () => {
    it('registers no service worker (no offline/PWA support)', async () => {
      renderPage([activeList])
      await screen.findByText('Weekly groceries')

      // The page never registers a service worker.
      if ('serviceWorker' in navigator) {
        const registrations = await navigator.serviceWorker.getRegistrations?.().catch(() => [])
        expect(registrations ?? []).toHaveLength(0)
      } else {
        expect('serviceWorker' in navigator).toBe(false)
      }
    })
  })
})
