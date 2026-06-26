import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { MoreHorizontal, Plus, Refrigerator } from 'lucide-react'
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
import { FridgeProvider, useFridges } from '@/features/fridges/FridgeProvider'

function FridgesContent() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const { fridges, isLoading, createFridge, deleteFridge } = useFridges()
  const [createOpen, setCreateOpen] = useState(false)

  const form = useForm<{ name: string }>({
    resolver: zodResolver(z.object({ name: z.string().min(1) })),
    defaultValues: { name: '' },
  })

  return (
    <div>
      <div className="flex items-center justify-between flex-wrap gap-4 mb-6">
        <h1 className="text-2xl font-bold">{t('fridges.title')}</h1>
        <Dialog open={createOpen} onOpenChange={setCreateOpen}>
          <DialogTrigger asChild>
            <Button><Plus className="w-4 h-4 mr-1" />{t('fridges.new')}</Button>
          </DialogTrigger>
          <DialogContent>
            <DialogHeader><DialogTitle>{t('fridges.create.title')}</DialogTitle></DialogHeader>
            <form onSubmit={form.handleSubmit(async (v) => { await createFridge(v.name); setCreateOpen(false); form.reset() })}>
              <Input placeholder={t('fridges.create.namePlaceholder')} {...form.register('name')} className="mt-2" />
              <DialogFooter className="mt-4">
                <Button type="submit">{t('fridges.create.submit')}</Button>
              </DialogFooter>
            </form>
          </DialogContent>
        </Dialog>
      </div>

      {isLoading ? (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
          {Array.from({ length: 3 }).map((_, i) => <Skeleton key={i} className="h-36 rounded-xl" />)}
        </div>
      ) : fridges.length === 0 ? (
        <div className="flex flex-col items-center gap-4 py-16">
          <Refrigerator className="w-12 h-12 text-muted-foreground" />
          <p className="text-muted-foreground font-medium">{t('fridges.noFridges.title')}</p>
          <Button onClick={() => setCreateOpen(true)}>{t('fridges.noFridges.cta')}</Button>
        </div>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
          {fridges.map((fridge) => (
            <Card
              key={fridge.id}
              className="cursor-pointer hover:shadow-md hover:scale-[1.01] transition-all"
              onClick={() => navigate(`/fridges/${fridge.id}`)}
            >
              <CardContent className="pt-5 pb-3 flex items-start gap-3">
                <Refrigerator className="w-8 h-8 text-primary mt-0.5" />
                <div className="flex-1">
                  <p className="font-semibold">{fridge.name}</p>
                  <Badge variant="secondary" className="text-xs mt-1">
                    {fridge.ingredientCount} {fridge.ingredientCount === 1 ? t('fridges.ingredient') : t('fridges.ingredients')}
                  </Badge>
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
                      <AlertDialogTitle>{t('fridges.delete.title')}</AlertDialogTitle>
                      <AlertDialogDescription>{t('fridges.delete.description', { name: fridge.name })}</AlertDialogDescription>
                    </AlertDialogHeader>
                    <AlertDialogFooter>
                      <AlertDialogCancel>{t('common.cancel')}</AlertDialogCancel>
                      <AlertDialogAction onClick={() => deleteFridge(fridge.id)}>{t('fridges.delete.confirm')}</AlertDialogAction>
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

export function FridgesPage() {
  return (
    <FridgeProvider>
      <FridgesContent />
    </FridgeProvider>
  )
}
