import { Navigate, useLocation } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

export default function ProtectedRoute({ children }) {
  const { user, loading } = useAuth()
  const location = useLocation()

  if (loading) {
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
            width: 44,
            height: 44,
            borderRadius: 12,
            background: '#D97757',
            display: 'grid',
            placeItems: 'center',
            margin: '0 auto 16px',
            boxShadow: '0 4px 16px rgba(217, 119, 87, 0.4)'
          }}>
            <span style={{ fontSize: 20 }}>⚡</span>
          </div>
          <strong style={{ display: 'block', fontSize: 18, marginBottom: 6 }}>Authenticating Session...</strong>
          <span style={{ fontSize: 13, color: '#AFA19B' }}>Verifying security credentials</span>
        </div>
      </div>
    )
  }

  if (!user) {
    return <Navigate to="/login" replace state={{ from: location }} />
  }

  return children
}
