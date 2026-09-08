import type { ReactNode } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../lib/auth'

export function Layout({ children, title }: { children: ReactNode; title: string }) {
  const { session, logout } = useAuth()
  const navigate = useNavigate()

  return (
    <div className="min-h-screen bg-neutral-50">
      <header className="bg-brand-green text-white">
        <div className="mx-auto flex max-w-4xl items-center justify-between px-6 py-4">
          <div>
            <p className="text-sm font-medium opacity-80">Boleta de Kilometraje Digital</p>
            <h1 className="text-xl font-semibold">{title}</h1>
          </div>
          {session && (
            <div className="flex items-center gap-4 text-sm">
              <span className="opacity-90">
                {session.email} · {session.role}
              </span>
              <button
                onClick={() => {
                  logout()
                  navigate('/login')
                }}
                className="rounded-md border border-white/40 px-3 py-1.5 hover:bg-white/10"
              >
                Salir
              </button>
            </div>
          )}
        </div>
      </header>
      <main className="mx-auto max-w-4xl px-6 py-8">{children}</main>
    </div>
  )
}
