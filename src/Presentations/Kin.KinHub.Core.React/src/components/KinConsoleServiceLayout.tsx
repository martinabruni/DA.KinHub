import { Outlet } from 'react-router-dom'
import { NavLink } from 'react-router-dom'
import { Grid2x2 } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { cn } from '@/lib/utils'

const consoleNavItems = [
  { to: '/console/services', icon: Grid2x2, labelKey: 'nav.services' },
]

export function KinConsoleServiceLayout() {
  const { t } = useTranslation()

  return (
    <div>
      <nav className="flex items-center gap-1 mb-6 p-1 bg-muted/50 rounded-xl border overflow-x-auto shrink-0">
        {consoleNavItems.map(({ to, icon: Icon, labelKey }) => (
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
          </NavLink>
        ))}
      </nav>
      <Outlet />
    </div>
  )
}
