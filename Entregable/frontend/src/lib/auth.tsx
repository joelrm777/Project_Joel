import { createContext, useContext, useMemo, useState, type ReactNode } from 'react'
import type { UserRole } from './types'

interface Session {
  token: string
  email: string
  role: UserRole
  nationalId: string | null
}

interface AuthContextValue {
  session: Session | null
  login: (session: Session) => void
  logout: () => void
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

const STORAGE_KEY = 'mileageClaims.session'

function loadSession(): Session | null {
  const raw = localStorage.getItem(STORAGE_KEY)
  if (!raw) return null
  try {
    return JSON.parse(raw) as Session
  } catch {
    return null
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<Session | null>(loadSession)

  const value = useMemo<AuthContextValue>(
    () => ({
      session,
      login: (s) => {
        localStorage.setItem(STORAGE_KEY, JSON.stringify(s))
        localStorage.setItem('mileageClaims.token', s.token)
        setSession(s)
      },
      logout: () => {
        localStorage.removeItem(STORAGE_KEY)
        localStorage.removeItem('mileageClaims.token')
        setSession(null)
      },
    }),
    [session],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth debe usarse dentro de AuthProvider')
  return ctx
}
