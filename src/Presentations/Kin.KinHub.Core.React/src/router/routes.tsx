import { createBrowserRouter, Navigate } from 'react-router-dom'
import { ProtectedRoute } from '@/components/ProtectedRoute'
import { MemberRoute } from '@/components/MemberRoute'
import { Layout } from '@/components/Layout'
import { KinRecipeServiceLayout } from '@/components/KinRecipeServiceLayout'
import { KinConsoleServiceLayout } from '@/components/KinConsoleServiceLayout'
import { ServiceGuard } from '@/components/ServiceGuard'
import { LoginPage } from '@/features/auth/pages/LoginPage'
import { RegisterPage } from '@/features/auth/pages/RegisterPage'
import { SelectMemberPage } from '@/features/family/pages/SelectMemberPage'
import { OnboardingPage } from '@/features/family/pages/OnboardingPage'
import { DashboardPage } from '@/features/dashboard/pages/DashboardPage'
import { FamilyPage } from '@/features/family/pages/FamilyPage'
import { ServicesPage } from '@/features/family/pages/ServicesPage'
import { ServicesConsolePage } from '@/features/family/pages/ServicesConsolePage'
import { RecipeBooksPage } from '@/features/recipes/pages/RecipeBooksPage'
import { RecipeBookDetailPage } from '@/features/recipes/pages/RecipeBookDetailPage'
import { RecipeDetailPage } from '@/features/recipes/pages/RecipeDetailPage'
import { FridgesPage } from '@/features/fridges/pages/FridgesPage'
import { FridgeDetailPage } from '@/features/fridges/pages/FridgeDetailPage'
import { ShoppingListsPage } from '@/features/shopping-lists/pages/ShoppingListsPage'
import { ShoppingListDetailPage } from '@/features/shopping-lists/pages/ShoppingListDetailPage'
import { AIAssistantPage } from '@/features/ai-assistant/pages/AIAssistantPage'
import { ProfilePage } from '@/features/profile/pages/ProfilePage'

export const router = createBrowserRouter([
  {
    path: '/login',
    element: <LoginPage />,
  },
  {
    path: '/register',
    element: <RegisterPage />,
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
              { index: true, element: <Navigate to="/dashboard" replace /> },
              { path: '/dashboard', element: <DashboardPage /> },
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
              {
                element: <KinRecipeServiceLayout />,
                children: [
                  {
                    element: <ServiceGuard serviceName="KinRecipe" />,
                    children: [
                      { path: '/recipe-books', element: <RecipeBooksPage /> },
                      { path: '/recipe-books/:id', element: <RecipeBookDetailPage /> },
                      { path: '/recipe-books/:id/recipes/:recipeId', element: <RecipeDetailPage /> },
                      { path: '/fridges', element: <FridgesPage /> },
                      { path: '/fridges/:id', element: <FridgeDetailPage /> },
                      { path: '/shopping-lists', element: <ShoppingListsPage /> },
                      { path: '/shopping-lists/:id', element: <ShoppingListDetailPage /> },
                      { path: '/ai-assistant', element: <AIAssistantPage /> },
                    ],
                  },
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


