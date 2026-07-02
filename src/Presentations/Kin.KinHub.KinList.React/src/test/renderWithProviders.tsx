import type { ReactElement } from 'react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { RouterProvider, createMemoryRouter } from 'react-router-dom'
import type { RouteObject } from 'react-router-dom'
import { render } from '@testing-library/react'

export function createTestQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: { retry: false, gcTime: 0 },
      mutations: { retry: false },
    },
  })
}

interface RenderRoutesOptions {
  routes: RouteObject[]
  initialEntries?: string[]
  queryClient?: QueryClient
}

/**
 * Renders a set of routes inside a data router (required for useBlocker) and a
 * React Query provider. Returns the router so tests can assert on navigation.
 */
export function renderWithProviders({
  routes,
  initialEntries = ['/'],
  queryClient = createTestQueryClient(),
}: RenderRoutesOptions) {
  const router = createMemoryRouter(routes, { initialEntries })
  const result = render(
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
    </QueryClientProvider>,
  )

  return { ...result, router, queryClient }
}

export function renderElement(element: ReactElement, options?: Partial<RenderRoutesOptions>) {
  return renderWithProviders({
    routes: [{ path: '/', element }],
    ...options,
  })
}
