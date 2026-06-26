import { useLocation } from 'react-router-dom'
import { Menu, Moon, Sun } from 'lucide-react'
import { useTheme } from 'next-themes'
import { useTranslation } from 'react-i18next'
import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import { Button } from '@/components/ui/button'
import { useAuthContext } from '@/store/authContext'
import { getInitials } from '@/lib/utils'

const routeTitles: Record<string, string> = {
  '/recipe-books': 'nav.recipeBooks',
  '/fridges': 'nav.fridges',
  '/shopping-lists': 'nav.shoppingLists',
  '/ai-assistant': 'nav.aiAssistant',
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
    <header className="lg:hidden sticky top-0 z-50 h-14 border-b bg-background/80 backdrop-blur flex items-center px-4 gap-3">
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
    </header>
  )
}
