import { useState } from 'react'
import { useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Check, Plus, ShoppingCart, Trash2 } from 'lucide-react'
import { useQuery } from '@tanstack/react-query'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Skeleton } from '@/components/ui/skeleton'
import { Badge } from '@/components/ui/badge'
import { AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent, AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle, AlertDialogTrigger } from '@/components/ui/alert-dialog'
import { ShoppingListProvider, useShoppingLists } from '@/features/shopping-lists/ShoppingListProvider'
import { apiClient } from '@/api/apiClient'
import type { ShoppingList, ShoppingListItem } from '@/types'

function ShoppingListDetailContent() {
  const { t } = useTranslation()
  const { id } = useParams<{ id: string }>()
  const { addItem, toggleItem, deleteItem, deleteChecked } = useShoppingLists()
  const [newItemName, setNewItemName] = useState('')

  const { data: list, isLoading: listLoading } = useQuery({
    queryKey: ['shopping-list', id],
    queryFn: async () => {
      const { data } = await apiClient.get<ShoppingList>(`/api/shopping-lists/${id}`)
      return data
    },
    enabled: !!id,
  })

  const { data: items = [], isLoading: itemsLoading } = useQuery({
    queryKey: ['shopping-list-items', id],
    queryFn: async () => {
      const { data } = await apiClient.get<ShoppingListItem[]>(`/api/shopping-lists/${id}/items`)
      return data
    },
    enabled: !!id,
  })

  const checkedCount = items.filter((i) => i.isChecked).length

  const handleAddItem = async () => {
    if (!newItemName.trim() || !id) return
    await addItem(id, newItemName.trim())
    setNewItemName('')
  }

  return (
    <div>
      <div className="flex items-center gap-3 mb-2">
        <ShoppingCart className="w-6 h-6 text-primary" />
        <h1 className="text-2xl font-bold">{list?.name ?? '...'}</h1>
      </div>

      {items.length > 0 && (
        <p className="text-sm text-muted-foreground mb-4">
          {t('shoppingLists.checkedCount', { checked: checkedCount, total: items.length })}
        </p>
      )}

      <div className="flex gap-3 mb-4 flex-wrap">
        <div className="flex gap-2 flex-1">
          <Input
            placeholder={t('shoppingLists.addItemPlaceholder')}
            value={newItemName}
            onChange={(e) => setNewItemName(e.target.value)}
            onKeyDown={(e) => e.key === 'Enter' && handleAddItem()}
            className="flex-1"
          />
          <Button onClick={handleAddItem} disabled={!newItemName.trim()}>
            <Plus className="w-4 h-4 mr-1" />{t('shoppingLists.addItem')}
          </Button>
        </div>
        {checkedCount > 0 && (
          <AlertDialog>
            <AlertDialogTrigger asChild>
              <Button variant="outline" size="sm" className="text-destructive border-destructive/30">
                <Trash2 className="w-4 h-4 mr-1" />{t('shoppingLists.deleteChecked')}
              </Button>
            </AlertDialogTrigger>
            <AlertDialogContent>
              <AlertDialogHeader>
                <AlertDialogTitle>{t('shoppingLists.deleteChecked')}</AlertDialogTitle>
                <AlertDialogDescription>{t('shoppingLists.deleteCheckedConfirm')}</AlertDialogDescription>
              </AlertDialogHeader>
              <AlertDialogFooter>
                <AlertDialogCancel>{t('common.cancel')}</AlertDialogCancel>
                <AlertDialogAction onClick={() => deleteChecked(id!)}>{t('common.delete')}</AlertDialogAction>
              </AlertDialogFooter>
            </AlertDialogContent>
          </AlertDialog>
        )}
      </div>

      {listLoading || itemsLoading ? (
        <Skeleton className="h-64 w-full rounded-xl" />
      ) : items.length === 0 ? (
        <div className="flex flex-col items-center gap-3 py-16">
          <ShoppingCart className="w-10 h-10 text-muted-foreground" />
          <p className="text-muted-foreground">{t('shoppingLists.emptyItems.title')}</p>
          <p className="text-muted-foreground text-sm">{t('shoppingLists.emptyItems.cta')}</p>
        </div>
      ) : (
        <div className="space-y-2">
          {items.map((item) => (
            <div
              key={item.id}
              className="flex items-center gap-3 p-3 rounded-lg border bg-card hover:bg-muted/30 transition-colors"
            >
              <button
                onClick={() => toggleItem(id!, item.id)}
                className={`w-5 h-5 rounded border-2 flex items-center justify-center shrink-0 transition-colors ${
                  item.isChecked
                    ? 'bg-primary border-primary text-primary-foreground'
                    : 'border-muted-foreground/40 hover:border-primary'
                }`}
              >
                {item.isChecked && <Check className="w-3 h-3" />}
              </button>
              <span className={`flex-1 text-sm ${item.isChecked ? 'line-through text-muted-foreground' : ''}`}>
                {item.name}
              </span>
              {item.isChecked && (
                <Badge variant="secondary" className="text-xs shrink-0">
                  ✓
                </Badge>
              )}
              <AlertDialog>
                <AlertDialogTrigger asChild>
                  <Button variant="ghost" size="icon" className="h-7 w-7 shrink-0">
                    <Trash2 className="w-3 h-3 text-destructive" />
                  </Button>
                </AlertDialogTrigger>
                <AlertDialogContent>
                  <AlertDialogHeader>
                    <AlertDialogTitle>{t('common.delete')}</AlertDialogTitle>
                    <AlertDialogDescription>{item.name}</AlertDialogDescription>
                  </AlertDialogHeader>
                  <AlertDialogFooter>
                    <AlertDialogCancel>{t('common.cancel')}</AlertDialogCancel>
                    <AlertDialogAction onClick={() => deleteItem(id!, item.id)}>{t('common.delete')}</AlertDialogAction>
                  </AlertDialogFooter>
                </AlertDialogContent>
              </AlertDialog>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}

export function ShoppingListDetailPage() {
  return (
    <ShoppingListProvider>
      <ShoppingListDetailContent />
    </ShoppingListProvider>
  )
}
