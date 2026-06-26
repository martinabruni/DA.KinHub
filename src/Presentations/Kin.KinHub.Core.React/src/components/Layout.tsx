import { useState } from 'react'
import { Outlet } from 'react-router-dom'
import { Sidebar } from './Sidebar'
import { TopBar } from './TopBar'
import { BottomNav } from './BottomNav'
import { ServicesProvider } from '@/features/family/ServicesProvider'
import { FamilyProvider } from '@/features/family/FamilyProvider'

export function Layout() {
  const [collapsed, setCollapsed] = useState(false)
  const [mobileOpen, setMobileOpen] = useState(false)

  return (
    <FamilyProvider>
      <ServicesProvider>
        <div className="min-h-dvh bg-background flex">
          <Sidebar
            collapsed={collapsed}
            onCollapse={() => setCollapsed((c) => !c)}
            mobileOpen={mobileOpen}
            onMobileClose={() => setMobileOpen(false)}
          />
          <div
            className={`flex flex-col flex-1 transition-all duration-200 ${collapsed ? 'lg:ml-16' : 'lg:ml-60'}`}
          >
            <TopBar onMenuClick={() => setMobileOpen(true)} />
            <main className="flex-1 px-4 pb-24 pt-4 sm:px-5 md:px-6 md:pt-6 lg:px-8 lg:pb-8">
              <div className="mx-auto w-full max-w-7xl">
                <Outlet />
              </div>
            </main>
          </div>
          <BottomNav />
        </div>
      </ServicesProvider>
    </FamilyProvider>
  )
}
