import { useEffect, useState } from 'react'
import { Layout } from '../components/Layout'
import { api, ApiError } from '../lib/api'
import type { MileageClaimDto } from '../lib/types'

function money(amount: number) {
  return amount.toLocaleString('es-CR', { style: 'currency', currency: 'CRC' })
}

export function FinancePage() {
  const [claims, setClaims] = useState<MileageClaimDto[]>([])
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    api
      .get<MileageClaimDto[]>('/finance/mileage-claims')
      .then(setClaims)
      .catch((err) => setError(err instanceof ApiError ? err.message : 'No se pudo cargar.'))
  }, [])

  return (
    <Layout title="Boletas aprobadas">
      {error && <p className="mb-4 rounded-md bg-red-50 px-3 py-2 text-sm text-brand-red">{error}</p>}

      <table className="w-full rounded-xl border border-neutral-200 bg-white text-left text-sm">
        <thead>
          <tr className="border-b border-neutral-200 text-neutral-500">
            <th className="px-4 py-3">Colaborador</th>
            <th>Jefatura</th>
            <th>Recibida</th>
            <th className="pr-4">Monto</th>
          </tr>
        </thead>
        <tbody>
          {claims.map((c) => (
            <tr key={c.id} className="border-b border-neutral-100">
              <td className="px-4 py-3">{c.employeeName}</td>
              <td>{c.approverEmail}</td>
              <td>{c.financeReceivedAt?.slice(0, 10)}</td>
              <td className="pr-4 font-medium">{money(c.totalAmount)}</td>
            </tr>
          ))}
          {claims.length === 0 && (
            <tr>
              <td colSpan={4} className="px-4 py-3 text-neutral-500">Todavía no hay boletas aprobadas.</td>
            </tr>
          )}
        </tbody>
      </table>
    </Layout>
  )
}
