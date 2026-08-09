import { Navigate, Route, Routes } from 'react-router-dom'
import Navbar from './components/Navbar'
import ProtectedRoute from './components/ProtectedRoute'
import Login from './pages/Login'
import Register from './pages/Register'
import Services from './pages/Services'
import SubmitRequest from './pages/SubmitRequest'
import MyRequests from './pages/MyRequests'
import AdminRequests from './pages/AdminRequests'
import AdminServices from './pages/AdminServices'

export default function App() {
  return (
    <>
      <Navbar />
      <main className="container">
        <Routes>
          <Route path="/" element={<Navigate to="/login" replace />} />
          <Route path="/login" element={<Login />} />
          <Route path="/register" element={<Register />} />
          <Route element={<ProtectedRoute role="Student" />}>
            <Route path="/services" element={<Services />} />
            <Route path="/submit-request/:serviceId" element={<SubmitRequest />} />
            <Route path="/my-requests" element={<MyRequests />} />
          </Route>
          <Route element={<ProtectedRoute role="Admin" />}>
            <Route path="/admin/requests" element={<AdminRequests />} />
            <Route path="/admin/services" element={<AdminServices />} />
          </Route>
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </main>
    </>
  )
}
