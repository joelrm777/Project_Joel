import { Navigate, Route, Routes } from 'react-router-dom'
import { ProtectedRoute } from './components/ProtectedRoute'
import { useAuth } from './lib/auth'
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
          <ProtectedRoute role="Employee">
            <EmployeePage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/aprobaciones"
        element={
          <ProtectedRoute role="Approver">
            <ApproverPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/finanzas"
        element={
          <ProtectedRoute role="Finance">
            <FinancePage />
          </ProtectedRoute>
        }
      />
    </Routes>
  )
}
