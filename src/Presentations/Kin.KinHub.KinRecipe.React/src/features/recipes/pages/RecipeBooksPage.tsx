import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { MoreHorizontal, Plus } from 'lucide-react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Skeleton } from '@/components/ui/skeleton'
import { Badge } from '@/components/ui/badge'
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog'
import { AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent, AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle, AlertDialogTrigger } from '@/components/ui/alert-dialog'
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '@/components/ui/dropdown-menu'
import { RecipeBookProvider, useRecipeBooks } from '@/features/recipes/RecipeBookProvider'
import { hashColor, formatDate } from '@/lib/utils'

function RecipeBooksContent() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const { books, isLoading, createBook, deleteBook } = useRecipeBooks()
  const [search, setSearch] = useState('')
  const [createOpen, setCreateOpen] = useState(false)

  const form = useForm<{ name: string }>({
    resolver: zodResolver(z.object({ name: z.string().min(1) })),
    defaultValues: { name: '' },
  })

  const filtered = books.filter((b) => b.name.toLowerCase().includes(search.toLowerCase()))

  return (
    <div className="space-y-5">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-bold">{t('recipeBooks.title')}</h1>
          <p className="mt-1 text-sm text-muted-foreground">{t('recipeBooks.search')}</p>
        </div>
        <Dialog open={createOpen} onOpenChange={setCreateOpen}>
          <DialogTrigger asChild>
            <Button className="w-full sm:w-auto"><Plus className="w-4 h-4 mr-1" />{t('recipeBooks.new')}</Button>
          </DialogTrigger>
          <DialogContent>
            <DialogHeader><DialogTitle>{t('recipeBooks.create.title')}</DialogTitle></DialogHeader>
            <form onSubmit={form.handleSubmit(async (v) => { await createBook(v.name); setCreateOpen(false); form.reset() })}>
              <Input placeholder={t('recipeBooks.create.namePlaceholder')} {...form.register('name')} className="mt-2" />
              <DialogFooter className="mt-4">
                <Button type="submit">{t('recipeBooks.create.submit')}</Button>
              </DialogFooter>
            </form>
          </DialogContent>
        </Dialog>
      </div>

      <Input
        placeholder={t('recipeBooks.search')}
        value={search}
        onChange={(e) => setSearch(e.target.value)}
        className="h-11 w-full sm:max-w-sm"
      />

      {isLoading ? (
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4">
          {Array.from({ length: 4 }).map((_, i) => <Skeleton key={i} className="aspect-square rounded-3xl" />)}
        </div>
      ) : filtered.length === 0 ? (
        <div className="flex flex-col items-center gap-4 rounded-3xl border border-dashed border-border/80 bg-card/40 px-6 py-14 text-center">
          <span className="text-5xl">📚</span>
          <p className="text-muted-foreground font-medium">{t('recipeBooks.empty.title')}</p>
          <Button onClick={() => setCreateOpen(true)} className="w-full sm:w-auto">{t('recipeBooks.empty.cta')}</Button>
        </div>
      ) : (
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4">
          {filtered.map((book) => (
            <Card
              key={book.id}
              className="h-full cursor-pointer border-border/70 bg-card/80 transition-all hover:-translate-y-0.5 hover:border-primary/40 hover:shadow-md"
              onClick={() => navigate(`/recipe-books/${book.id}`)}
            >
              <CardContent className="flex aspect-square h-full flex-col p-4 sm:p-5">
                <AlertDialog>
                  <div className="flex items-start justify-between gap-3">
                    <div
                      className="flex h-11 w-11 shrink-0 items-center justify-center rounded-2xl"
                      style={{ background: `linear-gradient(135deg, ${hashColor(book.id)}, ${hashColor(book.id + 'x')})` }}
                    >
                      <span className="text-sm font-semibold text-white">
                        {book.name.slice(0, 2).toUpperCase()}
                      </span>
                    </div>
                    <DropdownMenu>
                      <DropdownMenuTrigger asChild onClick={(e) => e.stopPropagation()}>
                        <Button variant="ghost" size="icon" className="-mr-2 -mt-2 h-9 w-9 rounded-2xl">
                          <MoreHorizontal className="h-4 w-4" />
                        </Button>
                      </DropdownMenuTrigger>
                      <DropdownMenuContent align="end">
                        <AlertDialogTrigger asChild>
                          <DropdownMenuItem className="text-destructive" onClick={(e) => e.stopPropagation()}>
                            {t('common.delete')}
                          </DropdownMenuItem>
                        </AlertDialogTrigger>
                      </DropdownMenuContent>
                    </DropdownMenu>
                  </div>
                  <div className="mt-4 flex-1">
                    <p className="line-clamp-2 font-semibold leading-tight">{book.name}</p>
                    <Badge variant="secondary" className="mt-3 text-xs">
                      {book.recipeCount} {book.recipeCount === 1 ? t('recipeBooks.recipe') : t('recipeBooks.recipes')}
                    </Badge>
                    <p className="mt-3 text-xs text-muted-foreground">
                      {t('recipeBooks.lastUpdated', { date: formatDate(book.updatedAt) })}
                    </p>
                  </div>
                  <AlertDialogContent>
                    <AlertDialogHeader>
                      <AlertDialogTitle>{t('recipeBooks.delete.title')}</AlertDialogTitle>
                      <AlertDialogDescription>{t('recipeBooks.delete.description', { name: book.name })}</AlertDialogDescription>
                    </AlertDialogHeader>
                    <AlertDialogFooter>
                      <AlertDialogCancel>{t('common.cancel')}</AlertDialogCancel>
                      <AlertDialogAction onClick={(e) => { e.stopPropagation(); deleteBook(book.id) }}>{t('recipeBooks.delete.confirm')}</AlertDialogAction>
                    </AlertDialogFooter>
                  </AlertDialogContent>
                </AlertDialog>
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  )
}

export function RecipeBooksPage() {
  return (
    <RecipeBookProvider>
      <RecipeBooksContent />
    </RecipeBookProvider>
  )
}
