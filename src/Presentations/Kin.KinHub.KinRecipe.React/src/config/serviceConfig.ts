import {
  BookOpen,
  Grid2x2,
  Refrigerator,
  Sparkles,
  Terminal,
  Users,
} from 'lucide-react'
import type { Service } from '@/types'

export interface ServiceConfig {
  icon: React.ElementType
  path: string
  color: string
  canToggle?: boolean
}

export const serviceConfig: Record<string, ServiceConfig> = {
  KinConsole: {
    icon: Terminal,
    path: '/console/services',
    color: 'text-slate-500',
    canToggle: false,
  },
  KinRecipe: { icon: BookOpen, path: '/recipe-books', color: 'text-orange-500' },
  Recipes: { icon: BookOpen, path: '/recipe-books', color: 'text-orange-500' },
  Fridges: { icon: Refrigerator, path: '/fridges', color: 'text-blue-500' },
  'AI Assistant': { icon: Sparkles, path: '/ai-assistant', color: 'text-violet-500' },
  Family: { icon: Users, path: '/family', color: 'text-green-500' },
  Services: { icon: Grid2x2, path: '/services', color: 'text-slate-500' },
}

export const defaultServiceConfig: ServiceConfig = {
  icon: Grid2x2,
  path: '/services',
  color: 'text-slate-500',
  canToggle: true,
}

export function getServiceConfig(serviceName: string): ServiceConfig {
  return serviceConfig[serviceName] ?? defaultServiceConfig
}

export function isServiceToggleable(serviceName: string): boolean {
  return getServiceConfig(serviceName).canToggle ?? true
}

export function normalizeServiceState<T extends Service>(service: T): T {
  if (isServiceToggleable(service.name)) {
    return service
  }

  return {
    ...service,
    isEnabled: true,
  }
}
