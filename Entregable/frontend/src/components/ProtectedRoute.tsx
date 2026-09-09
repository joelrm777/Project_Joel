import type { ReactNode } from 'react'
import { Navigate } from 'react-router-dom'
import { useAuth } from '../lib/auth'
import type { UserRole } from '../lib/types'

export function ProtectedRoute({ roles, children }: { roles: UserRole[]; children: ReactNode }) {
  const { session } = useAuth()

  if (!session) return <Navigate to="/login" replace />
  if (!roles.includes(session.role)) return <Navigate to="/login" replace />

  return <>{children}</>
}
