import { useEffect, useState } from 'react'
import { DataTable, type DataTableColumn } from '../components/DataTable'
import { Layout } from '../components/Layout'
import { api, ApiError } from '../lib/api'
import type { FuelType, RateTableEntry, Store, StoreDistance, VehicleType } from '../lib/types'

type RateTableEntryInput = Omit<RateTableEntry, 'id'>

const emptyRate: RateTableEntryInput = {
  vehicleType: 'Car',
  fuelType: 'Gasoline',
  engineDisplacementMin: 1000,
  engineDisplacementMax: 3000,
  vehicleAgeYears: 0,
  ratePerKm: 200,
}

const distanceColumns: DataTableColumn<StoreDistance>[] = [
  { key: 'originStoreId', header: 'Origen', render: (d) => d.originStoreId, sortAccessor: (d) => d.originStoreId },
  { key: 'destinationStoreId', header: 'Destino', render: (d) => d.destinationStoreId, sortAccessor: (d) => d.destinationStoreId },
  { key: 'distanceKm', header: 'Km', render: (d) => d.distanceKm, sortAccessor: (d) => d.distanceKm },
]

type AdminTab = 'timer' | 'rates' | 'stores' | 'distances'

const tabs: { key: AdminTab; label: string }[] = [
  { key: 'timer', label: 'Temporizador' },
  { key: 'rates', label: 'Tabla de tarifas' },
  { key: 'stores', label: 'Tiendas' },
  { key: 'distances', label: 'Distancias entre tiendas' },
]

export function AdminPage() {
  const [tab, setTab] = useState<AdminTab>('timer')

  return (
    <Layout title="Administración">
      <div className="mb-6 flex flex-wrap gap-2 border-b border-neutral-200">
        {tabs.map((t) => (
          <button
            key={t.key}
            onClick={() => setTab(t.key)}
            className={`-mb-px rounded-t-md border-b-2 px-4 py-2 text-sm font-medium ${
              tab === t.key
                ? 'border-brand-green text-brand-green'
                : 'border-transparent text-neutral-500 hover:text-neutral-700'
            }`}
          >
            {t.label}
          </button>
        ))}
      </div>

      {tab === 'timer' && <TimerSection />}
      {tab === 'rates' && <RateTableSection />}
      {tab === 'stores' && <StoresSection />}
      {tab === 'distances' && <StoreDistancesSection />}
    </Layout>
  )
}

interface TimerCycleResult {
  remindersSent: number
  claimsDiscarded: number
  claimsPurged: number
}

