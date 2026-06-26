import { useState } from 'react'
import { useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Check, Pencil, Plus, Refrigerator, Trash2, X } from 'lucide-react'
import { useForm } from 'react-hook-form'
import { useQuery } from '@tanstack/react-query'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Skeleton } from '@/components/ui/skeleton'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent, AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle, AlertDialogTrigger } from '@/components/ui/alert-dialog'
import { FridgeProvider, useFridges } from '@/features/fridges/FridgeProvider'
import { apiClient } from '@/api/apiClient'
import type { Fridge, FridgeIngredient } from '@/types'

function FridgeDetailContent() {
  const { t } = useTranslation()
  const { id } = useParams<{ id: string }>()
  const { addIngredient, updateIngredient, deleteIngredient } = useFridges()

  const [search, setSearch] = useState('')
  const [addOpen, setAddOpen] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [editValues, setEditValues] = useState<Omit<FridgeIngredient, 'id'>>({ name: '', quantity: 0, unit: '' })
  const addForm = useForm<Omit<FridgeIngredient, 'id'>>({ defaultValues: { name: '', quantity: 0, unit: '' } })

  const { data: fridge, isLoading } = useQuery({
    queryKey: ['fridge', id],
    queryFn: async () => {
      const { data } = await apiClient.get<Fridge>(`/api/fridges/${id}`)
      return data
    },
    enabled: !!id,
  })

  const { data: ingredients = [] } = useQuery({
    queryKey: ['fridge-ingredients', id],
    queryFn: async () => {
      const { data } = await apiClient.get<FridgeIngredient[]>(`/api/fridges/${id}/ingredients`)
      return data
    },
    enabled: !!id,
  })

  const filtered = ingredients.filter((i) =>
    i.name.toLowerCase().includes(search.toLowerCase()),
  )

  const startEdit = (ing: FridgeIngredient) => {
    setEditingId(ing.id)
    setEditValues({ name: ing.name, quantity: ing.quantity, unit: ing.unit })
  }

  return (
    <div>
      <div className="flex items-center gap-3 mb-6">
        <Refrigerator className="w-6 h-6 text-primary" />
        <h1 className="text-2xl font-bold">{fridge?.name ?? '...'}</h1>
      </div>

      <div className="mb-4 flex flex-col gap-3 sm:flex-row sm:flex-wrap">
        <div className="relative flex-1 sm:max-w-sm">
          <Input
            placeholder={t('fridges.search')}
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
          {search && (
            <Button variant="ghost" size="icon" className="absolute right-0 top-0 h-full" onClick={() => setSearch('')}>
              <X className="w-4 h-4" />
            </Button>
          )}
        </div>
        <Button onClick={() => setAddOpen(true)} className="w-full sm:w-auto">
          <Plus className="w-4 h-4 mr-1" />{t('fridges.addIngredient')}
        </Button>
      </div>

      {/* Add ingredient dialog */}
      <Dialog open={addOpen} onOpenChange={setAddOpen}>
        <DialogContent>
          <DialogHeader><DialogTitle>{t('fridges.addIngredient')}</DialogTitle></DialogHeader>
          <form
            onSubmit={addForm.handleSubmit(async (v) => {
              await addIngredient(id!, v)
              addForm.reset()
              setAddOpen(false)
            })}
            className="space-y-3 mt-2"
          >
            <Input placeholder={t('fridges.columns.name')} {...addForm.register('name')} />
            <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
              <Input type="number" placeholder={t('fridges.columns.quantity')} {...addForm.register('quantity', { valueAsNumber: true })} />
              <Input placeholder={t('fridges.columns.unit')} {...addForm.register('unit')} />
            </div>
            <DialogFooter>
              <Button type="submit">{t('fridges.create.submit')}</Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {isLoading ? (
        <Skeleton className="h-64 w-full rounded-xl" />
      ) : filtered.length === 0 ? (
        <div className="flex flex-col items-center gap-3 py-16">
          <Refrigerator className="w-10 h-10 text-muted-foreground" />
          <p className="text-muted-foreground">{t('fridges.empty.title')}</p>
          <p className="text-muted-foreground text-sm">{t('fridges.empty.cta')}</p>
        </div>
      ) : (
        <>
          <div className="space-y-3 sm:hidden">
            {filtered.map((ing) => (
              <Card key={ing.id}>
                <CardContent className="space-y-3 p-4">
                  {editingId === ing.id ? (
                    <>
                      <Input value={editValues.name} onChange={(e) => setEditValues((v) => ({ ...v, name: e.target.value }))} className="h-10" />
                      <div className="grid grid-cols-2 gap-3">
                        <Input type="number" value={editValues.quantity} onChange={(e) => setEditValues((v) => ({ ...v, quantity: Number(e.target.value) }))} className="h-10" />
                        <Input value={editValues.unit} onChange={(e) => setEditValues((v) => ({ ...v, unit: e.target.value }))} className="h-10" />
                      </div>
                      <div className="flex gap-2">
                        <Button size="sm" className="flex-1" onClick={async () => { await updateIngredient(id!, ing.id, editValues); setEditingId(null) }}>
                          <Check className="mr-1 h-3 w-3" />
                          {t('common.save')}
                        </Button>
                        <Button size="sm" variant="ghost" className="flex-1" onClick={() => setEditingId(null)}>
                          {t('common.cancel')}
                        </Button>
                      </div>
                    </>
                  ) : (
                    <>
                      <div className="flex items-start justify-between gap-3">
                        <div>
                          <p className="font-medium">{ing.name}</p>
                          <p className="mt-1 text-sm text-muted-foreground">{ing.quantity} {ing.unit}</p>
                        </div>
                        <div className="flex gap-1">
                          <Button variant="ghost" size="icon" className="h-8 w-8" onClick={() => startEdit(ing)}>
                            <Pencil className="h-3.5 w-3.5" />
                          </Button>
                          <AlertDialog>
                            <AlertDialogTrigger asChild>
                              <Button variant="ghost" size="icon" className="h-8 w-8">
                                <Trash2 className="h-3.5 w-3.5 text-destructive" />
                              </Button>
                            </AlertDialogTrigger>
                            <AlertDialogContent>
                              <AlertDialogHeader>
                                <AlertDialogTitle>{t('fridges.deleteIngredient.title')}</AlertDialogTitle>
                                <AlertDialogDescription>{t('fridges.deleteIngredient.description', { name: ing.name })}</AlertDialogDescription>
                              </AlertDialogHeader>
                              <AlertDialogFooter>
                                <AlertDialogCancel>{t('common.cancel')}</AlertDialogCancel>
                                <AlertDialogAction onClick={() => deleteIngredient(id!, ing.id)}>{t('fridges.deleteIngredient.confirm')}</AlertDialogAction>
                              </AlertDialogFooter>
                            </AlertDialogContent>
                          </AlertDialog>
                        </div>
                      </div>
                    </>
                  )}
                </CardContent>
              </Card>
            ))}
          </div>
          <div className="hidden overflow-auto rounded-lg border sm:block">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>{t('fridges.columns.name')}</TableHead>
                <TableHead>{t('fridges.columns.quantity')}</TableHead>
                <TableHead>{t('fridges.columns.unit')}</TableHead>
                <TableHead>{t('fridges.columns.actions')}</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {filtered.map((ing) =>
                editingId === ing.id ? (
                  <TableRow key={ing.id}>
                    <TableCell>
                      <Input value={editValues.name} onChange={(e) => setEditValues((v) => ({ ...v, name: e.target.value }))} className="h-8" />
                    </TableCell>
                    <TableCell>
                      <Input type="number" value={editValues.quantity} onChange={(e) => setEditValues((v) => ({ ...v, quantity: Number(e.target.value) }))} className="h-8 w-20" />
                    </TableCell>
                    <TableCell>
                      <Input value={editValues.unit} onChange={(e) => setEditValues((v) => ({ ...v, unit: e.target.value }))} className="h-8 w-20" />
                    </TableCell>
                    <TableCell>
                      <div className="flex gap-1">
                        <Button size="icon" className="h-7 w-7" onClick={async () => { await updateIngredient(id!, ing.id, editValues); setEditingId(null) }}>
                          <Check className="w-3 h-3" />
                        </Button>
                        <Button size="icon" variant="ghost" className="h-7 w-7" onClick={() => setEditingId(null)}>
                          <X className="w-3 h-3" />
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                ) : (
                  <TableRow key={ing.id}>
                    <TableCell className="font-medium">{ing.name}</TableCell>
                    <TableCell>{ing.quantity}</TableCell>
                    <TableCell>{ing.unit}</TableCell>
                    <TableCell>
                      <div className="flex gap-1">
                        <Button variant="ghost" size="icon" className="h-7 w-7" onClick={() => startEdit(ing)}>
                          <Pencil className="w-3 h-3" />
                        </Button>
                        <AlertDialog>
                          <AlertDialogTrigger asChild>
                            <Button variant="ghost" size="icon" className="h-7 w-7">
                              <Trash2 className="w-3 h-3 text-destructive" />
                            </Button>
                          </AlertDialogTrigger>
                          <AlertDialogContent>
                            <AlertDialogHeader>
                              <AlertDialogTitle>{t('fridges.deleteIngredient.title')}</AlertDialogTitle>
                              <AlertDialogDescription>{t('fridges.deleteIngredient.description', { name: ing.name })}</AlertDialogDescription>
                            </AlertDialogHeader>
                            <AlertDialogFooter>
                              <AlertDialogCancel>{t('common.cancel')}</AlertDialogCancel>
                              <AlertDialogAction onClick={() => deleteIngredient(id!, ing.id)}>{t('fridges.deleteIngredient.confirm')}</AlertDialogAction>
                            </AlertDialogFooter>
                          </AlertDialogContent>
                        </AlertDialog>
                      </div>
                    </TableCell>
                  </TableRow>
                ),
              )}
            </TableBody>
          </Table>
          </div>
        </>
      )}
    </div>
  )
}

export function FridgeDetailPage() {
  return (
    <FridgeProvider>
      <FridgeDetailContent />
    </FridgeProvider>
  )
}
