import { useTranslation } from 'react-i18next'
import { BookOpen, Grid2x2, Refrigerator, Sparkles, Users } from 'lucide-react'
import { Card, CardContent } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { Badge } from '@/components/ui/badge'
import { Switch } from '@/components/ui/switch'
import { useServices } from '@/features/family/ServicesProvider'

const serviceIcons: Record<string, React.ElementType> = {
  Recipes: BookOpen,
  Fridges: Refrigerator,
  'AI Assistant': Sparkles,
  Family: Users,
  Services: Grid2x2,
}

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
              const Icon = serviceIcons[service.name] ?? Grid2x2
              return (
                <Card key={service.id} className="p-6">
                  <CardContent className="p-0">
                    <div className="flex items-start justify-between mb-3">
                      <Icon className="w-7 h-7 text-primary" />
                      <Switch
                        checked={service.isEnabled}
                        onCheckedChange={(checked) => toggleService(service.id, checked)}
                      />
                    </div>
                    <p className="font-semibold">{service.name}</p>
                    <p className="text-muted-foreground text-sm mt-1">{service.description}</p>
                    <Badge
                      variant={service.isEnabled ? 'default' : 'secondary'}
                      className="mt-3 text-xs"
                    >
                      {service.isEnabled ? t('services.active') : t('services.inactive')}
                    </Badge>
                  </CardContent>
                </Card>
              )
            })}
      </div>
    </div>
  )
}