function TimerSection() {
  const [result, setResult] = useState<TimerCycleResult | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  async function run() {
    setBusy(true)
    setError(null)
    try {
      setResult(await api.post<TimerCycleResult>('/admin/timer/run-once'))
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'No se pudo correr el ciclo.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <section className="rounded-xl border border-neutral-200 bg-white p-6">
      <h2 className="mb-2 text-lg font-semibold">Temporizador</h2>
      <p className="mb-4 text-xs text-neutral-500">
        Normalmente corre solo cada cierto intervalo. Este botón fuerza un ciclo ahora mismo
        (recordatorios RN-14, descartes RN-13) — solo para probar/demostrar, no reemplaza el
        ciclo automático.
      </p>
      <ErrorBanner message={error} />
      <button onClick={run} disabled={busy} className="rounded-md bg-brand-green px-4 py-2 text-sm font-medium text-white hover:bg-brand-green-dark disabled:opacity-60">
        {busy ? 'Corriendo…' : 'Forzar ciclo ahora'}
      </button>
      {result && (
        <p className="mt-3 text-sm text-neutral-700">
          {result.remindersSent} recordatorio(s), {result.claimsDiscarded} descarte(s),{' '}
          {result.claimsPurged} boleta(s) purgada(s).
        </p>
      )}
    </section>
  )
}

function ErrorBanner({ message }: { message: string | null }) {
  if (!message) return null
  return <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-brand-red">{message}</p>
}

function RateTableSection() {
  const [rates, setRates] = useState<RateTableEntry[]>([])
  const [form, setForm] = useState<RateTableEntryInput>(emptyRate)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  async function reload() {
    setRates(await api.get<RateTableEntry[]>('/admin/rate-table'))
  }

  useEffect(() => {
    reload().catch((err) => setError(err instanceof ApiError ? err.message : 'No se pudo cargar.'))
  }, [])

  async function handleSubmit() {
    setError(null)
    try {
      if (editingId) {
        await api.put(`/admin/rate-table/${editingId}`, form)
      } else {
        await api.post('/admin/rate-table', form)
      }
      setForm(emptyRate)
      setEditingId(null)
      await reload()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'No se pudo guardar la tarifa.')
    }
  }

  const rateColumns: DataTableColumn<RateTableEntry>[] = [
    { key: 'vehicleType', header: 'Transporte', render: (r) => r.vehicleType, sortAccessor: (r) => r.vehicleType },
    { key: 'fuelType', header: 'Combustible', render: (r) => r.fuelType, sortAccessor: (r) => r.fuelType },
    {
      key: 'displacement',
      header: 'Cilindraje',
      render: (r) => `${r.engineDisplacementMin}-${r.engineDisplacementMax}cc`,
      sortAccessor: (r) => r.engineDisplacementMin,
    },
    { key: 'age', header: 'Antigüedad', render: (r) => `${r.vehicleAgeYears} años`, sortAccessor: (r) => r.vehicleAgeYears },
    { key: 'rate', header: '₡/km', render: (r) => r.ratePerKm, sortAccessor: (r) => r.ratePerKm },
    {
      key: 'actions',
      header: '',
      render: (r) => (
        <button
          className="text-xs text-brand-green underline"
          onClick={() => {
            setEditingId(r.id)
            setForm({
              vehicleType: r.vehicleType,
              fuelType: r.fuelType,
              engineDisplacementMin: r.engineDisplacementMin,
              engineDisplacementMax: r.engineDisplacementMax,
              vehicleAgeYears: r.vehicleAgeYears,
              ratePerKm: r.ratePerKm,
            })
          }}
        >
          Editar
        </button>
      ),
    },
  ]

  return (
    <section className="rounded-xl border border-neutral-200 bg-white p-6">
      <h2 className="mb-4 text-lg font-semibold">Tabla de tarifas</h2>
      <ErrorBanner message={error} />

      <div className="mb-4">
        <DataTable
          columns={rateColumns}
          data={rates}
          rowKey={(r) => r.id}
          getSearchText={(r) => `${r.vehicleType} ${r.fuelType}`}
          emptyMessage="Todavía no hay tarifas."
          searchPlaceholder="Buscar transporte o combustible…"
        />
      </div>

      <div className="grid grid-cols-2 gap-3 rounded-md border border-dashed border-neutral-300 p-4 sm:grid-cols-3 md:grid-cols-6">
        <label className="text-xs">
          Transporte
          <select value={form.vehicleType} onChange={(e) => setForm({ ...form, vehicleType: e.target.value as VehicleType })} className="mt-1 w-full rounded-md border border-neutral-300 px-2 py-1">
            <option value="Car">Carro</option>
            <option value="Motorcycle">Moto</option>
          </select>
        </label>
        <label className="text-xs">
          Combustible
          <select value={form.fuelType} onChange={(e) => setForm({ ...form, fuelType: e.target.value as FuelType })} className="mt-1 w-full rounded-md border border-neutral-300 px-2 py-1">
            <option value="Gasoline">Gasolina</option>
            <option value="Diesel">Diésel</option>
            <option value="Hybrid">Híbrido</option>
            <option value="Electric">Eléctrico</option>
          </select>
        </label>
        <label className="text-xs">
          Cilindraje min
          <input type="number" value={form.engineDisplacementMin} onChange={(e) => setForm({ ...form, engineDisplacementMin: Number(e.target.value) })} className="mt-1 w-full rounded-md border border-neutral-300 px-2 py-1" />
        </label>
        <label className="text-xs">
          Cilindraje max
          <input type="number" value={form.engineDisplacementMax} onChange={(e) => setForm({ ...form, engineDisplacementMax: Number(e.target.value) })} className="mt-1 w-full rounded-md border border-neutral-300 px-2 py-1" />
        </label>
        <label className="text-xs">
          Antigüedad (años)
          <input type="number" value={form.vehicleAgeYears} onChange={(e) => setForm({ ...form, vehicleAgeYears: Number(e.target.value) })} className="mt-1 w-full rounded-md border border-neutral-300 px-2 py-1" />
        </label>
        <label className="text-xs">
          ₡ por km
          <input type="number" value={form.ratePerKm} onChange={(e) => setForm({ ...form, ratePerKm: Number(e.target.value) })} className="mt-1 w-full rounded-md border border-neutral-300 px-2 py-1" />
        </label>
      </div>
      <div className="mt-3 flex gap-3">
        <button onClick={handleSubmit} className="rounded-md bg-brand-green px-4 py-2 text-sm font-medium text-white hover:bg-brand-green-dark">
          {editingId ? 'Guardar cambios' : 'Agregar tarifa'}
        </button>
        {editingId && (
          <button
            onClick={() => {
              setEditingId(null)
              setForm(emptyRate)
            }}
            className="rounded-md border border-neutral-300 px-4 py-2 text-sm"
          >
            Cancelar
          </button>
        )}
      </div>
    </section>
  )
}

