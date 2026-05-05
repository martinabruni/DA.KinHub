import { NavLink, useNavigate } from 'react-router-dom'
import {
  ChevronLeft,
  ChevronRight,
  Grid2x2,
  Home,
  LogOut,
  Moon,
  Shield,
  Sun,
  SwitchCamera,
  User,
  Users,
} from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { useTheme } from 'next-themes'
import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import { Button } from '@/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { Separator } from '@/components/ui/separator'
import { Sheet, SheetContent } from '@/components/ui/sheet'
import { Badge } from '@/components/ui/badge'
import { useAuth } from '@/features/auth/AuthProvider'
import { useAuthContext } from '@/store/authContext'
import { getInitials } from '@/lib/utils'
import { cn } from '@/lib/utils'

interface SidebarProps {
  collapsed: boolean
  onCollapse: () => void
  mobileOpen: boolean
  onMobileClose: () => void
}

const navItems = [
  { to: '/dashboard', icon: Home, labelKey: 'nav.dashboard', badge: undefined },
  { to: '/family', icon: Users, labelKey: 'nav.family', badge: undefined },
  { to: '/services', icon: Grid2x2, labelKey: 'nav.services', badge: undefined },
]

function SidebarContent({ collapsed }: { collapsed: boolean }) {
  const { t, i18n } = useTranslation()
  const { theme, setTheme } = useTheme()
  const { logout } = useAuth()
  const { activeMember } = useAuthContext()
  const navigate = useNavigate()

  const handleLogout = async () => {
    await logout()
    navigate('/login')
  }

  return (
    <div className="flex flex-col h-full">
      <div className={cn('flex items-center gap-3 px-4 py-5', collapsed && 'justify-center px-2')}>
        <Home className="w-7 h-7 text-primary shrink-0" />
        {!collapsed && (
          <span className="text-xl font-bold tracking-tight">{t('app.name')}</span>
        )}
      </div>

      <Separator />

      <nav className="flex-1 px-2 py-4 space-y-1">
        {!collapsed && (
          <p className="text-muted-foreground text-xs font-medium uppercase tracking-wider px-2 mb-2">
            Menu
          </p>
        )}
        {navItems.map(({ to, icon: Icon, labelKey, badge }) => (
          <NavLink
            key={to}
            to={to}
            className={({ isActive }) =>
              cn(
                'flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium transition-colors',
                isActive
                  ? 'bg-primary/10 text-primary font-semibold'
                  : 'text-foreground hover:bg-muted',
                collapsed && 'justify-center px-2',
              )
            }
          >
            <Icon className="w-5 h-5 shrink-0" />
            {!collapsed && (
              <>
                <span className="flex-1">{t(labelKey)}</span>
                {badge && (
                  <Badge className="text-[10px] px-1.5 py-0 bg-accent text-accent-foreground">
                    {badge}
                  </Badge>
                )}
              </>
            )}
          </NavLink>
        ))}
      </nav>

      <Separator />

      <div className={cn('px-2 py-4 space-y-2', collapsed && 'flex flex-col items-center')}>
        <Button
          variant="ghost"
          size="icon"
          onClick={() => setTheme(theme === 'dark' ? 'light' : 'dark')}
          title={theme === 'dark' ? 'Light mode' : 'Dark mode'}
        >
          {theme === 'dark' ? <Sun className="w-4 h-4" /> : <Moon className="w-4 h-4" />}
        </Button>

        {!collapsed && (
          <Button
            variant="ghost"
            size="sm"
            onClick={() => i18n.changeLanguage(i18n.language === 'en' ? 'it' : 'en')}
            className="w-full justify-start text-xs font-medium"
          >
            {i18n.language === 'en' ? '🇮🇹 IT' : '🇬🇧 EN'}
          </Button>
        )}

        <Separator />

        {/* Active member + actions */}
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button
              variant="ghost"
              className={cn('w-full justify-start gap-3 px-2', collapsed && 'w-10 px-0 justify-center')}
            >
              <Avatar className="w-7 h-7">
                <AvatarFallback className="text-xs bg-primary/20 text-primary">
                  {getInitials(activeMember?.name ?? 'U')}
                </AvatarFallback>
              </Avatar>
              {!collapsed && activeMember && (
                <div className="flex flex-col items-start min-w-0">
                  <span className="text-xs font-medium truncate max-w-[110px]">{activeMember.name}</span>
                  <span className="flex items-center gap-1 text-[10px] text-muted-foreground">
                    {activeMember.role === 'Admin'
                      ? <><Shield className="w-2.5 h-2.5" /> Admin</>
                      : <><User className="w-2.5 h-2.5" /> Member</>
                    }
                  </span>
                </div>
              )}
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent side="top" align="start" className="w-48">
            <DropdownMenuItem onClick={() => navigate('/profile')}>
              <User className="w-4 h-4 mr-2" />
              {t('nav.profile')}
            </DropdownMenuItem>
            <DropdownMenuItem onClick={() => navigate('/select-member')}>
              <SwitchCamera className="w-4 h-4 mr-2" />
              {t('selectMember.switchMember')}
            </DropdownMenuItem>
            <DropdownMenuSeparator />
            <DropdownMenuItem onClick={handleLogout} className="text-destructive">
              <LogOut className="w-4 h-4 mr-2" />
              {t('nav.logout')}
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>
    </div>
  )
}

export function Sidebar({ collapsed, onCollapse, mobileOpen, onMobileClose }: SidebarProps) {
  return (
    <>
      {/* Desktop sidebar */}
      <aside
        className={cn(
          'hidden lg:flex flex-col fixed top-0 left-0 h-full border-r bg-card transition-all duration-200 z-30',
          collapsed ? 'w-16' : 'w-60',
        )}
      >
        <SidebarContent collapsed={collapsed} />
        <Button
          variant="ghost"
          size="icon"
          onClick={onCollapse}
          className="absolute -right-3 top-6 w-6 h-6 rounded-full border bg-background shadow-sm"
        >
          {collapsed ? <ChevronRight className="w-3 h-3" /> : <ChevronLeft className="w-3 h-3" />}
        </Button>
      </aside>

      {/* Mobile Sheet */}
      <Sheet open={mobileOpen} onOpenChange={onMobileClose}>
        <SheetContent side="left" className="w-64 p-0">
          <SidebarContent collapsed={false} />
        </SheetContent>
      </Sheet>
    </>
  )
}
