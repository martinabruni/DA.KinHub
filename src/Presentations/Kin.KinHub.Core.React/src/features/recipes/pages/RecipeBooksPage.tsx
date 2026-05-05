import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { MoreHorizontal, Plus } from 'lucide-react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardFooter } from '@/components/ui/card'
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
    <div>
      <div className="flex items-center justify-between gap-4 flex-wrap mb-4">
        <h1 className="text-2xl font-bold">{t('recipeBooks.title')}</h1>
        <Dialog open={createOpen} onOpenChange={setCreateOpen}>
          <DialogTrigger asChild>
            <Button><Plus className="w-4 h-4 mr-1" />{t('recipeBooks.new')}</Button>
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
        className="max-w-sm mb-6"
      />

      {isLoading ? (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
          {Array.from({ length: 4 }).map((_, i) => <Skeleton key={i} className="h-52 rounded-xl" />)}
        </div>
      ) : filtered.length === 0 ? (
        <div className="flex flex-col items-center gap-4 py-16">
          <span className="text-5xl">📚</span>
          <p className="text-muted-foreground font-medium">{t('recipeBooks.empty.title')}</p>
          <Button onClick={() => setCreateOpen(true)}>{t('recipeBooks.empty.cta')}</Button>
        </div>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
          {filtered.map((book) => (
            <Card
              key={book.id}
              className="overflow-hidden hover:shadow-lg hover:scale-[1.02] transition-all cursor-pointer"
              onClick={() => navigate(`/recipe-books/${book.id}`)}
            >
              <div
                className="aspect-[4/3] relative flex items-end p-3"
                style={{ background: `linear-gradient(135deg, ${hashColor(book.id)}, ${hashColor(book.id + 'x')})` }}
              >
                <p className="text-white font-bold text-lg drop-shadow">{book.name}</p>
              </div>
              <CardContent className="pt-3 pb-0">
                <Badge variant="secondary" className="text-xs">
                  {book.recipeCount} {book.recipeCount === 1 ? t('recipeBooks.recipe') : t('recipeBooks.recipes')}
                </Badge>
                <p className="text-xs text-muted-foreground mt-1">
                  {t('recipeBooks.lastUpdated', { date: formatDate(book.updatedAt) })}
                </p>
              </CardContent>
              <CardFooter className="pt-2 pb-3">
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
                      <AlertDialogTitle>{t('recipeBooks.delete.title')}</AlertDialogTitle>
                      <AlertDialogDescription>{t('recipeBooks.delete.description', { name: book.name })}</AlertDialogDescription>
                    </AlertDialogHeader>
                    <AlertDialogFooter>
                      <AlertDialogCancel>{t('common.cancel')}</AlertDialogCancel>
                      <AlertDialogAction onClick={(e) => { e.stopPropagation(); deleteBook(book.id) }}>{t('recipeBooks.delete.confirm')}</AlertDialogAction>
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

export function RecipeBooksPage() {
  return (
    <RecipeBookProvider>
      <RecipeBooksContent />
    </RecipeBookProvider>
  )
}
