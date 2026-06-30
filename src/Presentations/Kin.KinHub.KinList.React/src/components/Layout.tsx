import { Outlet } from 'react-router-dom'
import { TopBar } from './TopBar'
import { FloatingUserMenu } from './FloatingUserMenu'

export function Layout() {
  return (
    <div className="min-h-dvh bg-background">
      <TopBar />
      <main className="px-4 pb-28 pt-4 sm:px-5 md:px-6 md:pt-6 lg:px-8">
        <div className="mx-auto w-full max-w-6xl">
          <Outlet />
        </div>
      </main>
      <FloatingUserMenu />
    </div>
  )
}
