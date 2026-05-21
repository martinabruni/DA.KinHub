import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { ThemeProvider } from 'next-themes'
import { RouterProvider } from 'react-router-dom'
import { Toaster } from 'sonner'
import { AuthProvider } from '@/features/auth/AuthProvider'
import { router } from '@/router/routes'
import { AuthContextProvider } from '@/store/authContext'
import './i18n'
import './index.css'

const queryClient = new QueryClient({
  defaultOptions: { queries: { staleTime: 1000 * 60, retry: 1 } },
})

export default function App() {
  return (
    <ThemeProvider attribute="class" defaultTheme="system" enableSystem>
      <QueryClientProvider client={queryClient}>
        <AuthContextProvider>
          <AuthProvider>
            <RouterProvider router={router} />
            <Toaster richColors closeButton />
          </AuthProvider>
        </AuthContextProvider>
      </QueryClientProvider>
    </ThemeProvider>
  )
}
