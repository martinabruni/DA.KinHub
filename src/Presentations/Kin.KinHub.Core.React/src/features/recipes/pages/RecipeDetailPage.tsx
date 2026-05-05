import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Plus, Trash2 } from 'lucide-react'
import { useForm } from 'react-hook-form'
import { useQuery } from '@tanstack/react-query'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Skeleton } from '@/components/ui/skeleton'
import { Badge } from '@/components/ui/badge'
import { Breadcrumb, BreadcrumbItem, BreadcrumbLink, BreadcrumbList, BreadcrumbSeparator } from '@/components/ui/breadcrumb'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent, AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle, AlertDialogTrigger } from '@/components/ui/alert-dialog'
import { Separator } from '@/components/ui/separator'
import { RecipeBookProvider, useRecipeBooks } from '@/features/recipes/RecipeBookProvider'
import { RecipeProvider, useRecipes } from '@/features/recipes/RecipeProvider'
import { apiClient } from '@/api/apiClient'
import { hashColor } from '@/lib/utils'
import type { Fridge } from '@/types'

function RecipeDetailContent() {
  const { t } = useTranslation()
  const { id, recipeId } = useParams<{ id: string; recipeId: string }>()
  const { getBook } = useRecipeBooks()
  const { getRecipe, isLoading, addIngredient, deleteIngredient, addStep, deleteStep } = useRecipes()

  const [showIngredientForm, setShowIngredientForm] = useState(false)
  const [showStepForm, setShowStepForm] = useState(false)
  const [selectedFridgeId, setSelectedFridgeId] = useState<string>('')
  const [missingIngredients, setMissingIngredients] = useState<string[] | null>(null)

  const ingForm = useForm<{ name: string; quantity: string; unit: string }>({ defaultValues: { name: '', quantity: '', unit: '' } })
  const stepForm = useForm<{ description: string }>({ defaultValues: { description: '' } })

  const { data: fridges = [] } = useQuery({
    queryKey: ['fridges'],
    queryFn: async () => { const { data } = await apiClient.get<Fridge[]>('/api/fridges'); return data },
  })

  const recipe = getRecipe(recipeId!)
  const book = getBook(id!)

  const checkMissing = async () => {
    if (!selectedFridgeId || !recipe) return
    try {
      const { data } = await apiClient.post(`/api/recipe-books/${id}/recipes/${recipeId}/missing-ingredients?fridgeId=${selectedFridgeId}`)
      setMissingIngredients(data.missingIngredients ?? [])
    } catch {
      setMissingIngredients([])
    }
  }

  if (isLoading) return <Skeleton className="h-96 w-full rounded-xl" />
  if (!recipe) return <p className="text-muted-foreground">{t('common.noData')}</p>

  return (
    <div>
      <Breadcrumb className="mb-4">
        <BreadcrumbList>
          <BreadcrumbItem><BreadcrumbLink asChild><Link to="/recipe-books">{t('recipeBooks.title')}</Link></BreadcrumbLink></BreadcrumbItem>
          <BreadcrumbSeparator />
          <BreadcrumbItem><BreadcrumbLink asChild><Link to={`/recipe-books/${id}`}>{book?.name}</Link></BreadcrumbLink></BreadcrumbItem>
          <BreadcrumbSeparator />
          <BreadcrumbItem>{recipe.name}</BreadcrumbItem>
        </BreadcrumbList>
      </Breadcrumb>

      <div
        className="h-[160px] rounded-xl flex items-center justify-center mb-4"
        style={{ background: `linear-gradient(135deg, ${hashColor(recipe.id)}, ${hashColor(recipe.name)})` }}
      >
        <h1 className="text-4xl font-bold text-white drop-shadow text-center px-4">{recipe.name}</h1>
      </div>

      <div className="flex items-center gap-3 mb-6 flex-wrap">
        <Badge variant="secondary">👥 {recipe.servingSize}</Badge>
        <Badge variant="secondary">⏱ {recipe.prepTimeMinutes}min</Badge>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-5 gap-8">
        {/* Ingredients (col-span-3) */}
        <div className="lg:col-span-3">
          <div className="flex items-center justify-between mb-3">
            <h2 className="text-lg font-semibold">{t('recipes.ingredients')}</h2>
            <Button size="sm" variant="outline" onClick={() => setShowIngredientForm((v) => !v)}>
              <Plus className="w-4 h-4 mr-1" />{t('recipes.addIngredient')}
            </Button>
          </div>

          {showIngredientForm && (
            <Card className="mb-3">
              <CardContent className="pt-3 pb-3">
                <form
                  onSubmit={ingForm.handleSubmit(async (v) => {
                    await addIngredient(id!, recipeId!, { name: v.name, quantity: Number(v.quantity), unit: v.unit })
                    ingForm.reset()
                    setShowIngredientForm(false)
                  })}
                  className="flex gap-2 flex-wrap"
                >
                  <Input placeholder={t('recipes.ingredientName')} {...ingForm.register('name')} className="flex-1 min-w-32" />
                  <Input type="number" placeholder={t('recipes.quantity')} {...ingForm.register('quantity')} className="w-20" />
                  <Input placeholder={t('recipes.unit')} {...ingForm.register('unit')} className="w-20" />
                  <Button type="submit" size="sm">{t('recipes.saveIngredient')}</Button>
                  <Button type="button" size="sm" variant="ghost" onClick={() => setShowIngredientForm(false)}>{t('recipes.cancel')}</Button>
                </form>
              </CardContent>
            </Card>
          )}

          <div className="space-y-2">
            {recipe.ingredients.map((ing) => (
              <div key={ing.id} className="flex items-center gap-2 p-2 rounded-lg border">
                <span className="flex-1 text-sm">{ing.name}</span>
                <span className="text-muted-foreground text-xs">{ing.quantity} {ing.unit}</span>
                <AlertDialog>
                  <AlertDialogTrigger asChild>
                    <Button variant="ghost" size="icon" className="h-7 w-7">
                      <Trash2 className="w-3 h-3 text-destructive" />
                    </Button>
                  </AlertDialogTrigger>
                  <AlertDialogContent>
                    <AlertDialogHeader>
                      <AlertDialogTitle>{t('common.delete')}</AlertDialogTitle>
                      <AlertDialogDescription>Remove {ing.name}?</AlertDialogDescription>
                    </AlertDialogHeader>
                    <AlertDialogFooter>
                      <AlertDialogCancel>{t('common.cancel')}</AlertDialogCancel>
                      <AlertDialogAction onClick={() => deleteIngredient(id!, recipeId!, ing.id)}>{t('common.delete')}</AlertDialogAction>
                    </AlertDialogFooter>
                  </AlertDialogContent>
                </AlertDialog>
              </div>
            ))}
          </div>
        </div>

        {/* Steps (col-span-2) */}
        <div className="lg:col-span-2">
          <div className="flex items-center justify-between mb-3">
            <h2 className="text-lg font-semibold">{t('recipes.steps')}</h2>
            <Button size="sm" variant="outline" onClick={() => setShowStepForm((v) => !v)}>
              <Plus className="w-4 h-4 mr-1" />{t('recipes.addStep')}
            </Button>
          </div>

          {showStepForm && (
            <Card className="mb-3">
              <CardContent className="pt-3 pb-3">
                <form
                  onSubmit={stepForm.handleSubmit(async (v) => {
                    await addStep(id!, recipeId!, { description: v.description, order: recipe.steps.length + 1 })
                    stepForm.reset()
                    setShowStepForm(false)
                  })}
                  className="space-y-2"
                >
                  <Input placeholder={t('recipes.stepDescription')} {...stepForm.register('description')} />
                  <div className="flex gap-2">
                    <Button type="submit" size="sm">{t('recipes.saveStep')}</Button>
                    <Button type="button" size="sm" variant="ghost" onClick={() => setShowStepForm(false)}>{t('recipes.cancel')}</Button>
                  </div>
                </form>
              </CardContent>
            </Card>
          )}

          <div className="space-y-2">
            {recipe.steps
              .slice()
              .sort((a, b) => a.order - b.order)
              .map((step) => (
                <div key={step.id} className="flex items-start gap-2 p-3 bg-card border rounded-lg">
                  <span className="w-6 h-6 rounded-full bg-primary text-primary-foreground text-xs flex items-center justify-center shrink-0 font-bold">
                    {step.order}
                  </span>
                  <p className="flex-1 text-sm">{step.description}</p>
                  <Button variant="ghost" size="icon" className="h-7 w-7" onClick={() => deleteStep(id!, recipeId!, step.id)}>
                    <Trash2 className="w-3 h-3 text-destructive" />
                  </Button>
                </div>
              ))}
          </div>
        </div>
      </div>

      {/* Missing Ingredients Check Bar */}
      <Separator className="my-6" />
      <Card className="lg:sticky lg:bottom-4">
        <CardContent className="flex flex-wrap items-center gap-3 py-4">
          <Select value={selectedFridgeId} onValueChange={setSelectedFridgeId}>
            <SelectTrigger className="w-48">
              <SelectValue placeholder={t('recipes.selectFridge')} />
            </SelectTrigger>
            <SelectContent>
              {fridges.map((f) => <SelectItem key={f.id} value={f.id}>{f.name}</SelectItem>)}
            </SelectContent>
          </Select>
          <Button onClick={checkMissing} disabled={!selectedFridgeId}>
            {t('recipes.checkMissing')}
          </Button>
          {missingIngredients !== null && (
            missingIngredients.length === 0
              ? <Badge className="bg-green-500 text-white">{t('recipes.allAvailable')}</Badge>
              : missingIngredients.map((name) => (
                  <Badge key={name} variant="destructive">{t('recipes.missing')}: {name}</Badge>
                ))
          )}
        </CardContent>
      </Card>
    </div>
  )
}

export function RecipeDetailPage() {
  const { id } = useParams<{ id: string }>()
  return (
    <RecipeBookProvider>
      <RecipeProvider bookId={id!}>
        <RecipeDetailContent />
      </RecipeProvider>
    </RecipeBookProvider>
  )
}
