import { Navigate, Route, Routes } from 'react-router-dom'
import { ProtectedRoute } from './components/ProtectedRoute'
import { useAuth } from './lib/auth'
import { AdminPage } from './pages/AdminPage'
import { ApproverPage } from './pages/ApproverPage'
import { EmployeePage } from './pages/EmployeePage'
import { FinancePage } from './pages/FinancePage'
import { LoginPage } from './pages/LoginPage'

function Home() {
  const { session } = useAuth()
  if (!session) return <Navigate to="/login" replace />
  const home: Record<string, string> = {
    Employee: '/mis-boletas',
    Approver: '/aprobaciones',
    Administrator: '/admin',
    Finance: '/finanzas',
  }
  return <Navigate to={home[session.role] ?? '/login'} replace />
}

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<Home />} />
      <Route path="/login" element={<LoginPage />} />
      <Route
        path="/mis-boletas"
        element={
          <ProtectedRoute roles={['Employee', 'Approver', 'Administrator', 'Finance']}>
            <EmployeePage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/aprobaciones"
        element={
          <ProtectedRoute roles={['Approver']}>
            <ApproverPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/finanzas"
        element={
          <ProtectedRoute roles={['Finance']}>
            <FinancePage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/admin"
        element={
          <ProtectedRoute roles={['Administrator']}>
            <AdminPage />
          </ProtectedRoute>
        }
      />
    </Routes>
  )
}
