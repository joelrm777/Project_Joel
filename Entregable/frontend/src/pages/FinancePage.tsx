import { useEffect, useState } from 'react'
import { DataTable, type DataTableColumn } from '../components/DataTable'
import { Layout } from '../components/Layout'
import { api, ApiError } from '../lib/api'
import type { MileageClaimDto, SummaryReport, SummaryReportRow } from '../lib/types'

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

  const columns: DataTableColumn<MileageClaimDto>[] = [
    { key: 'employeeName', header: 'Colaborador', render: (c) => c.employeeName, sortAccessor: (c) => c.employeeName },
    { key: 'approverEmail', header: 'Jefatura', render: (c) => c.approverEmail, sortAccessor: (c) => c.approverEmail },
    {
      key: 'financeReceivedAt',
      header: 'Recibida',
      render: (c) => c.financeReceivedAt?.slice(0, 10),
      sortAccessor: (c) => c.financeReceivedAt ?? '',
    },
    { key: 'totalAmount', header: 'Monto', render: (c) => money(c.totalAmount), sortAccessor: (c) => c.totalAmount },
  ]

  return (
    <Layout title="Boletas aprobadas">
      {error && <p className="mb-4 rounded-md bg-red-50 px-3 py-2 text-sm text-brand-red">{error}</p>}

      <div className="mb-8 rounded-xl border border-neutral-200 bg-white p-4">
        <DataTable
          columns={columns}
          data={claims}
          rowKey={(c) => c.id}
          getSearchText={(c) => `${c.employeeName} ${c.approverEmail}`}
          emptyMessage="Todavía no hay boletas aprobadas."
          searchPlaceholder="Buscar colaborador o jefatura…"
        />
      </div>

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
          <input value={approverEmail} onChange={(e) => setApproverEmail(e.target.value)} className="mt-1 block w-64 rounded-md border border-neutral-300 px-2 py-1.5" placeholder="carlos.jimenez@retail.test" />
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

          <DataTable
            columns={reportColumns}
            data={report.rows}
            rowKey={(r) => r.mileageClaimId}
            getSearchText={(r) => `${r.employeeName} ${r.status}`}
            emptyMessage="Sin resultados para ese filtro."
            searchPlaceholder="Buscar colaborador o estado…"
          />
        </>
      )}
    </section>
  )
}

const reportColumns: DataTableColumn<SummaryReportRow>[] = [
  { key: 'employeeName', header: 'Colaborador', render: (r) => r.employeeName, sortAccessor: (r) => r.employeeName },
  { key: 'submittedAt', header: 'Enviada', render: (r) => r.submittedAt.slice(0, 10), sortAccessor: (r) => r.submittedAt },
  {
    key: 'decidedAt',
    header: 'Decidida',
    render: (r) => r.decidedAt?.slice(0, 10) ?? '—',
    sortAccessor: (r) => r.decidedAt ?? '',
  },
  { key: 'status', header: 'Estado', render: (r) => r.status, sortAccessor: (r) => r.status },
  { key: 'totalAmount', header: 'Monto', render: (r) => money(r.totalAmount), sortAccessor: (r) => r.totalAmount },
]
