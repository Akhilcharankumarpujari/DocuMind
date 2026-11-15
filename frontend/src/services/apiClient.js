const API_BASE_URL = import.meta.env.VITE_API_BASE_URL !== undefined
  ? import.meta.env.VITE_API_BASE_URL
  : ''

const DEFAULT_TIMEOUT_MS = 15000 // 15 seconds timeout

let inMemoryAccessToken = null
let refreshPromise = null
let onUnauthorizedHandler = null

export function setAccessToken(token) {
  inMemoryAccessToken = token || null
}

export function getAccessToken() {
  return inMemoryAccessToken
}

export function setOnUnauthorizedHandler(handler) {
  onUnauthorizedHandler = handler
}

/**
 * Wraps fetch with an AbortController timeout and informative network error handling.
 */
async function fetchWithTimeout(url, options = {}, timeoutMs = DEFAULT_TIMEOUT_MS) {
  const controller = new AbortController()
  const timeoutId = setTimeout(() => {
    controller.abort()
  }, timeoutMs)

  // Chain any existing caller signal if supplied
  if (options.signal) {
    options.signal.addEventListener('abort', () => controller.abort())
  }

  try {
    const response = await fetch(url, {
      ...options,
      signal: controller.signal,
    })
    return response
  } catch (err) {
    if (err.name === 'AbortError') {
      throw new Error(`Request timed out after ${timeoutMs / 1000}s. Please check that the DocuMind server is running.`)
    }
    if (err.message?.includes('Failed to fetch') || err.message?.includes('NetworkError') || err.name === 'TypeError') {
      throw new Error('Unable to connect to DocuMind server. Please verify the backend service is running and reachable.')
    }
    throw err
  } finally {
    clearTimeout(timeoutId)
  }
}

/**
 * Executes an authenticated fetch request with Bearer token injection and automatic 401 retry.
 */
export async function authenticatedFetch(path, options = {}) {
  const url = `${API_BASE_URL}${path}`
  const headers = new Headers(options.headers || {})

  if (inMemoryAccessToken && !headers.has('Authorization')) {
    headers.set('Authorization', `Bearer ${inMemoryAccessToken}`)
  }

  const fetchOptions = {
    ...options,
    headers,
    credentials: 'include', // Ensures HttpOnly refresh cookies are sent
  }

  let response = await fetchWithTimeout(url, fetchOptions, options.timeout || DEFAULT_TIMEOUT_MS)

  // If 401 Unauthorized and not already calling an auth endpoint, attempt silent refresh
  if (response.status === 401 && !path.startsWith('/api/auth/')) {
    try {
      const newToken = await attemptSilentRefresh()
      if (newToken) {
        headers.set('Authorization', `Bearer ${newToken}`)
        response = await fetchWithTimeout(url, { ...options, headers, credentials: 'include' }, options.timeout || DEFAULT_TIMEOUT_MS)
      }
    } catch {
      inMemoryAccessToken = null
      onUnauthorizedHandler?.()
      window.dispatchEvent(new CustomEvent('documind:unauthorized'))
    }
  }

  return response
}

async function attemptSilentRefresh() {
  if (refreshPromise) return refreshPromise

  refreshPromise = (async () => {
    try {
      const res = await fetchWithTimeout(`${API_BASE_URL}/api/auth/refresh`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
      }, 8000)

      if (!res.ok) {
        throw new Error('Refresh failed')
      }

      const data = await res.json()
      inMemoryAccessToken = data.accessToken || null
      return inMemoryAccessToken
    } finally {
      refreshPromise = null
    }
  })()

  return refreshPromise
}

/**
 * Helper to extract human-readable error messages from HTTP error responses.
 */
export async function extractErrorMessage(response, defaultMessage = 'Request failed') {
  try {
    const errorData = await response.json()
    if (errorData.message) return errorData.message
    if (errorData.errors && typeof errorData.errors === 'object') {
      const messages = Object.values(errorData.errors).flat().filter(Boolean)
      if (messages.length > 0) return messages.join(' ')
    }
    if (errorData.detail) return errorData.detail
    if (errorData.title) return errorData.title
  } catch {
    const text = await response.text().catch(() => '')
    if (text) return text
  }

  if (response.status === 401) return 'Invalid credentials or session expired.'
  if (response.status === 403) return 'You do not have permission to perform this action.'
  if (response.status === 404) return 'Requested resource was not found.'
  if (response.status === 409) return 'A resource conflict occurred. Please verify your input.'
  if (response.status === 429) return 'Too many requests. Please wait a moment and try again.'
  if (response.status >= 500) return 'DocuMind server encountered an error. Please try again shortly.'

  return defaultMessage
}

export async function apiRequest(path, options = {}) {
  const response = await authenticatedFetch(path, options)

  if (!response.ok) {
    const errorMessage = await extractErrorMessage(response, `Request failed (${response.status})`)
    throw new Error(errorMessage)
  }

  return response.status === 204 ? null : response.json()
}

export { API_BASE_URL }
