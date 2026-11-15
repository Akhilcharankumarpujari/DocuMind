import { createContext, useContext, useState, useEffect, useCallback } from 'react'
import { loginUser, registerUser, logoutUser, refreshToken } from '../services/authService'
import { setOnUnauthorizedHandler } from '../services/apiClient'

const AuthContext = createContext(null)

export function AuthProvider({ children }) {
  const [user, setUser] = useState(null)
  const [loading, setLoading] = useState(true)

  const handleLogout = useCallback(async () => {
    try {
      await logoutUser()
    } catch {
      // Ignore network errors during logout
    } finally {
      setUser(null)
    }
  }, [])

  // Silent session restore on application boot with 5-second max guard
  useEffect(() => {
    let mounted = true

    const restoreSession = async () => {
      const fallbackTimer = setTimeout(() => {
        if (mounted) setLoading(false)
      }, 5000)

      try {
        const authData = await refreshToken()
        if (mounted && authData?.user) {
          setUser(authData.user)
        }
      } catch {
        if (mounted) {
          setUser(null)
        }
      } finally {
        clearTimeout(fallbackTimer)
        if (mounted) {
          setLoading(false)
        }
      }
    }

    restoreSession()

    // Register 401 callback
    setOnUnauthorizedHandler(() => {
      if (mounted) setUser(null)
    })

    const handleUnauthorizedEvent = () => {
      if (mounted) setUser(null)
    }
    window.addEventListener('documind:unauthorized', handleUnauthorizedEvent)

    return () => {
      mounted = false
      window.removeEventListener('documind:unauthorized', handleUnauthorizedEvent)
    }
  }, [])

  const login = async (email, password) => {
    const data = await loginUser(email, password)
    setUser(data.user)
    return data.user
  }

  const register = async (fullName, email, password) => {
    const data = await registerUser(fullName, email, password)
    setUser(data.user)
    return data.user
  }

  const value = {
    user,
    isAuthenticated: Boolean(user),
    loading,
    login,
    register,
    logout: handleLogout,
  }

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  return context
}
