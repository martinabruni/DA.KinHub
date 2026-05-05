import type { ReactNode } from 'react'
import { createContext, useCallback, useContext } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { useTranslation } from 'react-i18next'
import { apiClient } from '@/api/apiClient'
import { useAuthContext } from '@/store/authContext'
import type { RecipeBook } from '@/types'

interface RecipeBookContextValue {
  books: RecipeBook[]
  isLoading: boolean
  createBook: (name: string) => Promise<RecipeBook>
  updateBook: (id: string, name: string) => Promise<void>
  deleteBook: (id: string) => Promise<void>
  getBook: (id: string) => RecipeBook | undefined
}

const RecipeBookContext = createContext<RecipeBookContextValue | null>(null)

export function RecipeBookProvider({ children }: { children: ReactNode }) {
  const { t } = useTranslation()
  const { isAuthenticated } = useAuthContext()
  const queryClient = useQueryClient()
  const qKey = ['recipe-books']

  const { data: books = [], isLoading } = useQuery({
    queryKey: qKey,
    queryFn: async () => {
      const { data } = await apiClient.get<RecipeBook[]>('/api/recipe-books')
      return data
    },
    enabled: isAuthenticated,
  })

  const invalidate = useCallback(() => {
    queryClient.invalidateQueries({ queryKey: qKey })
  }, [queryClient])

  const createMutation = useMutation({
    mutationFn: async (name: string) => {
      const { data } = await apiClient.post<RecipeBook>('/api/recipe-books', { name })
      return data
    },
    onSuccess: () => { toast.success(t('recipeBooks.created')); invalidate() },
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, name }: { id: string; name: string }) =>
      apiClient.put(`/api/recipe-books/${id}`, { name }),
    onSuccess: () => { toast.success(t('recipeBooks.updated')); invalidate() },
  })

  const deleteMutation = useMutation({
    mutationFn: (id: string) => apiClient.delete(`/api/recipe-books/${id}`),
    onSuccess: () => { toast.success(t('recipeBooks.deleted')); invalidate() },
  })

  return (
    <RecipeBookContext.Provider
      value={{
        books,
        isLoading,
        createBook: (name) => createMutation.mutateAsync(name),
        updateBook: async (id, name) => { await updateMutation.mutateAsync({ id, name }) },
        deleteBook: async (id) => { await deleteMutation.mutateAsync(id) },
        getBook: (id) => books.find((b) => b.id === id),
      }}
    >
      {children}
    </RecipeBookContext.Provider>
  )
}

export function useRecipeBooks() {
  const ctx = useContext(RecipeBookContext)
  if (!ctx) throw new Error('useRecipeBooks must be used within RecipeBookProvider')
  return ctx
}
