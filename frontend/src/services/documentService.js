import { apiRequest, API_BASE_URL, authenticatedFetch } from './apiClient'

export const getDocuments = () => apiRequest('/api/documents')
export const getDocument = (id) => apiRequest(`/api/documents/${id}`)
export const getDocumentStatus = (id) => apiRequest(`/api/documents/${id}/status`)
export const deleteDocument = (id) => apiRequest(`/api/documents/${id}`, { method: 'DELETE' })

export const getDocumentFileUrl = (id) => `${API_BASE_URL}/api/documents/${id}/file`

/**
 * Fetches the raw PDF file as a Blob using authenticatedFetch.
 * Automatically handles Bearer token attachment and transparent 401 token refresh.
 */
export async function fetchDocumentBlob(id) {
  const response = await authenticatedFetch(`/api/documents/${id}/file`)
  if (!response.ok) {
    if (response.status === 401) {
      throw new Error('Your session has expired or you are not authorized to view this document.')
    }
    if (response.status === 404) {
      throw new Error('This document or file could not be found. It may have been deleted.')
    }
    throw new Error(`Failed to load document (${response.status} ${response.statusText})`)
  }
  return await response.blob()
}

export async function uploadDocument(file) {
  const form = new FormData()
  form.append('file', file)
  return apiRequest('/api/documents', { method: 'POST', body: form })
}
