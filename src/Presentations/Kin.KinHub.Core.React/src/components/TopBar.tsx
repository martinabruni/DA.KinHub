import { useLocation } from 'react-router-dom'
import { Home } from 'lucide-react'
import { useTranslation } from 'react-i18next'

const routeTitles: Record<string, string> = {
  '/services': 'nav.services',
  '/family': 'nav.family',
  '/profile': 'nav.profile',
  '/console/services': 'console.services.title',
}

export function TopBar() {
  const { t } = useTranslation()
  const location = useLocation()

  const titleKey = Object.entries(routeTitles).find(([path]) =>
    location.pathname.startsWith(path),
  )?.[1]

  return (
    <header className="sticky top-0 z-40 border-b bg-background/90 backdrop-blur supports-[backdrop-filter]:bg-background/80">
      <div className="mx-auto flex h-16 max-w-7xl items-center gap-3 px-4 pt-[max(env(safe-area-inset-top),0px)] sm:px-5 md:px-6 lg:px-8">
        <div className="flex h-10 w-10 items-center justify-center rounded-2xl bg-primary/10 text-primary">
          <Home className="h-5 w-5" />
        </div>
        <div className="min-w-0">
          <p className="text-xs font-medium uppercase tracking-[0.16em] text-muted-foreground">
            KinHub
          </p>
          <h1 className="truncate text-base font-semibold sm:text-lg">
            {titleKey ? t(titleKey) : t('app.name')}
          </h1>
        </div>
      </div>
    </header>
  )
}
