import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import {
  BookOpen,
  Refrigerator,
  ShoppingCart,
  Sparkles,
} from 'lucide-react'
import { Card, CardContent } from '@/components/ui/card'

const sections = [
  {
    to: '/recipe-books',
    icon: BookOpen,
    titleKey: 'nav.recipeBooks',
    descriptionKey: 'recipeHub.sections.recipeBooks',
    colorClass: 'text-orange-500',
  },
  {
    to: '/fridges',
    icon: Refrigerator,
    titleKey: 'nav.fridges',
    descriptionKey: 'recipeHub.sections.fridges',
    colorClass: 'text-sky-500',
  },
  {
    to: '/shopping-lists',
    icon: ShoppingCart,
    titleKey: 'nav.shoppingLists',
    descriptionKey: 'recipeHub.sections.shoppingLists',
    colorClass: 'text-emerald-500',
  },
  {
    to: '/ai-assistant',
    icon: Sparkles,
    titleKey: 'nav.aiAssistant',
    descriptionKey: 'recipeHub.sections.aiAssistant',
    colorClass: 'text-rose-500',
  },
]

export function DashboardPage() {
  const { t } = useTranslation()

  return (
    <div className="space-y-5">
      <section className="space-y-1">
        <h1 className="text-2xl font-semibold tracking-tight sm:text-3xl">
          {t('recipeHub.title')}
        </h1>
        <p className="max-w-2xl text-sm text-muted-foreground sm:text-base">
          {t('recipeHub.subtitle')}
        </p>
      </section>

      <section>
        <div className="mb-3 flex items-center justify-between gap-3">
          <h2 className="text-sm font-semibold uppercase tracking-[0.16em] text-muted-foreground">
            {t('recipeHub.sectionsTitle')}
          </h2>
        </div>
        <div className="grid grid-cols-2 gap-3">
          {sections.map(({ to, icon: Icon, titleKey, descriptionKey, colorClass }) => (
            <Link key={to} to={to} className="block">
              <Card className="h-full border-border/70 bg-card/80 transition-all hover:-translate-y-0.5 hover:border-primary/40 hover:shadow-md">
                <CardContent className="flex aspect-square h-full flex-col p-4 sm:p-5">
                  <div className={`flex h-10 w-10 items-center justify-center rounded-2xl bg-muted sm:h-11 sm:w-11 ${colorClass}`}>
                    <Icon className="h-5 w-5" />
                  </div>
                  <div className="mt-4 flex-1">
                    <p className="font-semibold leading-tight">{t(titleKey)}</p>
                    <p className="mt-2 text-sm text-muted-foreground line-clamp-3">{t(descriptionKey)}</p>
                  </div>
                  <span className="mt-4 text-sm font-medium text-primary">{t('hub.open')}</span>
                </CardContent>
              </Card>
            </Link>
          ))}
        </div>
      </section>
    </div>
  )
}

