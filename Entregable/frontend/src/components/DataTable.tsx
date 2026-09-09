import { useMemo, useState, type ReactNode } from 'react'

export interface DataTableColumn<T> {
  key: string
  header: string
  render: (row: T) => ReactNode
  sortAccessor?: (row: T) => string | number
}

interface DataTableProps<T> {
  columns: DataTableColumn<T>[]
  data: T[]
  rowKey: (row: T) => string
  getSearchText?: (row: T) => string
  pageSize?: number
  emptyMessage?: string
  searchPlaceholder?: string
}

type SortState = { key: string; direction: 'asc' | 'desc' } | null

export function DataTable<T>({
  columns,
  data,
  rowKey,
  getSearchText,
  pageSize = 10,
  emptyMessage = 'Sin resultados.',
  searchPlaceholder = 'Buscar…',
}: DataTableProps<T>) {
  const [search, setSearch] = useState('')
  const [sort, setSort] = useState<SortState>(null)
  const [page, setPage] = useState(1)

  const filtered = useMemo(() => {
    if (!getSearchText || !search.trim()) return data
    const needle = search.trim().toLowerCase()
    return data.filter((row) => getSearchText(row).toLowerCase().includes(needle))
  }, [data, getSearchText, search])

  const sorted = useMemo(() => {
    if (!sort) return filtered
    const column = columns.find((c) => c.key === sort.key)
    if (!column?.sortAccessor) return filtered
    const factor = sort.direction === 'asc' ? 1 : -1
    return [...filtered].sort((a, b) => {
      const av = column.sortAccessor!(a)
      const bv = column.sortAccessor!(b)
      if (av < bv) return -1 * factor
      if (av > bv) return 1 * factor
      return 0
    })
  }, [filtered, sort, columns])

  const totalPages = Math.max(1, Math.ceil(sorted.length / pageSize))
  const currentPage = Math.min(page, totalPages)
  const pageRows = sorted.slice((currentPage - 1) * pageSize, currentPage * pageSize)

  function toggleSort(column: DataTableColumn<T>) {
    if (!column.sortAccessor) return
    setSort((prev) => {
      if (prev?.key !== column.key) return { key: column.key, direction: 'asc' }
      if (prev.direction === 'asc') return { key: column.key, direction: 'desc' }
      return null
    })
  }

  return (
    <div>
      {getSearchText && (
        <div className="mb-3 flex justify-end">
          <input
            value={search}
            onChange={(e) => {
              setSearch(e.target.value)
              setPage(1)
            }}
            placeholder={searchPlaceholder}
            className="w-64 rounded-md border border-neutral-300 px-3 py-1.5 text-sm focus:border-brand-green focus:outline-none"
          />
        </div>
      )}

      <div className="overflow-x-auto">
        <table className="w-full text-left text-sm">
          <thead>
            <tr className="border-b border-neutral-200 text-neutral-500">
              {columns.map((c) => (
                <th
                  key={c.key}
                  onClick={() => toggleSort(c)}
                  className={`px-2 py-2 first:pl-4 ${c.sortAccessor ? 'cursor-pointer select-none hover:text-neutral-700' : ''}`}
                >
                  {c.header}
                  {sort?.key === c.key && (sort.direction === 'asc' ? ' ▲' : ' ▼')}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {pageRows.map((row) => (
              <tr key={rowKey(row)} className="border-b border-neutral-100">
                {columns.map((c) => (
                  <td key={c.key} className="px-2 py-2 first:pl-4">
                    {c.render(row)}
                  </td>
                ))}
              </tr>
            ))}
            {pageRows.length === 0 && (
              <tr>
                <td colSpan={columns.length} className="px-4 py-3 text-neutral-500">
                  {emptyMessage}
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {totalPages > 1 && (
        <div className="mt-3 flex items-center justify-between text-xs text-neutral-500">
          <span>
            Página {currentPage} de {totalPages} · {sorted.length} registro(s)
          </span>
          <div className="flex gap-2">
            <button
              disabled={currentPage <= 1}
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              className="rounded-md border border-neutral-300 px-3 py-1 disabled:opacity-40"
            >
              Anterior
            </button>
            <button
              disabled={currentPage >= totalPages}
              onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
              className="rounded-md border border-neutral-300 px-3 py-1 disabled:opacity-40"
            >
              Siguiente
            </button>
          </div>
        </div>
      )}
    </div>
  )
}
