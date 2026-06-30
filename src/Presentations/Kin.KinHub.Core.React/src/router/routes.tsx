import { createBrowserRouter, Navigate } from 'react-router-dom'
import { ProtectedRoute } from '@/components/ProtectedRoute'
import { MemberRoute } from '@/components/MemberRoute'
import { Layout } from '@/components/Layout'
import { KinConsoleServiceLayout } from '@/components/KinConsoleServiceLayout'
import { OAuthCallbackPage } from '@/features/auth/pages/OAuthCallbackPage'
import { SelectMemberPage } from '@/features/family/pages/SelectMemberPage'
import { OnboardingPage } from '@/features/family/pages/OnboardingPage'
import { FamilyPage } from '@/features/family/pages/FamilyPage'
import { ServicesPage } from '@/features/family/pages/ServicesPage'
import { ServicesConsolePage } from '@/features/family/pages/ServicesConsolePage'
import { ProfilePage } from '@/features/profile/pages/ProfilePage'

export const router = createBrowserRouter([
  {
    path: '/oauth/callback',
    element: <OAuthCallbackPage />,
  },
  {
    element: <ProtectedRoute />,
    children: [
      {
        path: '/select-member',
        element: <SelectMemberPage />,
      },
      {
        path: '/onboarding',
        element: <OnboardingPage />,
      },
      {
        element: <MemberRoute />,
        children: [
          {
            element: <Layout />,
            children: [
              { index: true, element: <Navigate to="/services" replace /> },
              { path: '/dashboard', element: <Navigate to="/services" replace /> },
              { path: '/family', element: <FamilyPage /> },
              { path: '/services', element: <ServicesPage /> },
              { path: '/profile', element: <ProfilePage /> },
              {
                element: <KinConsoleServiceLayout />,
                children: [
                  { path: '/console', element: <Navigate to="/console/services" replace /> },
                  { path: '/console/services', element: <ServicesConsolePage /> },
                ],
              },
            ],
          },
        ],
      },
    ],
  },
  { path: '*', element: <Navigate to="/" replace /> },
])


