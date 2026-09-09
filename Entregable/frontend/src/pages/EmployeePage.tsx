import { useEffect, useState } from 'react'
import { Layout } from '../components/Layout'
import { api, ApiError } from '../lib/api'
import type { DriveType, FuelType, MileageClaimDto, Store, VehicleType } from '../lib/types'

const statusStyles: Record<string, string> = {
  Draft: 'bg-neutral-100 text-neutral-700',
  Pending: 'bg-amber-100 text-amber-800',
  Approved: 'bg-brand-green/10 text-brand-green',
  Rejected: 'bg-red-100 text-brand-red',
  Discarded: 'bg-neutral-200 text-neutral-500',
}

function money(amount: number) {
  return amount.toLocaleString('es-CR', { style: 'currency', currency: 'CRC' })
}

export function EmployeePage() {
  const [claims, setClaims] = useState<MileageClaimDto[]>([])
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [showNewForm, setShowNewForm] = useState(false)

  async function reload() {
    const mine = await api.get<MileageClaimDto[]>('/mileage-claims/mine')
    setClaims(mine)
    if (selectedId && !mine.some((c) => c.id === selectedId)) setSelectedId(null)
  }

  useEffect(() => {
    reload().catch((err) => setError(err instanceof ApiError ? err.message : 'No se pudo cargar.'))
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const selected = claims.find((c) => c.id === selectedId) ?? null

  return (
    <Layout title="Mis boletas">
      {error && <p className="mb-4 rounded-md bg-red-50 px-3 py-2 text-sm text-brand-red">{error}</p>}

      <div className="grid grid-cols-1 gap-6 md:grid-cols-3">
        <div className="md:col-span-1">
          <button
            onClick={() => {
              setShowNewForm(true)
              setSelectedId(null)
            }}
            className="mb-4 w-full rounded-md bg-brand-green px-3 py-2 text-sm font-medium text-white hover:bg-brand-green-dark"
          >
            + Nueva boleta
          </button>

          <ul className="space-y-2">
            {claims.map((c) => (
              <li key={c.id}>
                <button
                  onClick={() => {
                    setShowNewForm(false)
                    setSelectedId(c.id)
                  }}
                  className={`w-full rounded-md border px-3 py-2 text-left text-sm hover:border-brand-green ${
                    selectedId === c.id ? 'border-brand-green bg-brand-green/5' : 'border-neutral-200'
                  }`}
                >
                  <div className="flex items-center justify-between">
                    <span className="font-medium">{c.plateNumber || '(sin placa)'}</span>
                    <span className={`rounded-full px-2 py-0.5 text-xs ${statusStyles[c.status]}`}>{c.status}</span>
                  </div>
                  <div className="text-neutral-500">{money(c.totalAmount)}</div>
                </button>
              </li>
            ))}
            {claims.length === 0 && <li className="text-sm text-neutral-500">Todavía no tenés boletas.</li>}
          </ul>
        </div>

        <div className="md:col-span-2">
          {showNewForm && (
            <NewClaimForm
              onCreated={async (claim) => {
                setShowNewForm(false)
                await reload()
                setSelectedId(claim.id)
              }}
            />
          )}
          {selected && (
            <ClaimDetail
              claim={selected}
              onChanged={async () => {
                await reload()
              }}
            />
          )}
          {!showNewForm && !selected && (
            <p className="text-sm text-neutral-500">Elegí una boleta de la lista o creá una nueva.</p>
          )}
        </div>
      </div>
    </Layout>
  )
}

function VehicleFields({
  vehicleType, setVehicleType,
  fuelType, setFuelType,
  plateNumber, setPlateNumber,
  driveType, setDriveType,
  modelYear, setModelYear,
  engineDisplacement, setEngineDisplacement,
}: {
  vehicleType: VehicleType; setVehicleType: (v: VehicleType) => void
  fuelType: FuelType; setFuelType: (v: FuelType) => void
  plateNumber: string; setPlateNumber: (v: string) => void
  driveType: DriveType; setDriveType: (v: DriveType) => void
  modelYear: number; setModelYear: (v: number) => void
  engineDisplacement: number; setEngineDisplacement: (v: number) => void
}) {
  return (
    <div className="grid grid-cols-2 gap-3">
      <label className="text-sm">
        Tipo de transporte
        <select value={vehicleType} onChange={(e) => setVehicleType(e.target.value as VehicleType)} className="mt-1 w-full rounded-md border border-neutral-300 px-2 py-1.5">
          <option value="Car">Carro</option>
          <option value="Motorcycle">Moto</option>
        </select>
      </label>
      <label className="text-sm">
        Combustible
        <select value={fuelType} onChange={(e) => setFuelType(e.target.value as FuelType)} className="mt-1 w-full rounded-md border border-neutral-300 px-2 py-1.5">
          <option value="Gasoline">Gasolina</option>
          <option value="Diesel">Diésel</option>
          <option value="Hybrid">Híbrido</option>
          <option value="Electric">Eléctrico</option>
        </select>
      </label>
      <label className="text-sm">
        Placa
        <input value={plateNumber} onChange={(e) => setPlateNumber(e.target.value)} className="mt-1 w-full rounded-md border border-neutral-300 px-2 py-1.5" />
      </label>
      <label className="text-sm">
        Tracción
        <select value={driveType} onChange={(e) => setDriveType(e.target.value as DriveType)} className="mt-1 w-full rounded-md border border-neutral-300 px-2 py-1.5">
          <option value="Single">Sencilla</option>
          <option value="DoubleTraction">Doble tracción</option>
          <option value="NotApplicable">No aplica</option>
        </select>
      </label>
      <label className="text-sm">
        Modelo (año)
        <input type="number" value={modelYear} onChange={(e) => setModelYear(Number(e.target.value))} className="mt-1 w-full rounded-md border border-neutral-300 px-2 py-1.5" />
      </label>
      <label className="text-sm">
        Cilindraje (cc)
        <input type="number" value={engineDisplacement} onChange={(e) => setEngineDisplacement(Number(e.target.value))} className="mt-1 w-full rounded-md border border-neutral-300 px-2 py-1.5" />
      </label>
    </div>
  )
}

function NewClaimForm({ onCreated }: { onCreated: (claim: MileageClaimDto) => void }) {
  const [vehicleType, setVehicleType] = useState<VehicleType>('Car')
  const [fuelType, setFuelType] = useState<FuelType>('Gasoline')
  const [plateNumber, setPlateNumber] = useState('')
  const [driveType, setDriveType] = useState<DriveType>('Single')
  const [modelYear, setModelYear] = useState(new Date().getFullYear())
  const [engineDisplacement, setEngineDisplacement] = useState(1500)
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  async function handleSubmit() {
    setError(null)
    setLoading(true)
    try {
      const claim = await api.post<MileageClaimDto>('/mileage-claims', {
        // El backend siempre usa la cédula del colaborador logueado (RN-17/RF-1): un
        // colaborador no puede crear boletas a nombre de otra cédula. Este valor se ignora.
        employeeNationalId: '',
        vehicleType,
        fuelType,
        plateNumber,
        driveType,
        modelYear,
        engineDisplacement,
      })
      onCreated(claim)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'No se pudo crear la boleta.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="rounded-xl border border-neutral-200 bg-white p-6">
      <h2 className="mb-4 text-lg font-semibold">Nueva boleta</h2>
      <p className="mb-3 text-xs text-neutral-500">
        Se crea con tus datos de colaborador (los trae el sistema de tu cédula automáticamente).
      </p>
      <VehicleFields
        vehicleType={vehicleType} setVehicleType={setVehicleType}
        fuelType={fuelType} setFuelType={setFuelType}
        plateNumber={plateNumber} setPlateNumber={setPlateNumber}
        driveType={driveType} setDriveType={setDriveType}
        modelYear={modelYear} setModelYear={setModelYear}
        engineDisplacement={engineDisplacement} setEngineDisplacement={setEngineDisplacement}
      />
      {error && <p className="mt-3 rounded-md bg-red-50 px-3 py-2 text-sm text-brand-red">{error}</p>}
      <button onClick={handleSubmit} disabled={loading} className="mt-4 rounded-md bg-brand-green px-4 py-2 text-sm font-medium text-white hover:bg-brand-green-dark disabled:opacity-60">
        {loading ? 'Creando…' : 'Crear boleta'}
      </button>
    </div>
  )
}

function AddTripForm({ claimId, onAdded }: { claimId: string; onAdded: () => void }) {
  const [stores, setStores] = useState<Store[]>([])
  const [date, setDate] = useState(new Date().toISOString().slice(0, 10))
  const [storeIds, setStoreIds] = useState<string>('')
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    api.get<Store[]>('/stores').then(setStores).catch(() => {})
  }, [])

  async function handleSubmit() {
    setError(null)
    setLoading(true)
    try {
      const ids = storeIds.split(',').map((s) => Number(s.trim())).filter((n) => !Number.isNaN(n))
      await api.post(`/mileage-claims/${claimId}/trips`, { date, storeIdsInOrder: ids })
      setStoreIds('')
      onAdded()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'No se pudo agregar el viaje.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="mt-4 rounded-md border border-dashed border-neutral-300 p-4">
      <h3 className="mb-2 text-sm font-semibold">Agregar viaje</h3>
      <p className="mb-2 text-xs text-neutral-500">
        Tiendas disponibles: {stores.map((s) => `${s.id}=${s.name}`).join(', ') || 'cargando…'}
      </p>
      <div className="flex flex-wrap items-end gap-3">
        <label className="text-sm">
          Fecha
          <input type="date" value={date} onChange={(e) => setDate(e.target.value)} className="mt-1 block rounded-md border border-neutral-300 px-2 py-1.5" />
        </label>
        <label className="text-sm">
          Tiendas en orden (IDs separados por coma)
          <input value={storeIds} onChange={(e) => setStoreIds(e.target.value)} placeholder="1,2,3" className="mt-1 block w-56 rounded-md border border-neutral-300 px-2 py-1.5" />
        </label>
        <button onClick={handleSubmit} disabled={loading} className="rounded-md bg-brand-green px-3 py-2 text-sm font-medium text-white hover:bg-brand-green-dark disabled:opacity-60">
          {loading ? 'Agregando…' : 'Agregar'}
        </button>
      </div>
      {error && <p className="mt-2 rounded-md bg-red-50 px-3 py-2 text-sm text-brand-red">{error}</p>}
    </div>
  )
}

function ClaimDetail({ claim, onChanged }: { claim: MileageClaimDto; onChanged: () => Promise<void> }) {
  const editable = claim.status === 'Draft' || claim.status === 'Pending' || claim.status === 'Rejected'
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const [editingVehicle, setEditingVehicle] = useState(false)
  const [vehicleType, setVehicleType] = useState<VehicleType>(claim.vehicleType)
  const [fuelType, setFuelType] = useState<FuelType>(claim.fuelType)
  const [plateNumber, setPlateNumber] = useState(claim.plateNumber)
  const [driveType, setDriveType] = useState<DriveType>(claim.driveType)
  const [modelYear, setModelYear] = useState(claim.modelYear)
  const [engineDisplacement, setEngineDisplacement] = useState(claim.engineDisplacement)

  async function run(action: () => Promise<unknown>) {
    setError(null)
    setBusy(true)
    try {
      await action()
      await onChanged()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Ocurrió un error.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="rounded-xl border border-neutral-200 bg-white p-6">
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-lg font-semibold">
          {claim.vehicleType} · {claim.plateNumber}
        </h2>
        <div className="flex items-center gap-2">
          {editable && (
            <button
              onClick={() => setEditingVehicle((v) => !v)}
              className="text-xs text-brand-green underline"
            >
              {editingVehicle ? 'Cancelar' : 'Editar vehículo'}
            </button>
          )}
          <span className={`rounded-full px-2 py-0.5 text-xs ${statusStyles[claim.status]}`}>{claim.status}</span>
        </div>
      </div>

      {claim.status === 'Rejected' && claim.rejectionReason && (
        <p className="mb-4 rounded-md bg-red-50 px-3 py-2 text-sm text-brand-red">
          Rechazada: {claim.rejectionReason}
        </p>
      )}

      {editingVehicle && (
        <div className="mb-4 rounded-md border border-dashed border-neutral-300 p-4">
          <VehicleFields
            vehicleType={vehicleType} setVehicleType={setVehicleType}
            fuelType={fuelType} setFuelType={setFuelType}
            plateNumber={plateNumber} setPlateNumber={setPlateNumber}
            driveType={driveType} setDriveType={setDriveType}
            modelYear={modelYear} setModelYear={setModelYear}
            engineDisplacement={engineDisplacement} setEngineDisplacement={setEngineDisplacement}
          />
          <button
            disabled={busy}
            onClick={() =>
              run(() =>
                api.put(`/mileage-claims/${claim.id}/vehicle`, {
                  vehicleType, fuelType, plateNumber, driveType, modelYear, engineDisplacement,
                }),
              )
            }
            className="mt-3 rounded-md bg-brand-green px-4 py-2 text-sm font-medium text-white hover:bg-brand-green-dark disabled:opacity-60"
          >
            Guardar vehículo
          </button>
          <p className="mt-2 text-xs text-neutral-500">
            Guardado, cerrá con "Cancelar" — la tabla de viajes de abajo ya refleja el nuevo monto.
          </p>
        </div>
      )}

      <table className="w-full text-left text-sm">
        <thead>
          <tr className="border-b border-neutral-200 text-neutral-500">
            <th className="py-2">Fecha</th>
            <th>Tarifa</th>
            <th>Distancia</th>
            <th>Monto</th>
            <th>Completo</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {claim.trips.map((t) => (
            <tr key={t.id} className="border-b border-neutral-100">
              <td className="py-2">{t.date}</td>
              <td>{t.rateSummary}</td>
              <td>{t.totalDistance} km</td>
              <td>{money(t.totalAmount)}</td>
              <td>{t.isDistanceComplete ? '✓' : 'Falta tramo'}</td>
              <td>
                {editable && (
                  <button
                    disabled={busy}
                    onClick={() => run(() => api.delete(`/mileage-claims/${claim.id}/trips/${t.id}`))}
                    className="text-xs text-brand-red underline disabled:opacity-60"
                  >
                    Quitar
                  </button>
                )}
              </td>
            </tr>
          ))}
          {claim.trips.length === 0 && (
            <tr>
              <td colSpan={6} className="py-3 text-neutral-500">Sin viajes todavía.</td>
            </tr>
          )}
        </tbody>
      </table>

      <p className="mt-3 text-right text-base font-semibold">Total: {money(claim.totalAmount)}</p>

      {editable && <AddTripForm claimId={claim.id} onAdded={() => run(async () => {})} />}

      {error && <p className="mt-4 rounded-md bg-red-50 px-3 py-2 text-sm text-brand-red">{error}</p>}

      <div className="mt-4 flex gap-3">
        {editable && (
          <button
            disabled={busy}
            onClick={() => run(() => api.post(`/mileage-claims/${claim.id}/submit`))}
            className="rounded-md bg-brand-green px-4 py-2 text-sm font-medium text-white hover:bg-brand-green-dark disabled:opacity-60"
          >
            Enviar a aprobación
          </button>
        )}
        {editable && (
          <button
            disabled={busy}
            onClick={() => run(() => api.delete(`/mileage-claims/${claim.id}`))}
            className="rounded-md border border-brand-red px-4 py-2 text-sm font-medium text-brand-red hover:bg-red-50 disabled:opacity-60"
          >
            Retirar boleta
          </button>
        )}
      </div>
    </div>
  )
}
