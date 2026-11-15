import { lazy, Suspense } from 'react'
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { AuthProvider } from './context/AuthContext'
import ProtectedRoute from './components/ProtectedRoute'
import LandingPage from './pages/LandingPage.jsx'

const Dashboard = lazy(() => import('./pages/Dashboard.jsx'))
const LoginPage = lazy(() => import('./pages/LoginPage.jsx'))
const RegisterPage = lazy(() => import('./pages/RegisterPage.jsx'))

function LoadingScreen() {
  return (
    <div style={{
      display: 'grid',
      placeItems: 'center',
      height: '100vh',
      background: '#352A28',
      color: '#FFFFFF',
      fontFamily: "'Manrope', sans-serif"
    }}>
      <div style={{ textAlign: 'center' }}>
        <div style={{
          width: 52,
          height: 52,
          borderRadius: 14,
          background: 'rgba(255, 255, 255, 0.05)',
          display: 'grid',
          placeItems: 'center',
          margin: '0 auto 16px',
          boxShadow: '0 4px 16px rgba(217, 119, 87, 0.35)',
          padding: 4
        }}>
          <img
            src="/documind-icon.png"
            alt="DocuMind AI Logo"
            style={{ width: '100%', height: '100%', objectFit: 'contain' }}
          />
        </div>
        <strong style={{ display: 'block', fontSize: 18, marginBottom: 6 }}>
          Loading DocuMind...
        </strong>
        <span style={{ fontSize: 13, color: '#AFA19B' }}>
          Preparing document intelligence engine
        </span>
      </div>
    </div>
  )
}

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Suspense fallback={<LoadingScreen />}>
          <Routes>
            <Route path="/" element={<LandingPage />} />
            <Route path="/login" element={<LoginPage />} />
            <Route path="/register" element={<RegisterPage />} />
            <Route
              path="/app"
              element={
                <ProtectedRoute>
                  <Dashboard />
                </ProtectedRoute>
              }
            />
            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </Suspense>
      </AuthProvider>
    </BrowserRouter>
  )
}
