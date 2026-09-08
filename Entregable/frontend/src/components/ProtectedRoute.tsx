import type { ReactNode } from 'react'
import { Navigate } from 'react-router-dom'
import { useAuth } from '../lib/auth'
import type { UserRole } from '../lib/types'

export function ProtectedRoute({ role, children }: { role: UserRole; children: ReactNode }) {
  const { session } = useAuth()

  if (!session) return <Navigate to="/login" replace />
  if (session.role !== role) return <Navigate to="/login" replace />

  return <>{children}</>
}
