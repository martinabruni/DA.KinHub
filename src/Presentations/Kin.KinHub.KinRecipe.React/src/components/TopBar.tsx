import { useLocation } from 'react-router-dom'
import { ChefHat } from 'lucide-react'
import { useTranslation } from 'react-i18next'

const routeTitles: Record<string, string> = {
  '/': 'app.name',
  '/recipe-books': 'nav.recipeBooks',
  '/fridges': 'nav.fridges',
  '/shopping-lists': 'nav.shoppingLists',
  '/ai-assistant': 'nav.aiAssistant',
}

export function TopBar() {
  const { t } = useTranslation()
  const location = useLocation()

  const titleKey = Object.entries(routeTitles).find(([path]) =>
    path === '/' ? location.pathname === '/' : location.pathname.startsWith(path),
  )?.[1]

  return (
    <header className="sticky top-0 z-40 border-b bg-background/90 backdrop-blur supports-[backdrop-filter]:bg-background/80">
      <div className="mx-auto flex h-16 max-w-6xl items-center gap-3 px-4 pt-[max(env(safe-area-inset-top),0px)] sm:px-5 md:px-6 lg:px-8">
        <div className="flex h-10 w-10 items-center justify-center rounded-2xl bg-primary/10 text-primary">
          <ChefHat className="h-5 w-5" />
        </div>
        <div className="min-w-0">
          <p className="text-xs font-medium uppercase tracking-[0.16em] text-muted-foreground">
            KinRecipe
          </p>
          <h1 className="truncate text-base font-semibold sm:text-lg">
            {titleKey ? t(titleKey) : t('app.name')}
          </h1>
        </div>
      </div>
    </header>
  )
}
