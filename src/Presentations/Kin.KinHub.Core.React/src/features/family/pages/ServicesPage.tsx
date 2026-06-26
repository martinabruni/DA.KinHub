import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { ArrowRight, Settings2 } from 'lucide-react'
import { Card, CardContent } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { apiClient } from '@/api/apiClient'
import { useAuth } from '@/features/auth/AuthProvider'
import { useServices } from '@/features/family/ServicesProvider'
import {
  serviceConfig,
  defaultServiceConfig,
  getServiceHref,
} from '@/config/serviceConfig'
import { useAuthContext } from '@/store/authContext'
import type { Family } from '@/types'

export function ServicesPage() {
  const { t } = useTranslation()
  const { user } = useAuth()
  const { services, isLoading } = useServices()
  const { activeMember } = useAuthContext()
  const enabledServices = services.filter((service) => service.isEnabled)

  const { data: family, isLoading: loadingFamily } = useQuery({
    queryKey: ['family'],
    queryFn: async () => {
      const { data } = await apiClient.get<Family>('/api/families')
      return data
    },
    enabled: !!user?.familyId,
    retry: false,
  })

  return (
    <div className="space-y-6">
      <section className="rounded-3xl border bg-card px-4 py-5 shadow-sm sm:px-6">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
          <div className="space-y-1">
            <p className="text-sm font-medium text-primary">
              {t('hub.greeting', {
                name: activeMember?.name ?? user?.email?.split('@')[0] ?? '',
              })}
            </p>
            <h1 className="text-2xl font-semibold tracking-tight sm:text-3xl">
              {t('services.title')}
            </h1>
            {loadingFamily ? (
              <Skeleton className="h-4 w-44" />
            ) : family ? (
              <p className="text-sm text-muted-foreground">
                {t('hub.familyBanner', { name: family.name })}
                {' · '}
                {t('hub.members', { count: family.members?.length ?? 0 })}
              </p>
            ) : (
              <p className="text-sm text-muted-foreground">{t('services.subtitle')}</p>
            )}
          </div>
          <Button asChild variant="outline" className="h-11 justify-between gap-2 sm:w-auto">
            <Link to="/console/services">
              <span>{t('hub.manageServices')}</span>
              <Settings2 className="h-4 w-4" />
            </Link>
          </Button>
        </div>
      </section>

      <section>
        <div className="mb-3 flex items-center justify-between gap-3">
          <h2 className="text-sm font-semibold uppercase tracking-[0.16em] text-muted-foreground">
            {t('hub.yourServices')}
          </h2>
        </div>

        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 xl:grid-cols-3">
        {isLoading
          ? Array.from({ length: 4 }).map((_, i) => (
              <Skeleton key={i} className="h-36 rounded-3xl" />
            ))
          : enabledServices.map((service) => {
              const cfg = serviceConfig[service.name] ?? defaultServiceConfig;
              const Icon = cfg.icon;
              const href = getServiceHref(service.name, activeMember);
              return (
                cfg.external ? (
                  <a key={service.id} href={href} className="block">
                    <Card className="h-full border-border/70 bg-card/80 transition-all hover:-translate-y-0.5 hover:border-primary/40 hover:shadow-md">
                      <CardContent className="flex h-full flex-col gap-4 p-5">
                        <div className="flex items-start justify-between gap-3">
                          <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-muted">
                          <Icon className={`w-6 h-6 ${cfg.color}`} />
                          </div>
                          <ArrowRight className="h-4 w-4 text-muted-foreground" />
                        </div>
                        <div className="flex-1">
                          <p className="font-semibold leading-tight">
                            {service.name}
                          </p>
                          <p className="mt-2 text-sm text-muted-foreground line-clamp-2">
                            {service.description}
                          </p>
                        </div>
                        <span className="text-sm font-medium text-primary">
                          {t('services.open')}
                        </span>
                      </CardContent>
                    </Card>
                  </a>
                ) : (
                <Link key={service.id} to={href} className="block">
                  <Card className="h-full border-border/70 bg-card/80 transition-all hover:-translate-y-0.5 hover:border-primary/40 hover:shadow-md">
                    <CardContent className="flex h-full flex-col gap-4 p-5">
                      <div className="flex items-start justify-between gap-3">
                        <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-muted">
                        <Icon className={`w-6 h-6 ${cfg.color}`} />
                        </div>
                        <ArrowRight className="h-4 w-4 text-muted-foreground" />
                      </div>
                      <div className="flex-1">
                        <p className="font-semibold leading-tight">
                          {service.name}
                        </p>
                        <p className="mt-2 text-sm text-muted-foreground line-clamp-2">
                          {service.description}
                        </p>
                      </div>
                      <span className="text-sm font-medium text-primary">
                        {t('services.open')}
                      </span>
                    </CardContent>
                  </Card>
                </Link>
                )
              );
            })}
        </div>
      </section>
    </div>
  )
}
