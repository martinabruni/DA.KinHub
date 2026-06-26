import { useLocation } from 'react-router-dom'
import { Menu, Moon, Sun } from 'lucide-react'
import { useTheme } from 'next-themes'
import { useTranslation } from 'react-i18next'
import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import { Button } from '@/components/ui/button'
import { useAuthContext } from '@/store/authContext'
import { getInitials } from '@/lib/utils'

const routeTitles: Record<string, string> = {
  '/services': 'nav.services',
  '/family': 'nav.family',
  '/recipe-books': 'nav.recipeBooks',
  '/fridges': 'nav.fridges',
  '/ai-assistant': 'nav.aiAssistant',
  '/profile': 'nav.profile',
  '/select-member': 'selectMember.title',
}

interface TopBarProps {
  onMenuClick: () => void
}

export function TopBar({ onMenuClick }: TopBarProps) {
  const { t } = useTranslation()
  const { theme, setTheme } = useTheme()
  const { activeMember } = useAuthContext()
  const location = useLocation()

  const titleKey = Object.entries(routeTitles).find(([path]) =>
    location.pathname.startsWith(path),
  )?.[1]

  return (
    <header className="lg:hidden sticky top-0 z-50 border-b bg-background/90 backdrop-blur supports-[backdrop-filter]:bg-background/80">
      <div className="flex h-14 items-center gap-3 px-4 pt-[max(env(safe-area-inset-top),0px)]">
      <Button variant="ghost" size="icon" onClick={onMenuClick}>
        <Menu className="w-5 h-5" />
      </Button>

      <span className="flex-1 text-center font-semibold text-sm">
        {titleKey ? t(titleKey) : t('app.name')}
      </span>

      <Button
        variant="ghost"
        size="icon"
        onClick={() => setTheme(theme === 'dark' ? 'light' : 'dark')}
      >
        {theme === 'dark' ? <Sun className="w-4 h-4" /> : <Moon className="w-4 h-4" />}
      </Button>

      <Avatar className="w-7 h-7">
        <AvatarFallback className="text-xs bg-primary/20 text-primary">
          {getInitials(activeMember?.name ?? 'U')}
        </AvatarFallback>
      </Avatar>
      </div>
    </header>
  )
}
