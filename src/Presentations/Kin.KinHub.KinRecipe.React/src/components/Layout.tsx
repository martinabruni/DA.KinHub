import { useState } from 'react'
import { Outlet } from 'react-router-dom'
import { Sidebar } from './Sidebar'
import { TopBar } from './TopBar'
import { ServicesProvider } from '@/features/family/ServicesProvider'
import { FamilyProvider } from '@/features/family/FamilyProvider'

export function Layout() {
  const [collapsed, setCollapsed] = useState(false)
  const [mobileOpen, setMobileOpen] = useState(false)

  return (
    <FamilyProvider>
    <ServicesProvider>
      <div className="min-h-screen bg-background flex">
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
          <main className="flex-1 p-4 md:p-6 lg:p-8">
            <div className="max-w-7xl mx-auto">
              <Outlet />
            </div>
          </main>
        </div>
      </div>
    </ServicesProvider>
    </FamilyProvider>
  )
}
