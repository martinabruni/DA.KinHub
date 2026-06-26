import type { ReactNode } from 'react'
import { createContext, useCallback, useContext } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { useTranslation } from 'react-i18next'
import { apiClient } from '@/api/apiClient'
import type { BulkAddShoppingListItemsResponse, ShoppingList, ShoppingListItem } from '@/types'

interface ShoppingListContextValue {
  shoppingLists: ShoppingList[]
  isLoading: boolean
  createList: (name: string) => Promise<void>
  updateList: (id: string, name: string) => Promise<void>
  deleteList: (id: string) => Promise<void>
  getItems: (listId: string) => ShoppingListItem[]
  isItemsLoading: (listId: string) => boolean
  addItem: (listId: string, name: string) => Promise<void>
  bulkAddItems: (listId: string, names: string[]) => Promise<BulkAddShoppingListItemsResponse>
  toggleItem: (listId: string, itemId: string) => Promise<void>
  deleteItem: (listId: string, itemId: string) => Promise<void>
  deleteChecked: (listId: string) => Promise<void>
}

const ShoppingListContext = createContext<ShoppingListContextValue | null>(null)

export function ShoppingListProvider({ children }: { children: ReactNode }) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const qKey = ['shopping-lists']

  const { data: shoppingLists = [], isLoading } = useQuery({
    queryKey: qKey,
    queryFn: async () => {
      const { data } = await apiClient.get<ShoppingList[]>('/api/shopping-lists')
      return data
    },
  })

  const invalidateLists = useCallback(() => {
    queryClient.invalidateQueries({ queryKey: qKey })
  }, [queryClient])

  const invalidateItems = useCallback((listId: string) => {
    queryClient.invalidateQueries({ queryKey: ['shopping-list-items', listId] })
  }, [queryClient])

  const createMutation = useMutation({
    mutationFn: (name: string) => apiClient.post('/api/shopping-lists', { name }),
    onSuccess: () => { toast.success(t('shoppingLists.created')); invalidateLists() },
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, name }: { id: string; name: string }) =>
      apiClient.put(`/api/shopping-lists/${id}`, { name }),
    onSuccess: () => { toast.success(t('shoppingLists.updated')); invalidateLists() },
  })

  const deleteMutation = useMutation({
    mutationFn: (id: string) => apiClient.delete(`/api/shopping-lists/${id}`),
    onSuccess: () => { toast.success(t('shoppingLists.deleted')); invalidateLists() },
  })

  const addItemMutation = useMutation({
    mutationFn: ({ listId, name }: { listId: string; name: string }) =>
      apiClient.post(`/api/shopping-lists/${listId}/items`, { name, shoppingListId: listId }),
    onSuccess: (_, { listId }) => {
      toast.success(t('shoppingLists.itemAdded'))
      invalidateItems(listId)
      invalidateLists()
    },
  })

  const bulkAddItemsMutation = useMutation({
    mutationFn: ({ listId, names }: { listId: string; names: string[] }) =>
      apiClient.post<BulkAddShoppingListItemsResponse>(`/api/shopping-lists/${listId}/items/bulk`, { names, shoppingListId: listId }),
    onSuccess: (_, { listId }) => {
      invalidateItems(listId)
      invalidateLists()
    },
  })

  const toggleItemMutation = useMutation({
    mutationFn: ({ listId, itemId }: { listId: string; itemId: string }) =>
      apiClient.patch(`/api/shopping-lists/${listId}/items/${itemId}/toggle`),
    onSuccess: (_, { listId }) => {
      invalidateItems(listId)
      invalidateLists()
    },
  })

  const deleteItemMutation = useMutation({
    mutationFn: ({ listId, itemId }: { listId: string; itemId: string }) =>
      apiClient.delete(`/api/shopping-lists/${listId}/items/${itemId}`),
    onSuccess: (_, { listId }) => {
      toast.success(t('shoppingLists.itemDeleted'))
      invalidateItems(listId)
      invalidateLists()
    },
  })

  const deleteCheckedMutation = useMutation({
    mutationFn: (listId: string) =>
      apiClient.delete(`/api/shopping-lists/${listId}/items/checked`),
    onSuccess: (_, listId) => {
      toast.success(t('shoppingLists.checkedDeleted'))
      invalidateItems(listId)
      invalidateLists()
    },
  })

  return (
    <ShoppingListContext.Provider
      value={{
        shoppingLists,
        isLoading,
        createList: async (name) => { await createMutation.mutateAsync(name) },
        updateList: async (id, name) => { await updateMutation.mutateAsync({ id, name }) },
        deleteList: async (id) => { await deleteMutation.mutateAsync(id) },
        getItems: (listId) => {
          return queryClient.getQueryData<ShoppingListItem[]>(['shopping-list-items', listId]) ?? []
        },
        isItemsLoading: (listId) => {
          return queryClient.isFetching({ queryKey: ['shopping-list-items', listId] }) > 0
        },
        addItem: async (listId, name) => { await addItemMutation.mutateAsync({ listId, name }) },
        bulkAddItems: async (listId, names) => {
          const res = await bulkAddItemsMutation.mutateAsync({ listId, names })
          return res.data
        },
        toggleItem: async (listId, itemId) => { await toggleItemMutation.mutateAsync({ listId, itemId }) },
        deleteItem: async (listId, itemId) => { await deleteItemMutation.mutateAsync({ listId, itemId }) },
        deleteChecked: async (listId) => { await deleteCheckedMutation.mutateAsync(listId) },
      }}
    >
      {children}
    </ShoppingListContext.Provider>
  )
}

export function useShoppingLists() {
  const ctx = useContext(ShoppingListContext)
  if (!ctx) throw new Error('useShoppingLists must be used within ShoppingListProvider')
  return ctx
}
