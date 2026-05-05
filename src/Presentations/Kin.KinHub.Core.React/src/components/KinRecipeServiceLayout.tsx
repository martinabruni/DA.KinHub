import { Outlet } from 'react-router-dom'
import { NavLink } from 'react-router-dom'
import { BookOpen, Refrigerator, Sparkles } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Badge } from '@/components/ui/badge'
import { cn } from '@/lib/utils'

const serviceNavItems = [
  { to: '/recipe-books', icon: BookOpen, labelKey: 'nav.recipeBooks' },
  { to: '/fridges', icon: Refrigerator, labelKey: 'nav.fridges' },
  { to: '/ai-assistant', icon: Sparkles, labelKey: 'nav.aiAssistant', badge: 'AI' },
]

export function KinRecipeServiceLayout() {
  const { t } = useTranslation()

  return (
    <div>
      <nav className="flex items-center gap-1 mb-6 p-1 bg-muted/50 rounded-xl border overflow-x-auto shrink-0">
        {serviceNavItems.map(({ to, icon: Icon, labelKey, badge }) => (
          <NavLink
            key={to}
            to={to}
            className={({ isActive }) =>
              cn(
                'flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-medium transition-colors whitespace-nowrap',
                isActive
                  ? 'bg-background shadow-sm text-foreground'
                  : 'text-muted-foreground hover:text-foreground hover:bg-background/60',
              )
            }
          >
            <Icon className="w-4 h-4 shrink-0" />
            <span>{t(labelKey)}</span>
            {badge && (
              <Badge className="text-[10px] px-1.5 py-0 bg-accent text-accent-foreground">
                {badge}
              </Badge>
            )}
          </NavLink>
        ))}
      </nav>
      <Outlet />
    </div>
  )
}
