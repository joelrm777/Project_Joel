import { useEffect, useState } from 'react'
import { Layout } from '../components/Layout'
import { api, ApiError } from '../lib/api'
import type { MileageClaimDto, SummaryReport } from '../lib/types'

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

      <table className="mb-8 w-full rounded-xl border border-neutral-200 bg-white text-left text-sm">
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

      <ReportSection />
    </Layout>
  )
}

function ReportSection() {
  const [approverEmail, setApproverEmail] = useState('')
  const [from, setFrom] = useState('')
  const [to, setTo] = useState('')
  const [report, setReport] = useState<SummaryReport | null>(null)
  const [error, setError] = useState<string | null>(null)

  async function search() {
    setError(null)
    try {
      const params = new URLSearchParams()
      if (approverEmail) params.set('approverEmail', approverEmail)
      if (from) params.set('from', from)
      if (to) params.set('to', to)
      setReport(await api.get<SummaryReport>(`/reports/summary?${params.toString()}`))
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'No se pudo cargar el reporte.')
    }
  }

  return (
    <section className="rounded-xl border border-neutral-200 bg-white p-6">
      <h2 className="mb-2 text-lg font-semibold">Reportes (REG-2)</h2>
      <p className="mb-4 text-xs text-neutral-500">
        Monto, volumen y tiempo promedio de trámite por jefatura y periodo — incluye boletas
        ya purgadas del detalle (retenidas 12 meses como resumen).
      </p>
      {error && <p className="mb-3 rounded-md bg-red-50 px-3 py-2 text-sm text-brand-red">{error}</p>}

      <div className="mb-4 flex flex-wrap items-end gap-3">
        <label className="text-xs">
          Correo de jefatura (opcional)
          <input value={approverEmail} onChange={(e) => setApproverEmail(e.target.value)} className="mt-1 block w-64 rounded-md border border-neutral-300 px-2 py-1.5" placeholder="carlos.jimenez@automercado.test" />
        </label>
        <label className="text-xs">
          Desde
          <input type="date" value={from} onChange={(e) => setFrom(e.target.value)} className="mt-1 block rounded-md border border-neutral-300 px-2 py-1.5" />
        </label>
        <label className="text-xs">
          Hasta
          <input type="date" value={to} onChange={(e) => setTo(e.target.value)} className="mt-1 block rounded-md border border-neutral-300 px-2 py-1.5" />
        </label>
        <button onClick={search} className="rounded-md bg-brand-green px-4 py-2 text-sm font-medium text-white hover:bg-brand-green-dark">
          Consultar
        </button>
      </div>

      {report && (
        <>
          <div className="mb-4 grid grid-cols-3 gap-3 text-sm">
            <div className="rounded-md bg-neutral-50 p-3">
              <p className="text-neutral-500">Monto total</p>
              <p className="text-lg font-semibold">{money(report.totalAmount)}</p>
            </div>
            <div className="rounded-md bg-neutral-50 p-3">
              <p className="text-neutral-500">Boletas</p>
              <p className="text-lg font-semibold">{report.claimCount}</p>
            </div>
            <div className="rounded-md bg-neutral-50 p-3">
              <p className="text-neutral-500">Trámite promedio</p>
              <p className="text-lg font-semibold">
                {report.averageProcessingHours !== null ? `${report.averageProcessingHours.toFixed(1)} h` : '—'}
              </p>
            </div>
          </div>

          <table className="w-full text-left text-sm">
            <thead>
              <tr className="border-b border-neutral-200 text-neutral-500">
                <th className="py-2">Colaborador</th>
                <th>Enviada</th>
                <th>Decidida</th>
                <th>Estado</th>
                <th>Monto</th>
              </tr>
            </thead>
            <tbody>
              {report.rows.map((r) => (
                <tr key={r.mileageClaimId} className="border-b border-neutral-100">
                  <td className="py-2">{r.employeeName}</td>
                  <td>{r.submittedAt.slice(0, 10)}</td>
                  <td>{r.decidedAt?.slice(0, 10) ?? '—'}</td>
                  <td>{r.status}</td>
                  <td>{money(r.totalAmount)}</td>
                </tr>
              ))}
              {report.rows.length === 0 && (
                <tr>
                  <td colSpan={5} className="py-3 text-neutral-500">Sin resultados para ese filtro.</td>
                </tr>
              )}
            </tbody>
          </table>
        </>
      )}
    </section>
  )
}
