import type { ReactNode } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../lib/auth'
import type { UserRole } from '../lib/types'

const navByRole: Record<UserRole, { to: string; label: string }[]> = {
  Employee: [{ to: '/mis-boletas', label: 'Mis boletas' }],
  Approver: [
    { to: '/mis-boletas', label: 'Mis boletas' },
    { to: '/aprobaciones', label: 'Aprobaciones' },
  ],
  Administrator: [
    { to: '/mis-boletas', label: 'Mis boletas' },
    { to: '/admin', label: 'Administración' },
  ],
  Finance: [
    { to: '/mis-boletas', label: 'Mis boletas' },
    { to: '/finanzas', label: 'Finanzas' },
  ],
}

export function Layout({ children, title }: { children: ReactNode; title: string }) {
  const { session, logout } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()

  const links = session ? navByRole[session.role] : []

  return (
    <div className="min-h-screen bg-neutral-50">
      <header className="bg-brand-green text-white">
        <div className="mx-auto max-w-4xl px-6 py-4">
          <div className="flex items-center justify-between">
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
          {links.length > 1 && (
            <nav className="mt-3 flex gap-2">
              {links.map((link) => (
                <Link
                  key={link.to}
                  to={link.to}
                  className={`rounded-md px-3 py-1.5 text-sm font-medium ${
                    location.pathname === link.to ? 'bg-white/20' : 'hover:bg-white/10'
                  }`}
                >
                  {link.label}
                </Link>
              ))}
            </nav>
          )}
        </div>
      </header>
      <main className="mx-auto max-w-4xl px-6 py-8">{children}</main>
    </div>
  )
}
