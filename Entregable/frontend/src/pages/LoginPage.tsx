import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { api, ApiError } from '../lib/api'
import { useAuth } from '../lib/auth'
import type { UserRole } from '../lib/types'

interface LoginResponse {
  token: string
  email: string
  role: UserRole
  nationalId: string | null
}

const roleHome: Record<UserRole, string> = {
  Employee: '/mis-boletas',
  Approver: '/aprobaciones',
  Administrator: '/admin',
  Finance: '/finanzas',
}

export function LoginPage() {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)
  const { login } = useAuth()
  const navigate = useNavigate()

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    setLoading(true)
    try {
      const response = await api.post<LoginResponse>('/auth/login', { email, password })
      login(response)
      navigate(roleHome[response.role])
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'No se pudo iniciar sesión.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-neutral-50 px-4">
      <div className="w-full max-w-sm rounded-xl border border-neutral-200 bg-white p-8 shadow-sm">
        <h1 className="mb-1 text-xl font-semibold text-neutral-900">Boleta de Kilometraje Digital</h1>
        <p className="mb-6 text-sm text-neutral-500">Iniciá sesión con tu cuenta corporativa (simulada).</p>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="mb-1 block text-sm font-medium text-neutral-700">Correo</label>
            <input
              type="email"
              required
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              className="w-full rounded-md border border-neutral-300 px-3 py-2 text-sm focus:border-brand-green focus:outline-none"
              placeholder="ana.rodriguez@automercado.test"
            />
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium text-neutral-700">Contraseña</label>
            <input
              type="password"
              required
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className="w-full rounded-md border border-neutral-300 px-3 py-2 text-sm focus:border-brand-green focus:outline-none"
            />
          </div>

          {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-brand-red">{error}</p>}

          <button
            type="submit"
            disabled={loading}
            className="w-full rounded-md bg-brand-green px-3 py-2 text-sm font-medium text-white hover:bg-brand-green-dark disabled:opacity-60"
          >
            {loading ? 'Ingresando…' : 'Ingresar'}
          </button>
        </form>
      </div>
    </div>
  )
}
