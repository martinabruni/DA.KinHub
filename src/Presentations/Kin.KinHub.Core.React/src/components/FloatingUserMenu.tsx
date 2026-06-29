import { ArrowRight, Globe, Grid2x2, LogOut, Moon, Sun, SwitchCamera, User, Users } from 'lucide-react'
import { useTheme } from 'next-themes'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import { Button } from '@/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { buildIdentityLoginUrl } from '@/config/appLinks'
import { useAuth } from '@/features/auth/AuthProvider'
import { useAuthContext } from '@/store/authContext'
import { getInitials } from '@/lib/utils'

function MenuLinkRow({
  icon: Icon,
  label,
  onClick,
  showArrow = false,
  destructive = false,
}: {
  icon: React.ElementType
  label: string
  onClick: () => void
  showArrow?: boolean
  destructive?: boolean
}) {
  return (
    <DropdownMenuItem
      onClick={onClick}
      className={`flex h-12 cursor-pointer items-center gap-3 rounded-2xl px-3 text-base ${
        destructive ? 'text-destructive focus:text-destructive' : ''
      }`}
    >
      <Icon className="h-4 w-4 shrink-0" />
      <span className="flex-1">{label}</span>
      {showArrow ? <ArrowRight className="h-4 w-4 shrink-0" /> : null}
    </DropdownMenuItem>
  )
}

export function FloatingUserMenu() {
  const navigate = useNavigate()
  const { t, i18n } = useTranslation()
  const { theme, setTheme } = useTheme()
  const { logout } = useAuth()
  const { activeMember } = useAuthContext()

  const handleLogout = async () => {
    await logout()
    window.location.assign(buildIdentityLoginUrl())
  }

  return (
    <div className="fixed bottom-4 right-4 z-50">
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button
            variant="secondary"
            className="h-14 rounded-full border border-border/70 bg-card px-3 shadow-lg shadow-black/5 backdrop-blur"
          >
            <Avatar className="h-8 w-8">
              <AvatarFallback className="bg-primary/15 text-xs font-semibold text-primary">
                {getInitials(activeMember?.name ?? 'U')}
              </AvatarFallback>
            </Avatar>
            <div className="ml-2 hidden min-w-0 text-left sm:block">
              <p className="truncate text-sm font-medium">
                {activeMember?.name ?? t('selectMember.switchMember')}
              </p>
              <p className="text-xs text-muted-foreground">{t('family.members')}</p>
            </div>
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent
          side="top"
          align="end"
          className="mb-3 w-[280px] rounded-[28px] border border-border/70 bg-card p-2 shadow-2xl"
        >
          <MenuLinkRow icon={Grid2x2} label={t('nav.services')} onClick={() => navigate('/services')} />
          <MenuLinkRow icon={Users} label={t('nav.family')} onClick={() => navigate('/family')} />
          <MenuLinkRow icon={User} label={t('nav.profile')} onClick={() => navigate('/profile')} />
          <MenuLinkRow
            icon={SwitchCamera}
            label={t('selectMember.switchMember')}
            onClick={() => navigate('/select-member')}
            showArrow
          />
          <DropdownMenuSeparator />
          <MenuLinkRow
            icon={theme === 'dark' ? Sun : Moon}
            label={theme === 'dark'
              ? i18n.language === 'it' ? 'Modalita chiara' : 'Light mode'
              : t('profile.preferences.darkMode')}
            onClick={() => setTheme(theme === 'dark' ? 'light' : 'dark')}
          />
          <MenuLinkRow
            icon={Globe}
            label={i18n.language === 'en' ? 'Italiano' : 'English'}
            onClick={() => i18n.changeLanguage(i18n.language === 'en' ? 'it' : 'en')}
          />
          <DropdownMenuSeparator />
          <MenuLinkRow icon={LogOut} label={t('nav.logout')} onClick={handleLogout} destructive />
        </DropdownMenuContent>
      </DropdownMenu>
    </div>
  )
}
