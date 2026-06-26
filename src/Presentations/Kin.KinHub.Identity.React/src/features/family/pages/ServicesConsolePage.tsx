import { useTranslation } from 'react-i18next'
import { Card, CardContent } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { Badge } from '@/components/ui/badge'
import { Switch } from '@/components/ui/switch'
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

      <div className="mt-6 grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
        {isLoading
          ? Array.from({ length: 4 }).map((_, i) => <Skeleton key={i} className="h-36 rounded-xl" />)
          : services.map((service) => {
              const config = getServiceConfig(service.name)
              const Icon = config.icon
              const canToggle = isServiceToggleable(service.name)
              return (
                <Card key={service.id} className="p-6">
                  <CardContent className="p-0">
                    <div className="mb-3 flex items-start justify-between gap-3">
                      <Icon className={`w-7 h-7 shrink-0 ${config.color}`} />
                      {canToggle ? (
                        <Switch
                          checked={service.isEnabled}
                          onCheckedChange={(checked) => toggleService(service.id, checked)}
                        />
                      ) : (
                        <Badge variant="secondary" className="shrink-0 text-xs">
                          {t('console.services.alwaysOn')}
                        </Badge>
                      )}
                    </div>
                    <p className="font-semibold">{service.name}</p>
                    <p className="text-muted-foreground text-sm mt-1">{service.description}</p>
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
                    {!canToggle && (
                      <p className="text-muted-foreground mt-2 text-xs">
                        {t('console.services.alwaysOnDescription')}
                      </p>
                    )}
                  </CardContent>
                </Card>
              )
            })}
      </div>
    </div>
  )
}
