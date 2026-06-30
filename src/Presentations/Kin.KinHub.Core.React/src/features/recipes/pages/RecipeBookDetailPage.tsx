import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { MoreHorizontal, Plus } from 'lucide-react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { Separator } from '@/components/ui/separator'
import { Breadcrumb, BreadcrumbItem, BreadcrumbLink, BreadcrumbList, BreadcrumbSeparator } from '@/components/ui/breadcrumb'
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog'
import { AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent, AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle, AlertDialogTrigger } from '@/components/ui/alert-dialog'
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '@/components/ui/dropdown-menu'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import { EntityCard } from '@/components/entity-card'
import { RecipeBookProvider, useRecipeBooks } from '@/features/recipes/RecipeBookProvider'
import { RecipeProvider, useRecipes } from '@/features/recipes/RecipeProvider'
import { hashColor } from '@/lib/utils'

const addSchema = z.object({
  name: z.string().min(1),
  description: z.string().optional(),
  servingSize: z.number().min(1),
  prepTimeMinutes: z.number().min(0),
})

type AddValues = z.infer<typeof addSchema>

function RecipeBookDetailContent() {
  const { t } = useTranslation()
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { getBook } = useRecipeBooks()
  const { recipes, isLoading, createRecipe, deleteRecipe } = useRecipes()
  const [addOpen, setAddOpen] = useState(false)

  const book = getBook(id!)
  const form = useForm<AddValues>({
    resolver: zodResolver(addSchema),
    defaultValues: { name: '', description: '', servingSize: 2, prepTimeMinutes: 30 },
  })

  return (
    <div>
      <Breadcrumb className="mb-4">
        <BreadcrumbList>
          <BreadcrumbItem><BreadcrumbLink asChild><Link to="/recipe-books">{t('recipeBooks.title')}</Link></BreadcrumbLink></BreadcrumbItem>
          <BreadcrumbSeparator />
          <BreadcrumbItem>{book?.name ?? id}</BreadcrumbItem>
        </BreadcrumbList>
      </Breadcrumb>

      <div
        className="h-[120px] rounded-xl flex items-center justify-center mb-4"
        style={{ background: `linear-gradient(135deg, ${hashColor(id!)}, ${hashColor(id! + 'x')})` }}
      >
        <h1 className="text-3xl font-bold text-white drop-shadow">{book?.name}</h1>
      </div>

      <div className="flex items-center gap-2 mb-4">
        <Dialog open={addOpen} onOpenChange={setAddOpen}>
          <DialogTrigger asChild>
            <Button><Plus className="w-4 h-4 mr-1" />{t('recipes.addRecipe')}</Button>
          </DialogTrigger>
          <DialogContent>
            <DialogHeader><DialogTitle>{t('recipes.create.title')}</DialogTitle></DialogHeader>
            <form onSubmit={form.handleSubmit(async (v) => { await createRecipe(id!, v); setAddOpen(false); form.reset() })} className="space-y-3 mt-2">
              <Input placeholder={t('recipes.name')} {...form.register('name')} />
              <Textarea placeholder={t('recipes.description')} {...form.register('description')} />
              <div className="flex gap-3">
                <Input type="number" placeholder={t('recipes.servingSize')} {...form.register('servingSize', { valueAsNumber: true })} />
                <Input type="number" placeholder={t('recipes.prepTime')} {...form.register('prepTimeMinutes', { valueAsNumber: true })} />
              </div>
              <DialogFooter>
                <Button type="submit">{t('recipes.create.submit')}</Button>
              </DialogFooter>
            </form>
          </DialogContent>
        </Dialog>
      </div>

      <Separator className="mb-6" />

      {isLoading ? (
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 xl:grid-cols-4">
          {Array.from({ length: 4 }).map((_, i) => <Skeleton key={i} className="aspect-square rounded-3xl" />)}
        </div>
      ) : recipes.length === 0 ? (
        <div className="flex flex-col items-center gap-4 py-16">
          <span className="text-4xl">🍽️</span>
          <p className="text-muted-foreground">{t('recipes.title')} — no recipes yet</p>
          <Button onClick={() => setAddOpen(true)}>{t('recipes.addRecipe')}</Button>
        </div>
      ) : (
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 xl:grid-cols-4">
          {recipes.map((recipe) => (
            <EntityCard
              key={recipe.id}
              className="cursor-pointer"
              onClick={() => navigate(`/recipe-books/${id}/recipes/${recipe.id}`)}
              icon={
                <div
                  className="flex h-10 w-10 items-center justify-center rounded-2xl text-sm font-semibold text-white sm:h-11 sm:w-11"
                  style={{ background: `linear-gradient(135deg, ${hashColor(recipe.id)}, ${hashColor(recipe.name)})` }}
                >
                  {recipe.name.slice(0, 2).toUpperCase()}
                </div>
              }
              topRight={
                <AlertDialog>
                  <DropdownMenu>
                    <DropdownMenuTrigger asChild onClick={(e) => e.stopPropagation()}>
                      <Button variant="ghost" size="icon" className="-mr-2 -mt-2 h-9 w-9 rounded-2xl">
                        <MoreHorizontal className="h-4 w-4" />
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
                      <AlertDialogTitle>{t('recipes.delete.title')}</AlertDialogTitle>
                      <AlertDialogDescription>{t('recipes.delete.description', { name: recipe.name })}</AlertDialogDescription>
                    </AlertDialogHeader>
                    <AlertDialogFooter>
                      <AlertDialogCancel>{t('common.cancel')}</AlertDialogCancel>
                      <AlertDialogAction onClick={(e) => { e.stopPropagation(); deleteRecipe(id!, recipe.id) }}>{t('recipes.delete.confirm')}</AlertDialogAction>
                    </AlertDialogFooter>
                  </AlertDialogContent>
                </AlertDialog>
              }
              title={recipe.name}
              description={recipe.description}
              meta={
                <p className="mt-3 text-xs text-muted-foreground">
                  {recipe.servingSize} porzioni · {recipe.prepTimeMinutes} min
                </p>
              }
              footer={<span className="text-sm font-medium text-primary">{t('hub.open')}</span>}
            />
          ))}
        </div>
      )}
    </div>
  )
}

export function RecipeBookDetailPage() {
  const { id } = useParams<{ id: string }>()
  return (
    <RecipeBookProvider>
      <RecipeProvider bookId={id!}>
        <RecipeBookDetailContent />
      </RecipeProvider>
    </RecipeBookProvider>
  )
}
