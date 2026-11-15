import { authenticatedFetch, setAccessToken, extractErrorMessage } from './apiClient'

export async function registerUser(fullName, email, password) {
  const response = await authenticatedFetch('/api/auth/register', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ fullName, email, password }),
  })

  if (!response.ok) {
    const message = await extractErrorMessage(
      response,
      'Registration could not be completed. Please verify your details.'
    )
    throw new Error(message)
  }

  const data = await response.json()
  setAccessToken(data.accessToken)
  return data
}

export async function loginUser(email, password) {
  const response = await authenticatedFetch('/api/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password }),
  })

  if (!response.ok) {
    const message = await extractErrorMessage(
      response,
      'Invalid email or password. Please try again.'
    )
    throw new Error(message)
  }

  const data = await response.json()
  setAccessToken(data.accessToken)
  return data
}

export async function refreshToken() {
  const response = await authenticatedFetch('/api/auth/refresh', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    timeout: 8000, // 8-second timeout for session refresh
  })

  if (!response.ok) {
    setAccessToken(null)
    throw new Error('Session expired. Please sign in again.')
  }

  const data = await response.json()
  setAccessToken(data.accessToken)
  return data
}

export async function logoutUser() {
  try {
    await authenticatedFetch('/api/auth/logout', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      timeout: 5000,
    })
  } finally {
    setAccessToken(null)
  }
}

export async function getCurrentUser() {
  const response = await authenticatedFetch('/api/auth/me', { timeout: 8000 })
  if (!response.ok) {
    throw new Error('Failed to retrieve user profile.')
  }
  return response.json()
}
