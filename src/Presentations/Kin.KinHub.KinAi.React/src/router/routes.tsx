import { createBrowserRouter, Navigate } from 'react-router-dom'
import { ProtectedRoute } from '@/components/ProtectedRoute'
import { MemberRoute } from '@/components/MemberRoute'
import { LoginPage } from '@/features/auth/LoginPage'
import { SelectMemberPage } from '@/features/auth/SelectMemberPage'
import { ConversationDetailPage } from '@/features/chat/pages/ConversationDetailPage'
import { ConversationListPage } from '@/features/chat/pages/ConversationListPage'

export const router = createBrowserRouter([
  { path: '/login', element: <LoginPage /> },
  {
    element: <ProtectedRoute />,
    children: [
      { path: '/select-member', element: <SelectMemberPage /> },
      {
        element: <MemberRoute />,
        children: [
          { index: true, element: <Navigate to="/conversations" replace /> },
          { path: '/conversations', element: <ConversationListPage /> },
          { path: '/conversations/:id', element: <ConversationDetailPage /> },
        ],
      },
    ],
  },
  { path: '*', element: <Navigate to="/" replace /> },
])
