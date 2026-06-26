import { Outlet } from 'react-router-dom'
import { TopBar } from './TopBar'
import { FloatingUserMenu } from './FloatingUserMenu'
import { ServicesProvider } from '@/features/family/ServicesProvider'
import { FamilyProvider } from '@/features/family/FamilyProvider'

export function Layout() {
  return (
    <FamilyProvider>
      <ServicesProvider>
        <div className="min-h-dvh bg-background">
          <TopBar />
          <main className="px-4 pb-28 pt-4 sm:px-5 md:px-6 md:pt-6 lg:px-8">
            <div className="mx-auto w-full max-w-6xl">
              <Outlet />
            </div>
          </main>
          <FloatingUserMenu />
        </div>
      </ServicesProvider>
    </FamilyProvider>
  )
}
