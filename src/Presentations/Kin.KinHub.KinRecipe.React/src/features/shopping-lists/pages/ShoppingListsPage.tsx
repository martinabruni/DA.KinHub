import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { MoreHorizontal, Plus, ShoppingCart } from 'lucide-react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardFooter } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog'
import { AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent, AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle, AlertDialogTrigger } from '@/components/ui/alert-dialog'
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '@/components/ui/dropdown-menu'
import { Input } from '@/components/ui/input'
import { ShoppingListProvider, useShoppingLists } from '@/features/shopping-lists/ShoppingListProvider'

function ShoppingListsContent() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const { shoppingLists, isLoading, createList, deleteList } = useShoppingLists()
  const [createOpen, setCreateOpen] = useState(false)

  const form = useForm<{ name: string }>({
    resolver: zodResolver(z.object({ name: z.string().min(1) })),
    defaultValues: { name: '' },
  })

  return (
    <div className="space-y-5">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-bold">{t('shoppingLists.title')}</h1>
          <p className="mt-1 text-sm text-muted-foreground">{t('shoppingLists.empty.cta')}</p>
        </div>
        <Dialog open={createOpen} onOpenChange={setCreateOpen}>
          <DialogTrigger asChild>
            <Button className="w-full sm:w-auto"><Plus className="w-4 h-4 mr-1" />{t('shoppingLists.new')}</Button>
          </DialogTrigger>
          <DialogContent>
            <DialogHeader><DialogTitle>{t('shoppingLists.create.title')}</DialogTitle></DialogHeader>
            <form onSubmit={form.handleSubmit(async (v) => { await createList(v.name); setCreateOpen(false); form.reset() })}>
              <Input placeholder={t('shoppingLists.create.namePlaceholder')} {...form.register('name')} className="mt-2" />
              <DialogFooter className="mt-4">
                <Button type="submit">{t('shoppingLists.create.submit')}</Button>
              </DialogFooter>
            </form>
          </DialogContent>
        </Dialog>
      </div>

      {isLoading ? (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-3">
          {Array.from({ length: 3 }).map((_, i) => <Skeleton key={i} className="h-36 rounded-xl" />)}
        </div>
      ) : shoppingLists.length === 0 ? (
        <div className="flex flex-col items-center gap-4 py-16">
          <ShoppingCart className="w-12 h-12 text-muted-foreground" />
          <p className="text-muted-foreground font-medium">{t('shoppingLists.empty.title')}</p>
          <Button onClick={() => setCreateOpen(true)}>{t('shoppingLists.empty.cta')}</Button>
        </div>
      ) : (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-3">
          {shoppingLists.map((list) => (
            <Card
              key={list.id}
              className="cursor-pointer hover:shadow-md hover:scale-[1.01] transition-all"
              onClick={() => navigate(`/shopping-lists/${list.id}`)}
            >
              <CardContent className="pt-5 pb-3 flex items-start gap-3">
                <ShoppingCart className="w-8 h-8 text-primary mt-0.5" />
                <div className="flex-1">
                  <p className="font-semibold">{list.name}</p>
                  <div className="flex gap-2 mt-1 flex-wrap">
                    <Badge variant="secondary" className="text-xs">
                      {t('shoppingLists.itemCount', { count: list.itemCount })}
                    </Badge>
                    {list.itemCount > 0 && (
                      <Badge variant="outline" className="text-xs">
                        {t('shoppingLists.checkedCount', { checked: list.checkedCount, total: list.itemCount })}
                      </Badge>
                    )}
                  </div>
                </div>
              </CardContent>
              <CardFooter className="pb-3 pt-0">
                <AlertDialog>
                  <DropdownMenu>
                    <DropdownMenuTrigger asChild onClick={(e) => e.stopPropagation()}>
                      <Button variant="ghost" size="icon" className="ml-auto">
                        <MoreHorizontal className="w-4 h-4" />
                      </Button>
                    </DropdownMenuTrigger>
                    <DropdownMenuContent>
                      <AlertDialogTrigger asChild>
                        <DropdownMenuItem className="text-destructive" onClick={(e) => e.stopPropagation()}>
                          {t('common.delete')}
                        </DropdownMenuItem>
                      </AlertDialogTrigger>
                    </DropdownMenuContent>
                  </DropdownMenu>
                  <AlertDialogContent>
                    <AlertDialogHeader>
                      <AlertDialogTitle>{t('shoppingLists.delete.title')}</AlertDialogTitle>
                      <AlertDialogDescription>{t('shoppingLists.delete.description', { name: list.name })}</AlertDialogDescription>
                    </AlertDialogHeader>
                    <AlertDialogFooter>
                      <AlertDialogCancel>{t('common.cancel')}</AlertDialogCancel>
                      <AlertDialogAction onClick={() => deleteList(list.id)}>{t('shoppingLists.delete.confirm')}</AlertDialogAction>
                    </AlertDialogFooter>
                  </AlertDialogContent>
                </AlertDialog>
              </CardFooter>
            </Card>
          ))}
        </div>
      )}
    </div>
  )
}

export function ShoppingListsPage() {
  return (
    <ShoppingListProvider>
      <ShoppingListsContent />
    </ShoppingListProvider>
  )
}
