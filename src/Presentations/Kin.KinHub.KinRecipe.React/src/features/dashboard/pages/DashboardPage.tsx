import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import {
  ArrowRight,
  BookOpen,
  Refrigerator,
  ShoppingCart,
  Sparkles,
} from 'lucide-react'
import { Card, CardContent } from '@/components/ui/card'
import { useAuth } from '@/features/auth/AuthProvider'
import { useAuthContext } from '@/store/authContext'

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
  const { user } = useAuth()
  const { activeMember } = useAuthContext()

  return (
    <div className="space-y-6">
      <section className="rounded-3xl border bg-card px-4 py-5 shadow-sm sm:px-6">
        <div className="space-y-2">
          <p className="text-sm font-medium text-primary">
          {t('hub.greeting', { name: activeMember?.name ?? user?.email?.split('@')[0] ?? '' })}
          </p>
          <h2 className="text-2xl font-semibold tracking-tight sm:text-3xl">
            {t('recipeHub.title')}
          </h2>
          <p className="max-w-2xl text-sm text-muted-foreground sm:text-base">
            {t('recipeHub.subtitle')}
          </p>
        </div>
      </section>

      <section>
        <div className="mb-3 flex items-center justify-between gap-3">
          <h3 className="text-sm font-semibold uppercase tracking-[0.16em] text-muted-foreground">
            {t('recipeHub.sectionsTitle')}
          </h3>
        </div>
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          {sections.map(({ to, icon: Icon, titleKey, descriptionKey, colorClass }) => (
            <Link key={to} to={to} className="block">
              <Card className="h-full border-border/70 bg-card/80 transition-all hover:-translate-y-0.5 hover:border-primary/40 hover:shadow-md">
                <CardContent className="flex h-full flex-col gap-4 p-5">
                  <div className="flex items-start justify-between gap-3">
                    <div className={`flex h-12 w-12 items-center justify-center rounded-2xl bg-muted ${colorClass}`}>
                      <Icon className="h-6 w-6" />
                    </div>
                    <ArrowRight className="h-4 w-4 text-muted-foreground" />
                  </div>
                  <div className="flex-1">
                    <p className="font-semibold leading-tight">{t(titleKey)}</p>
                    <p className="mt-2 text-sm text-muted-foreground">{t(descriptionKey)}</p>
                  </div>
                  <span className="text-sm font-medium text-primary">{t('hub.open')}</span>
                </CardContent>
              </Card>
            </Link>
          ))}
        </div>
      </section>
    </div>
  )
}

