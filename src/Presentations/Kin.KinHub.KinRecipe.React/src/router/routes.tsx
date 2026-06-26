import { createBrowserRouter, Navigate } from 'react-router-dom'
import { ProtectedRoute } from '@/components/ProtectedRoute'
import { MemberRoute } from '@/components/MemberRoute'
import { Layout } from '@/components/Layout'
import { KinRecipeServiceLayout } from '@/components/KinRecipeServiceLayout'
import { ServiceGuard } from '@/components/ServiceGuard'
import { DashboardPage } from '@/features/dashboard/pages/DashboardPage'
import { RecipeBooksPage } from '@/features/recipes/pages/RecipeBooksPage'
import { RecipeBookDetailPage } from '@/features/recipes/pages/RecipeBookDetailPage'
import { RecipeDetailPage } from '@/features/recipes/pages/RecipeDetailPage'
import { FridgesPage } from '@/features/fridges/pages/FridgesPage'
import { FridgeDetailPage } from '@/features/fridges/pages/FridgeDetailPage'
import { ShoppingListsPage } from '@/features/shopping-lists/pages/ShoppingListsPage'
import { ShoppingListDetailPage } from '@/features/shopping-lists/pages/ShoppingListDetailPage'
import { AIAssistantPage } from '@/features/ai-assistant/pages/AIAssistantPage'

export const router = createBrowserRouter([
  {
    element: <ProtectedRoute />,
    children: [
      {
        element: <MemberRoute />,
        children: [
          {
            element: <Layout />,
            children: [
              {
                element: <ServiceGuard serviceName="KinRecipe" />,
                children: [
                  { index: true, element: <DashboardPage /> },
                  { path: '/dashboard', element: <Navigate to="/" replace /> },
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


