import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { render, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { buildKinListDetailUrl, buildKinListRootUrl } from '@/config/appLinks'
import { ShoppingListDetailRedirect, ShoppingListsRedirect } from './ShoppingListRedirects'

describe('ShoppingListRedirects', () => {
  const originalLocation = window.location
  let assignSpy: ReturnType<typeof vi.fn>

  beforeEach(() => {
    assignSpy = vi.fn()
    Object.defineProperty(window, 'location', {
      configurable: true,
      value: {
        ...originalLocation,
        assign: assignSpy,
      } satisfies Partial<Location>,
    })
  })

  afterEach(() => {
    vi.clearAllMocks()
    Object.defineProperty(window, 'location', {
      configurable: true,
      value: originalLocation,
    })
  })

  it('maps the legacy shopping-list root to KinList root', async () => {
    render(
      <MemoryRouter initialEntries={['/shopping-lists']}>
        <Routes>
          <Route path="/shopping-lists" element={<ShoppingListsRedirect />} />
        </Routes>
      </MemoryRouter>,
    )

    await waitFor(() => expect(assignSpy).toHaveBeenCalledWith(buildKinListRootUrl()))
  })

  it('preserves the list id when redirecting legacy shopping-list detail URLs', async () => {
    render(
      <MemoryRouter initialEntries={['/shopping-lists/list-42']}>
        <Routes>
          <Route path="/shopping-lists/:id" element={<ShoppingListDetailRedirect />} />
        </Routes>
      </MemoryRouter>,
    )

    await waitFor(() => expect(assignSpy).toHaveBeenCalledWith(buildKinListDetailUrl('list-42')))
  })

  it('falls back to the KinList root when the legacy detail route lacks an id', async () => {
    render(
      <MemoryRouter initialEntries={['/shopping-lists']}>
        <Routes>
          <Route path="/shopping-lists" element={<ShoppingListDetailRedirect />} />
        </Routes>
      </MemoryRouter>,
    )

    await waitFor(() => expect(assignSpy).toHaveBeenCalledWith(buildKinListRootUrl()))
  })
})

describe('KinRecipe KinList app links', () => {
  it('targets the KinList list root', () => {
    expect(buildKinListRootUrl()).toBe('http://localhost:5175/lists')
  })

  it('targets the KinList detail route and preserves the list id', () => {
    expect(buildKinListDetailUrl('legacy-list-id')).toBe('http://localhost:5175/lists/legacy-list-id')
  })
})
