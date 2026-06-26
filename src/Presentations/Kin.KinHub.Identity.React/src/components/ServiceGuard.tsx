import { Navigate, Outlet } from 'react-router-dom'
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
    return <Navigate to="/services" replace />
  }

  return <Outlet />
}
