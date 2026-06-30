import { useTranslation } from 'react-i18next'
import { Skeleton } from '@/components/ui/skeleton'
import { Badge } from '@/components/ui/badge'
import { Switch } from '@/components/ui/switch'
import { EntityCard } from '@/components/entity-card'
import {
  getServiceConfig,
  isServiceToggleable,
} from '@/config/serviceConfig'
import { useServices } from '@/features/family/ServicesProvider'

export function ServicesConsolePage() {
  const { t } = useTranslation()
  const { services, isLoading, toggleService } = useServices()

  return (
    <div>
      <h1 className="text-2xl font-bold">{t('console.services.title')}</h1>
      <p className="text-muted-foreground text-sm mt-1">{t('console.services.subtitle')}</p>

      <div className="mt-6 grid grid-cols-2 gap-3 sm:grid-cols-3 xl:grid-cols-4">
        {isLoading
          ? Array.from({ length: 4 }).map((_, i) => (
              <Skeleton key={i} className="aspect-square rounded-3xl" />
            ))
          : services.map((service) => {
              const config = getServiceConfig(service.name)
              const Icon = config.icon
              const canToggle = isServiceToggleable(service.name)
              return (
                <EntityCard
                  key={service.id}
                  icon={
                    <div className="flex h-10 w-10 items-center justify-center rounded-2xl bg-muted sm:h-11 sm:w-11">
                      <Icon className={`h-5 w-5 ${config.color}`} />
                    </div>
                  }
                  topRight={
                    canToggle ? (
                      <Switch
                        checked={service.isEnabled}
                        onCheckedChange={(checked) => toggleService(service.id, checked)}
                      />
                    ) : (
                      <Badge variant="secondary" className="shrink-0 text-xs">
                        {t('console.services.alwaysOn')}
                      </Badge>
                    )
                  }
                  title={service.name}
                  description={service.description}
                  meta={
                    <>
                      <Badge
                        variant={service.isEnabled ? 'default' : 'secondary'}
                        className="mt-3 text-xs"
                      >
                        {canToggle
                          ? service.isEnabled
                            ? t('services.active')
                            : t('services.inactive')
                          : t('console.services.alwaysOn')}
                      </Badge>
                      {!canToggle ? (
                        <p className="mt-2 text-xs text-muted-foreground">
                          {t('console.services.alwaysOnDescription')}
                        </p>
                      ) : null}
                    </>
                  }
                />
              )
            })}
      </div>
    </div>
  )
}
