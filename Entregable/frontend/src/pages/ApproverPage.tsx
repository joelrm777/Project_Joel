import { useEffect, useState, type ReactNode } from 'react'
import { DataTable, type DataTableColumn } from '../components/DataTable'
import { Layout } from '../components/Layout'
import { api, ApiError } from '../lib/api'
import type { MileageClaimDto } from '../lib/types'

function money(amount: number) {
  return amount.toLocaleString('es-CR', { style: 'currency', currency: 'CRC' })
}

type Tab = 'pending' | 'approved'

export function ApproverPage() {
  const [tab, setTab] = useState<Tab>('pending')

  return (
    <Layout title="Aprobaciones">
      <div className="mb-6 flex gap-2">
        <TabButton active={tab === 'pending'} onClick={() => setTab('pending')}>
          Pendientes
        </TabButton>
        <TabButton active={tab === 'approved'} onClick={() => setTab('approved')}>
          Boletas que aprobé
        </TabButton>
      </div>

      {tab === 'pending' ? <PendingSection /> : <ApprovedSection />}
    </Layout>
  )
}

function TabButton({ active, onClick, children }: { active: boolean; onClick: () => void; children: ReactNode }) {
  return (
    <button
      onClick={onClick}
      className={`rounded-md px-4 py-2 text-sm font-medium ${
        active ? 'bg-brand-green text-white' : 'border border-neutral-300 text-neutral-600 hover:bg-neutral-100'
      }`}
    >
      {children}
    </button>
  )
}

function PendingSection() {
  const [claims, setClaims] = useState<MileageClaimDto[]>([])
  const [error, setError] = useState<string | null>(null)
  const [busyId, setBusyId] = useState<string | null>(null)
  const [reasonById, setReasonById] = useState<Record<string, string>>({})

  async function reload() {
    setClaims(await api.get<MileageClaimDto[]>('/approvals/pending'))
  }

  useEffect(() => {
    reload().catch((err) => setError(err instanceof ApiError ? err.message : 'No se pudo cargar.'))
  }, [])

  async function approve(id: string) {
    setBusyId(id)
    setError(null)
    try {
      await api.post(`/approvals/${id}/approve`)
      await reload()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'No se pudo aprobar.')
    } finally {
      setBusyId(null)
    }
  }

  async function reject(id: string) {
    const reason = reasonById[id]?.trim()
    if (!reason) {
      setError('El rechazo requiere un motivo.')
      return
    }
    setBusyId(id)
    setError(null)
    try {
      await api.post(`/approvals/${id}/reject`, { reason })
      await reload()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'No se pudo rechazar.')
    } finally {
      setBusyId(null)
    }
  }

  return (
    <>
      {error && <p className="mb-4 rounded-md bg-red-50 px-3 py-2 text-sm text-brand-red">{error}</p>}

      <div className="space-y-4">
        {claims.map((c) => (
          <div key={c.id} className="rounded-xl border border-neutral-200 bg-white p-5">
            <div className="mb-2 flex items-center justify-between">
              <h2 className="font-semibold">{c.employeeName}</h2>
              <span className="text-sm text-neutral-500">Enviada: {c.submittedAt?.slice(0, 10)}</span>
            </div>
            <p className="text-sm text-neutral-600">
              {c.vehicleType} · {c.plateNumber} · {c.trips.length} viaje(s)
            </p>
            <p className="mt-1 text-lg font-semibold">{money(c.totalAmount)}</p>

            <div className="mt-3 flex flex-wrap items-center gap-3">
              <button
                disabled={busyId === c.id}
                onClick={() => approve(c.id)}
                className="rounded-md bg-brand-green px-4 py-2 text-sm font-medium text-white hover:bg-brand-green-dark disabled:opacity-60"
              >
                Aprobar
              </button>
              <input
                value={reasonById[c.id] ?? ''}
                onChange={(e) => setReasonById((prev) => ({ ...prev, [c.id]: e.target.value }))}
                placeholder="Motivo del rechazo"
                className="rounded-md border border-neutral-300 px-2 py-2 text-sm"
              />
              <button
                disabled={busyId === c.id}
                onClick={() => reject(c.id)}
                className="rounded-md border border-brand-red px-4 py-2 text-sm font-medium text-brand-red hover:bg-red-50 disabled:opacity-60"
              >
                Rechazar
              </button>
            </div>
          </div>
        ))}
        {claims.length === 0 && <p className="text-sm text-neutral-500">No hay boletas pendientes.</p>}
      </div>
    </>
  )
}

function ApprovedSection() {
  const [claims, setClaims] = useState<MileageClaimDto[]>([])
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    api
      .get<MileageClaimDto[]>('/approvals/approved')
      .then(setClaims)
      .catch((err) => setError(err instanceof ApiError ? err.message : 'No se pudo cargar.'))
  }, [])

  const columns: DataTableColumn<MileageClaimDto>[] = [
    { key: 'employeeName', header: 'Colaborador', render: (c) => c.employeeName, sortAccessor: (c) => c.employeeName },
    {
      key: 'vehicle',
      header: 'Vehículo',
      render: (c) => `${c.vehicleType} · ${c.plateNumber}`,
      sortAccessor: (c) => c.plateNumber,
    },
    {
      key: 'decidedAt',
      header: 'Decidida',
      render: (c) => c.decidedAt?.slice(0, 10),
      sortAccessor: (c) => c.decidedAt ?? '',
    },
    { key: 'totalAmount', header: 'Monto', render: (c) => money(c.totalAmount), sortAccessor: (c) => c.totalAmount },
  ]

  return (
    <>
      {error && <p className="mb-4 rounded-md bg-red-50 px-3 py-2 text-sm text-brand-red">{error}</p>}

      <div className="rounded-xl border border-neutral-200 bg-white p-4">
        <DataTable
          columns={columns}
          data={claims}
          rowKey={(c) => c.id}
          getSearchText={(c) => `${c.employeeName} ${c.plateNumber}`}
          emptyMessage="Todavía no has aprobado ninguna boleta."
          searchPlaceholder="Buscar colaborador o placa…"
        />
      </div>
    </>
  )
}
