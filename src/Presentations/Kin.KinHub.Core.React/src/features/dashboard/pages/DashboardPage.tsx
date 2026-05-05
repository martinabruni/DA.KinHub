import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import {
  BookOpen,
  Grid2x2,
  Refrigerator,
  Settings2,
  Sparkles,
  Users,
} from 'lucide-react'
import { useQuery } from '@tanstack/react-query'
import { Card, CardContent } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { useAuth } from '@/features/auth/AuthProvider'
import { useAuthContext } from '@/store/authContext'
import { apiClient } from '@/api/apiClient'
import type { Family, Service } from '@/types'

const serviceConfig: Record<string, { icon: React.ElementType; path: string; color: string }> = {
  Recipes: { icon: BookOpen, path: '/recipe-books', color: 'text-orange-500' },
  Fridges: { icon: Refrigerator, path: '/fridges', color: 'text-blue-500' },
  'AI Assistant': { icon: Sparkles, path: '/ai-assistant', color: 'text-violet-500' },
  Family: { icon: Users, path: '/family', color: 'text-green-500' },
  Services: { icon: Grid2x2, path: '/services', color: 'text-slate-500' },
}

export function DashboardPage() {
  const { t } = useTranslation()
  const { user } = useAuth()
  const { activeMember } = useAuthContext()

  const { data: family, isLoading: loadingFamily } = useQuery({
    queryKey: ['family'],
    queryFn: async () => {
      const { data } = await apiClient.get<Family>('/api/families')
      return data
    },
    enabled: !!user?.familyId,
    retry: false,
  })

  const { data: services, isLoading: loadingServices } = useQuery({
    queryKey: ['services', 'family', user?.familyId],
    queryFn: async () => {
      const { data } = await apiClient.get<Service[]>(`/api/services/family/${user!.familyId}`)
      return data
    },
    enabled: !!user?.familyId,
  })

  const enabledServices = (services ?? []).filter((s) => s.isEnabled)

  return (
    <div className="space-y-8">
      {/* Header */}
      <div>
        <h1 className="text-3xl font-bold tracking-tight">
          {t('hub.greeting', { name: activeMember?.name ?? user?.email?.split('@')[0] ?? '' })}
        </h1>
        {loadingFamily ? (
          <Skeleton className="h-4 w-48 mt-2" />
        ) : family ? (
          <p className="text-muted-foreground mt-1">
            {t('hub.familyBanner', { name: family.name })}
            {' · '}
            {t('hub.members', { count: family.members?.length ?? 0 })}
          </p>
        ) : null}
      </div>

      {/* Services hub grid */}
      <section>
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-semibold">{t('hub.yourServices')}</h2>
          {activeMember?.role === 'Admin' && (
            <Button asChild variant="ghost" size="sm" className="text-muted-foreground gap-1">
              <Link to="/services">
                <Settings2 className="w-4 h-4" />
                {t('hub.manageServices')}
              </Link>
            </Button>
          )}
        </div>

        {loadingServices ? (
          <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-4">
            {Array.from({ length: 4 }).map((_, i) => (
              <Skeleton key={i} className="h-40 rounded-2xl" />
            ))}
          </div>
        ) : enabledServices.length === 0 ? (
          <Card className="bg-muted/40">
            <CardContent className="flex flex-col items-center gap-3 py-10">
              <Grid2x2 className="w-10 h-10 text-muted-foreground" />
              <p className="font-medium text-muted-foreground">{t('hub.noServicesTitle')}</p>
              <p className="text-sm text-muted-foreground text-center max-w-xs">
                {t('hub.noServicesDescription')}
              </p>
              {activeMember?.role === 'Admin' && (
                <Button asChild size="sm" variant="outline">
                  <Link to="/services">{t('hub.manageServices')}</Link>
                </Button>
              )}
            </CardContent>
          </Card>
        ) : (
          <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-4">
            {enabledServices.map((service) => {
              const cfg = serviceConfig[service.name] ?? {
                icon: Grid2x2,
                path: '/services',
                color: 'text-slate-500',
              }
              const Icon = cfg.icon
              return (
                <Link key={service.id} to={cfg.path}>
                  <Card className="h-full hover:shadow-lg hover:border-primary/40 transition-all cursor-pointer group">
                    <CardContent className="flex flex-col gap-3 p-5 h-full">
                      <div className="w-12 h-12 rounded-xl bg-muted flex items-center justify-center group-hover:bg-primary/10 transition-colors">
                        <Icon className={`w-6 h-6 ${cfg.color}`} />
                      </div>
                      <div className="flex-1">
                        <p className="font-semibold leading-tight">{service.name}</p>
                        <p className="text-muted-foreground text-xs mt-1 line-clamp-2">
                          {service.description}
                        </p>
                      </div>
                      <span className="text-xs font-medium text-primary">{t('hub.open')} →</span>
                    </CardContent>
                  </Card>
                </Link>
              )
            })}
          </div>
        )}
      </section>
    </div>
  )
}