function StoresSection() {
  const [stores, setStores] = useState<Store[]>([])
  const [name, setName] = useState('')
  const [error, setError] = useState<string | null>(null)

  async function reload() {
    setStores(await api.get<Store[]>('/admin/stores'))
  }

  useEffect(() => {
    reload().catch((err) => setError(err instanceof ApiError ? err.message : 'No se pudo cargar.'))
  }, [])

  async function handleAdd() {
    setError(null)
    try {
      await api.post('/admin/stores', { name })
      setName('')
      await reload()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'No se pudo crear la tienda.')
    }
  }

  return (
    <section className="rounded-xl border border-neutral-200 bg-white p-6">
      <h2 className="mb-4 text-lg font-semibold">Tiendas</h2>
      <ErrorBanner message={error} />
      <ul className="mb-4 flex flex-wrap gap-2">
        {stores.map((s) => (
          <li key={s.id} className="rounded-full bg-neutral-100 px-3 py-1 text-sm">
            {s.id} · {s.name}
          </li>
        ))}
      </ul>
      <div className="flex gap-3">
        <input value={name} onChange={(e) => setName(e.target.value)} placeholder="Nombre de la tienda" className="rounded-md border border-neutral-300 px-2 py-2 text-sm" />
        <button onClick={handleAdd} className="rounded-md bg-brand-green px-4 py-2 text-sm font-medium text-white hover:bg-brand-green-dark">
          Agregar tienda
        </button>
      </div>
    </section>
  )
}

function StoreDistancesSection() {
  const [distances, setDistances] = useState<StoreDistance[]>([])
  const [origin, setOrigin] = useState('')
  const [destination, setDestination] = useState('')
  const [km, setKm] = useState('')
  const [error, setError] = useState<string | null>(null)

  async function reload() {
    setDistances(await api.get<StoreDistance[]>('/admin/store-distances'))
  }

  useEffect(() => {
    reload().catch((err) => setError(err instanceof ApiError ? err.message : 'No se pudo cargar.'))
  }, [])

  async function handleSave() {
    setError(null)
    try {
      await api.put('/admin/store-distances', {
        originStoreId: Number(origin),
        destinationStoreId: Number(destination),
        distanceKm: Number(km),
      })
      setOrigin('')
      setDestination('')
      setKm('')
      await reload()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'No se pudo guardar la distancia.')
    }
  }

  return (
    <section className="rounded-xl border border-neutral-200 bg-white p-6">
      <h2 className="mb-4 text-lg font-semibold">Distancias entre tiendas</h2>
      <ErrorBanner message={error} />

      <div className="mb-4">
        <DataTable
          columns={distanceColumns}
          data={distances}
          rowKey={(d) => String(d.id)}
          getSearchText={(d) => `${d.originStoreId} ${d.destinationStoreId}`}
          emptyMessage="Todavía no hay distancias registradas."
          searchPlaceholder="Buscar por ID de tienda…"
        />
      </div>

      <div className="flex flex-wrap items-end gap-3 rounded-md border border-dashed border-neutral-300 p-4">
        <label className="text-xs">
          ID tienda origen
          <input value={origin} onChange={(e) => setOrigin(e.target.value)} className="mt-1 block w-28 rounded-md border border-neutral-300 px-2 py-1" />
        </label>
        <label className="text-xs">
          ID tienda destino
          <input value={destination} onChange={(e) => setDestination(e.target.value)} className="mt-1 block w-28 rounded-md border border-neutral-300 px-2 py-1" />
        </label>
        <label className="text-xs">
          Distancia (km)
          <input value={km} onChange={(e) => setKm(e.target.value)} className="mt-1 block w-28 rounded-md border border-neutral-300 px-2 py-1" />
        </label>
        <button onClick={handleSave} className="rounded-md bg-brand-green px-4 py-2 text-sm font-medium text-white hover:bg-brand-green-dark">
          Guardar
        </button>
      </div>
    </section>
  )
}
