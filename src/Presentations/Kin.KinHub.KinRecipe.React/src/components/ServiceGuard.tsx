import { useEffect } from 'react'
import { Outlet } from 'react-router-dom'
import { buildCoreServicesUrl } from '@/config/appLinks'
import { useServices } from '@/features/family/ServicesProvider'

interface ServiceGuardProps {
  serviceName: string
}

export function ServiceGuard({ serviceName }: ServiceGuardProps) {
  const { services, isLoading } = useServices()

  if (isLoading) return null

  const service = services.find(
    (s) => s.name.toLowerCase() === serviceName.toLowerCase(),
  ) 

  if (!service?.isEnabled) {
    return <ServiceUnavailableRedirect targetUrl={buildCoreServicesUrl()} />
  }

  return <Outlet />
}

function ServiceUnavailableRedirect({ targetUrl }: { targetUrl: string }) {
  useEffect(() => {
    window.location.assign(targetUrl)
  }, [targetUrl])

  return null
}
