import { createBrowserRouter, Navigate } from 'react-router-dom'
import { ProtectedRoute } from '@/components/ProtectedRoute'
import { Layout } from '@/components/Layout'
import { OAuthCallbackPage } from '@/features/auth/pages/OAuthCallbackPage'
import { KinListDetailPage } from '@/features/kin-list/pages/KinListDetailPage'
import { KinListsPage } from '@/features/kin-list/pages/KinListsPage'

export const router = createBrowserRouter([
  {
    path: '/oauth/callback',
    element: <OAuthCallbackPage />,
  },
  {
    element: <ProtectedRoute />,
    children: [
      {
        element: <Layout />,
        children: [
          {
            index: true,
            element: <KinListsPage />,
          },
          { path: '/draft/new', element: <KinListDetailPage /> },
          { path: '/lists/:id', element: <KinListDetailPage /> },
          { path: '/dashboard', element: <Navigate to="/" replace /> },
        ],
      },
    ],
  },
  { path: '*', element: <Navigate to="/" replace /> },
])


