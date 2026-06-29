import { useLocation, useNavigate } from 'react-router-dom'
import { ArrowLeft, ChefHat } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'

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
  const navigate = useNavigate()
  const fallbackPath = '/'
  const historyIndex = window.history.state?.idx ?? 0
  const canGoBack = historyIndex > 0

  const handleBack = () => {
    if (canGoBack) {
      navigate(-1)
      return
    }

    navigate(fallbackPath, { replace: true })
  }

  const titleKey = Object.entries(routeTitles).find(([path]) =>
    path === '/' ? location.pathname === '/' : location.pathname.startsWith(path),
  )?.[1]

  return (
    <header className="sticky top-0 z-40 border-b bg-background/90 backdrop-blur supports-[backdrop-filter]:bg-background/80">
      <div className="mx-auto flex h-16 max-w-6xl items-center gap-3 px-4 pt-[max(env(safe-area-inset-top),0px)] sm:px-5 md:px-6 lg:px-8">
        {canGoBack ? (
          <Button
            variant="ghost"
            size="icon"
            className="h-10 w-10 rounded-2xl"
            onClick={handleBack}
            aria-label={t('common.back')}
          >
            <ArrowLeft className="h-5 w-5" />
          </Button>
        ) : (
          <Button
            variant="ghost"
            size="icon"
            className="h-10 w-10 rounded-2xl bg-primary/10 text-primary hover:bg-primary/15"
            onClick={() => navigate(fallbackPath, { replace: location.pathname === fallbackPath })}
            aria-label={t('app.name')}
          >
            <ChefHat className="h-5 w-5" />
          </Button>
        )}
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
