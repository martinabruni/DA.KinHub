import { NavLink } from 'react-router-dom'
import { Grid2x2, Home, User, Users } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { cn } from '@/lib/utils'

const items = [
  { to: '/dashboard', icon: Home, labelKey: 'nav.dashboard' },
  { to: '/family', icon: Users, labelKey: 'nav.family' },
  { to: '/services', icon: Grid2x2, labelKey: 'nav.services' },
  { to: '/profile', icon: User, labelKey: 'nav.profile' },
]

export function BottomNav() {
  const { t } = useTranslation()

  return (
    <nav className="lg:hidden fixed bottom-0 left-0 right-0 z-50 h-16 border-t bg-background/80 backdrop-blur flex items-center justify-around px-2">
      {items.map(({ to, icon: Icon, labelKey }) => (
        <NavLink
          key={to}
          to={to}
          className={({ isActive }) =>
            cn(
              'flex flex-col items-center gap-0.5 px-3 py-1 rounded-lg transition-colors',
              isActive ? 'text-primary' : 'text-muted-foreground hover:text-foreground',
            )
          }
        >
          {({ isActive }) => (
            <>
              <Icon className="w-5 h-5" />
              <span className="text-[10px]">{t(labelKey)}</span>
              {isActive && <span className="w-1 h-1 rounded-full bg-primary mt-0.5" />}
            </>
          )}
        </NavLink>
      ))}
    </nav>
  )
}
