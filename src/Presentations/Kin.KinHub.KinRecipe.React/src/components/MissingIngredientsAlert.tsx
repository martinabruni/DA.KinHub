import { Check, CircleAlert } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import type { ShoppingList } from '@/types'

interface MissingIngredientsAlertProps {
  ingredients: string[]
  shoppingLists: ShoppingList[]
  selectedListId: string
  onSelectedListChange: (value: string) => void
  onAdd: () => void
  addState: 'idle' | 'loading' | 'added'
  className?: string
}

export function MissingIngredientsAlert({
  ingredients,
  shoppingLists,
  selectedListId,
  onSelectedListChange,
  onAdd,
  addState,
  className,
}: MissingIngredientsAlertProps) {
  const { t } = useTranslation()

  if (ingredients.length === 0) {
    return null
  }

  return (
    <div className={`rounded-3xl border border-destructive/25 bg-destructive/8 p-4 text-sm ${className ?? ''}`}>
      <div className="flex items-start gap-3">
        <div className="mt-0.5 flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-destructive/15 text-destructive">
          <CircleAlert className="h-4 w-4" />
        </div>
        <div className="min-w-0 flex-1">
          <p className="font-semibold text-destructive">{t('shoppingLists.missingAlertTitle')}</p>
          <p className="mt-1 text-muted-foreground">{t('shoppingLists.missingAlertDescription')}</p>
          <ul className="mt-3 list-disc space-y-1 pl-5 text-foreground">
            {ingredients.map((ingredient) => (
              <li key={ingredient} className="break-words">
                {ingredient}
              </li>
            ))}
          </ul>
          <div className="mt-4 flex flex-col gap-2 sm:flex-row">
            <Select value={selectedListId} onValueChange={onSelectedListChange}>
              <SelectTrigger className="w-full sm:flex-1">
                <SelectValue placeholder={t('shoppingLists.selectList')} />
              </SelectTrigger>
              <SelectContent>
                {shoppingLists.map((list) => (
                  <SelectItem key={list.id} value={list.id}>
                    {list.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            {addState === 'added' ? (
              <div className="flex h-10 min-w-[120px] items-center justify-center gap-2 rounded-xl border border-emerald-500/30 bg-emerald-500/10 px-4 text-sm font-medium text-emerald-700 dark:text-emerald-400">
                <Check className="h-4 w-4" />
                <span>{t('shoppingLists.added')}</span>
              </div>
            ) : (
              <Button
                onClick={onAdd}
                disabled={!selectedListId || addState === 'loading'}
                className="w-full sm:w-auto"
              >
                {addState === 'loading' ? t('shoppingLists.adding') : t('shoppingLists.addToList')}
              </Button>
            )}
          </div>
        </div>
      </div>
    </div>
  )
}
