const API_BASE_URL = import.meta.env.VITE_API_BASE_URL !== undefined
  ? import.meta.env.VITE_API_BASE_URL
  : (import.meta.env.PROD ? '' : 'http://localhost:5000')

export async function apiRequest(path, options = {}) {
  const response = await fetch(`${API_BASE_URL}${path}`, options)
  if (!response.ok) {
    let errorMessage = `Request failed (${response.status})`
    try {
      const errorData = await response.json()
      errorMessage = errorData.message || errorData.detail || errorData.title || JSON.stringify(errorData)
    } catch {
      const text = await response.text().catch(() => '')
      if (text) errorMessage = text
    }
    throw new Error(errorMessage)
  }
  return response.status === 204 ? null : response.json()
}

export { API_BASE_URL }
