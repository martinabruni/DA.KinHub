import { QueryClient, QueryClientProvider, QueryCache } from '@tanstack/react-query'
import { ThemeProvider } from 'next-themes'
import { RouterProvider } from 'react-router-dom'
import { toast } from 'sonner'
import { Toaster } from '@/components/ui/sonner'
import { AuthContextProvider } from '@/store/authContext'
import { AuthProvider } from '@/features/auth/AuthProvider'
import { router } from '@/router/routes'
import { getApiErrorMessage, isHttpStatus } from '@/lib/errors'
import './i18n'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: { staleTime: 1000 * 60, retry: 1 },
  },
  queryCache: new QueryCache({
    onError: (error) => {
      if (isHttpStatus(error, 404) || isHttpStatus(error, 401) || isHttpStatus(error, 403)) return
      toast.error(getApiErrorMessage(error))
    },
  }),
})

function App() {
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

export default App
